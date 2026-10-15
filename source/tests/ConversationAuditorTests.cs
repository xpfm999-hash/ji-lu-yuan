using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

internal static class ConversationAuditorTests
{
    private static int Main(string[] args)
    {
        try { RunFixtureAssertions(); }
        catch (Exception exception)
        {
            Console.WriteLine(exception.GetType().FullName + ": " + exception.Message);
            return 1;
        }
        Console.WriteLine("fixture assertions: PASS");
        if (args.Length > 0) PrintAudit(args[0]);
        return 0;
    }

    private static void RunFixtureAssertions()
    {
        string path = Path.Combine(Path.GetTempPath(), "jiluyuan-audit-test-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine("  \"messages\": [");
            json.Append(Message(0, "Aqron", "Private", "wxid_contact", "文本", "1700000000", false, true));
            json.Append(Message(1, "xpfm9", "Private", "wxid_contact", "文本", "1700000001", false, true));
            json.Append(Message(2, "Friend", "Private", "wxid_contact", "图片", "1700000002", false, true));
            json.Append(Message(3, "SystemAlias", "Private", "wxid_contact", "系统消息", "1700000003", true, true));
            json.Append(Message(4, "Other", "Group", "room@chatroom", "文本", "1700000004", false, false));
            json.AppendLine("  ]");
            json.AppendLine("}");
            File.WriteAllText(path, json.ToString(), new UTF8Encoding(false));

            ConversationAudit audit = ConversationAuditor.Inspect(path, new string[] { "Aqron", "xpfm9" });
            Assert(audit.MessageCount == 5, "message total");
            Assert(audit.Conversations.Count == 2, "conversation isolation");
            Assert(audit.MissingConversationIdCount == 0, "all IDs present");
            Assert(audit.SystemMessageCount == 1, "system event count");
            Assert(audit.OutgoingCount == 2 && audit.IncomingCount == 1 && audit.OtherSpeakerCount == 1 && audit.UnknownCount == 0, "alias-based direction counts exclude system events");
            ConversationAuditGroup privateChat = audit.Conversations["wxid_contact"];
            Assert(privateChat.MessageCount == 4, "private message count");
            Assert(privateChat.HumanMessageCount == 3, "human message count");
            Assert(privateChat.SystemMessageCount == 1, "private system event count");
            Assert(privateChat.Speakers.Count == 3, "system alias excluded from human speakers");
            Assert(privateChat.Speakers["Aqron"] == 1 && privateChat.Speakers["xpfm9"] == 1 && privateChat.Speakers["Friend"] == 1, "speaker counts");
            Assert(privateChat.OutgoingCount == 2 && privateChat.IncomingCount == 1, "per-conversation direction counts");
            Assert(audit.OtherSpeakerCount == 1 && audit.Conversations["room@chatroom"].OtherSpeakerCount == 1, "group-chat participants are not labeled as direct incoming messages");
            Assert(privateChat.FirstTimestamp == "1700000000" && privateChat.LastTimestamp == "1700000003", "timestamp bounds");
            Assert(audit.ContainsRequestedConversation("Private"), "exact display-name matching");
            Assert(!audit.ContainsRequestedConversation("Pri"), "partial name rejected");

            string reportDir = Path.Combine(Path.GetTempPath(), "jiluyuan-audit-report-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(reportDir);
            try
            {
                ConversationAuditor.WriteReports(reportDir, audit, "Private", false);
                string index = File.ReadAllText(Path.Combine(reportDir, "conversation_index.json"), new UTF8Encoding(false));
                string summary = File.ReadAllText(Path.Combine(reportDir, "conversation_summary.csv"), new UTF8Encoding(false));
                new JavaScriptSerializer().DeserializeObject(index);
                Assert(index.Contains("\"sender_count\": 3"), "human sender count written");
                Assert(index.Contains("\"system_message_count\": 1"), "system events separately counted");
                Assert(!index.Contains("SystemAlias"), "system label absent from conversation index");
                Assert(!summary.Contains("SystemAlias"), "system label absent from CSV sender list");
                Assert(summary.Contains("\"Private\",\"wxid_contact\",\"private\",4,3,1,3,2,1,0,0"), "CSV separates human and system counts");
                ConversationAudit noAliases = ConversationAuditor.Inspect(path);
                Assert(noAliases.OutgoingCount == 0 && noAliases.IncomingCount == 0 && noAliases.UnknownCount == 4, "directions stay unknown without aliases");
            }
            finally
            {
                if (Directory.Exists(reportDir)) Directory.Delete(reportDir, true);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static string Message(int index, string speaker, string name, string id, string type, string timestamp, bool system, bool comma)
    {
        StringBuilder record = new StringBuilder();
        record.AppendLine("    {");
        record.Append("      \"index\": ").Append(index).AppendLine(",");
        record.Append("      \"timestamp\": ").Append(ConversationAudit.JsonString(timestamp)).AppendLine(",");
        record.Append("      \"speaker\": ").Append(ConversationAudit.JsonString(speaker)).AppendLine(",");
        record.Append("      \"conversation\": ").Append(ConversationAudit.JsonString(name)).AppendLine(",");
        record.AppendLine("      \"raw\": {");
        record.Append("        \"conversation_id\": ").Append(ConversationAudit.JsonString(id)).AppendLine(",");
        record.Append("        \"type\": ").Append(ConversationAudit.JsonString(type));
        if (system) record.AppendLine(",").AppendLine("        \"msg_type\": 10000");
        else record.AppendLine();
        record.AppendLine("      }");
        record.Append("    }");
        if (comma) record.Append(',');
        record.AppendLine();
        return record.ToString();
    }

    private static void Assert(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("assertion failed: " + label);
    }

    private static void PrintAudit(string path)
    {
        string[] aliases = Environment.GetEnvironmentVariable("JILUYUAN_TEST_ALIASES") == null
            ? new string[0]
            : Environment.GetEnvironmentVariable("JILUYUAN_TEST_ALIASES").Split(',');
        ConversationAudit audit = ConversationAuditor.Inspect(path, aliases);
        Console.WriteLine("messages=" + audit.MessageCount + "; conversations=" + audit.Conversations.Count + "; system=" + audit.SystemMessageCount + "; missing_ids=" + audit.MissingConversationIdCount);
        Console.WriteLine("directions=outgoing:" + audit.OutgoingCount + ";incoming:" + audit.IncomingCount + ";other_speaker:" + audit.OtherSpeakerCount + ";unknown:" + audit.UnknownCount);
        ConversationAuditGroup group;
        if (audit.Conversations.TryGetValue("wxid_7rb53ura2ox122", out group))
        {
            Console.WriteLine("target=wxid_7rb53ura2ox122; name=" + String.Join("|", group.Names) + "; messages=" + group.MessageCount + "; system=" + group.SystemMessageCount + "; human_speakers=" + group.Speakers.Count);
            foreach (System.Collections.Generic.KeyValuePair<string, int> speaker in group.Speakers)
                Console.WriteLine("speaker=" + speaker.Key + "; messages=" + speaker.Value);
        }
    }
}
