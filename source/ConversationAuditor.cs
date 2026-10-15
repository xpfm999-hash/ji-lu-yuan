using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

internal sealed class ConversationAuditGroup
{
    public readonly string Id;
    public readonly SortedSet<string> Names = new SortedSet<string>(StringComparer.Ordinal);
    public readonly SortedDictionary<string, int> Speakers = new SortedDictionary<string, int>(StringComparer.Ordinal);
    public readonly SortedDictionary<string, int> TypeCounts = new SortedDictionary<string, int>(StringComparer.Ordinal);
    public int MessageCount;
    public int HumanMessageCount;
    public int SystemMessageCount;
    public int OutgoingCount;
    public int IncomingCount;
    public int OtherSpeakerCount;
    public int UnknownCount;
    public string FirstTimestamp;
    public string LastTimestamp;
    public string ConversationType = "unknown";

    public ConversationAuditGroup(string id) { Id = id; }
}

internal sealed class ConversationAudit
{
    public readonly SortedDictionary<string, ConversationAuditGroup> Conversations = new SortedDictionary<string, ConversationAuditGroup>(StringComparer.Ordinal);
    public int MessageCount;
    public int SystemMessageCount;
    public int MissingConversationIdCount;
    public int OutgoingCount;
    public int IncomingCount;
    public int OtherSpeakerCount;
    public int UnknownCount;
    public bool DirectionConfigured;
    public readonly string[] UserSpeakers;

    public ConversationAudit(string[] userSpeakers)
    {
        UserSpeakers = userSpeakers ?? new string[0];
        DirectionConfigured = UserSpeakers.Length > 0;
    }

    public bool ContainsRequestedConversation(string requested)
    {
        if (String.IsNullOrWhiteSpace(requested)) return true;
        foreach (ConversationAuditGroup group in Conversations.Values)
        {
            if (String.Equals(group.Id, requested.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            foreach (string name in group.Names)
                if (String.Equals(name, requested.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public string ToJson(string requested, bool allConversations)
    {
        StringBuilder json = new StringBuilder();
        json.AppendLine("{");
        json.AppendLine("  \"schema_version\": \"1.1\",");
        json.Append("  \"requested_filter\": ").Append(JsonString(requested)).AppendLine(",");
        json.Append("  \"all_conversations_requested\": ").Append(allConversations ? "true" : "false").AppendLine(",");
        json.Append("  \"message_count\": ").Append(MessageCount).AppendLine(",");
        json.Append("  \"human_message_count\": ").Append(MessageCount - SystemMessageCount).AppendLine(",");
        json.Append("  \"system_message_count\": ").Append(SystemMessageCount).AppendLine(",");
        json.Append("  \"missing_conversation_id_count\": ").Append(MissingConversationIdCount).AppendLine(",");
        json.Append("  \"conversation_count\": ").Append(Conversations.Count).AppendLine(",");
        json.AppendLine("  \"direction_counts\": {");
        json.Append("    \"outgoing\": ").Append(OutgoingCount).AppendLine(",");
        json.Append("    \"incoming\": ").Append(IncomingCount).AppendLine(",");
        json.Append("    \"other_speaker\": ").Append(OtherSpeakerCount).AppendLine(",");
        json.Append("    \"unknown\": ").Append(UnknownCount).AppendLine();
        json.AppendLine("  },");
        json.Append("  \"direction_method\": ").Append(JsonString(DirectionConfigured ? "configured-self-speaker-aliases" : "not-configured; directions left unknown")).AppendLine(",");
        json.Append("  \"system_events_excluded_from_senders_and_directions\": true,").AppendLine();
        json.AppendLine("  \"conversations\": [");
        int groupIndex = 0;
        foreach (ConversationAuditGroup group in Conversations.Values)
        {
            if (groupIndex++ > 0) json.AppendLine(",");
            json.AppendLine("    {");
            json.Append("      \"conversation_id\": ").Append(JsonString(group.Id)).AppendLine(",");
            json.Append("      \"conversation\": ").Append(JsonString(PrimaryName(group))).AppendLine(",");
            json.Append("      \"conversation_type\": ").Append(JsonString(group.ConversationType)).AppendLine(",");
            json.Append("      \"message_count\": ").Append(group.MessageCount).AppendLine(",");
            json.Append("      \"human_message_count\": ").Append(group.HumanMessageCount).AppendLine(",");
            json.Append("      \"system_message_count\": ").Append(group.SystemMessageCount).AppendLine(",");
            json.Append("      \"sender_count\": ").Append(group.Speakers.Count).AppendLine(",");
            json.AppendLine("      \"sender_counts\": {");
            AppendMap(json, group.Speakers, "      ");
            json.AppendLine("      },");
            AppendCounts(json, "direction_counts", group.OutgoingCount, group.IncomingCount, group.OtherSpeakerCount, group.UnknownCount, "      ");
            json.AppendLine(",");
            json.Append("      \"type_counts\": {");
            if (group.TypeCounts.Count > 0) json.AppendLine();
            AppendMap(json, group.TypeCounts, "      ");
            json.AppendLine("      },");
            json.AppendLine("      \"names\": [");
            int nameIndex = 0;
            foreach (string name in group.Names)
            {
                if (nameIndex++ > 0) json.AppendLine(",");
                json.Append("        ").Append(JsonString(name));
            }
            json.AppendLine();
            json.AppendLine("      ],");
            json.Append("      \"first_timestamp\": ").Append(JsonString(group.FirstTimestamp)).AppendLine(",");
            json.Append("      \"last_timestamp\": ").Append(JsonString(group.LastTimestamp)).AppendLine();
            json.Append("    }");
        }
        json.AppendLine();
        json.AppendLine("  ]");
        json.AppendLine("}");
        return json.ToString();
    }

    public string ToSummaryCsv()
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("conversation,conversation_id,conversation_type,message_count,human_message_count,system_message_count,sender_count,outgoing,incoming,other_speaker,unknown,senders,first_timestamp,last_timestamp");
        foreach (ConversationAuditGroup group in Conversations.Values)
        {
            List<string> senders = new List<string>();
            foreach (KeyValuePair<string, int> speaker in group.Speakers)
                senders.Add(speaker.Key + "(" + speaker.Value + ")");
            csv.Append(Csv(PrimaryName(group))).Append(',')
                .Append(Csv(group.Id)).Append(',')
                .Append(Csv(group.ConversationType)).Append(',')
                .Append(group.MessageCount).Append(',')
                .Append(group.HumanMessageCount).Append(',')
                .Append(group.SystemMessageCount).Append(',')
                .Append(group.Speakers.Count).Append(',')
                .Append(group.OutgoingCount).Append(',')
                .Append(group.IncomingCount).Append(',')
                .Append(group.OtherSpeakerCount).Append(',')
                .Append(group.UnknownCount).Append(',')
                .Append(Csv(String.Join("、", senders.ToArray()))).Append(',')
                .Append(Csv(group.FirstTimestamp)).Append(',')
                .Append(Csv(group.LastTimestamp)).AppendLine();
        }
        return csv.ToString();
    }

    private static string PrimaryName(ConversationAuditGroup group)
    {
        return group.Names.Count == 0 ? group.Id : String.Join(" / ", new List<string>(group.Names).ToArray());
    }

    private static void AppendCounts(StringBuilder json, string key, int outgoing, int incoming, int otherSpeaker, int unknown, string indent)
    {
        json.Append(indent).Append(JsonString(key)).AppendLine(": {");
        json.Append(indent).Append("  \"outgoing\": ").Append(outgoing).AppendLine(",");
        json.Append(indent).Append("  \"incoming\": ").Append(incoming).AppendLine(",");
        json.Append(indent).Append("  \"other_speaker\": ").Append(otherSpeaker).AppendLine(",");
        json.Append(indent).Append("  \"unknown\": ").Append(unknown).AppendLine();
        json.Append(indent).Append("}");
    }

    private static void AppendMap(StringBuilder json, SortedDictionary<string, int> map, string indent)
    {
        int index = 0;
        foreach (KeyValuePair<string, int> item in map)
        {
            if (index++ > 0) json.AppendLine(",");
            else json.AppendLine();
            json.Append(indent).Append("  ").Append(JsonString(item.Key)).Append(": ").Append(item.Value);
        }
        if (map.Count > 0) json.AppendLine();
    }

    private static string Csv(string value)
    {
        if (value == null) return "\"\"";
        return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
    }

    internal static string JsonString(string value)
    {
        if (value == null) return "null";
        StringBuilder escaped = new StringBuilder("\"");
        foreach (char character in value)
        {
            switch (character)
            {
                case '\\': escaped.Append("\\\\"); break;
                case '"': escaped.Append("\\\""); break;
                case '\n': escaped.Append("\\n"); break;
                case '\r': escaped.Append("\\r"); break;
                case '\t': escaped.Append("\\t"); break;
                case '\b': escaped.Append("\\b"); break;
                case '\f': escaped.Append("\\f"); break;
                default:
                    if (character < 0x20) escaped.Append("\\u").Append(((int)character).ToString("x4"));
                    else escaped.Append(character);
                    break;
            }
        }
        escaped.Append('"');
        return escaped.ToString();
    }
}

internal static class ConversationAuditor
{
    public static ConversationAudit Inspect(string transcriptPath) { return Inspect(transcriptPath, new string[0]); }

    public static ConversationAudit Inspect(string transcriptPath, string[] userSpeakers)
    {
        ConversationAudit audit = new ConversationAudit(userSpeakers);
        bool inMessages = false;
        bool inRecord = false;
        bool inRaw = false;
        bool systemMessage = false;
        string conversationId = null, conversationName = null, speakerName = null, timestamp = null, messageType = null, conversationType = null;

        foreach (string line in File.ReadLines(transcriptPath, new UTF8Encoding(false, true)))
        {
            if (!inMessages)
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("\"messages\"", StringComparison.Ordinal) && trimmed.IndexOf('[') >= 0) inMessages = true;
                continue;
            }
            if (!inRecord)
            {
                if (String.Equals(line, "    {", StringComparison.Ordinal))
                {
                    inRecord = true; inRaw = false; systemMessage = false;
                    conversationId = conversationName = speakerName = timestamp = messageType = conversationType = null;
                }
                else if (String.Equals(line.Trim(), "]", StringComparison.Ordinal)) break;
                continue;
            }
            if (String.Equals(line, "    },", StringComparison.Ordinal) || String.Equals(line, "    }", StringComparison.Ordinal))
            {
                AddRecord(audit, conversationId, conversationName, speakerName, systemMessage, timestamp, messageType, conversationType);
                inRecord = false; inRaw = false; continue;
            }
            if (String.Equals(line, "      \"raw\": {", StringComparison.Ordinal)) { inRaw = true; continue; }
            if (inRaw && (String.Equals(line, "      },", StringComparison.Ordinal) || String.Equals(line, "      }", StringComparison.Ordinal))) { inRaw = false; continue; }

            string value;
            if (TryGetScalar(line, 6, "speaker", out value)) speakerName = value;
            if (TryGetScalar(line, 6, "conversation", out value)) conversationName = value;
            if (TryGetScalar(line, 6, "conversation_id", out value)) conversationId = value;
            if (TryGetScalar(line, 6, "timestamp", out value)) timestamp = value;
            if (TryGetScalar(line, 6, "conversation_type", out value)) conversationType = value;
            if (inRaw)
            {
                if (TryGetScalar(line, 8, "conversation_id", out value)) conversationId = value;
                if (TryGetScalar(line, 8, "type", out value))
                {
                    messageType = value;
                    if (String.Equals(value, "系统消息", StringComparison.OrdinalIgnoreCase) || String.Equals(value, "system", StringComparison.OrdinalIgnoreCase)) systemMessage = true;
                }
                if (IsNumericProperty(line, 8, "msg_type", "10000") || IsNumericProperty(line, 8, "type_code", "10000")) systemMessage = true;
            }
        }
        return audit;
    }

    public static void WriteReports(string outputDir, ConversationAudit audit, string requested, bool allConversations)
    {
        File.WriteAllText(Path.Combine(outputDir, "conversation_audit.json"), audit.ToJson(requested, allConversations), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outputDir, "conversation_index.json"), audit.ToJson(requested, allConversations), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outputDir, "conversation_summary.csv"), audit.ToSummaryCsv(), new UTF8Encoding(false));
    }

    private static bool TryGetScalar(string line, int indentation, string key, out string value)
    {
        value = null;
        string prefix = new String(' ', indentation) + "\"" + key + "\"";
        if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;
        int colon = line.IndexOf(':', prefix.Length);
        if (colon < 0) return false;
        int start = colon + 1;
        while (start < line.Length && Char.IsWhiteSpace(line[start])) start++;
        if (start >= line.Length) return false;
        if (line[start] == '"')
        {
            start++;
            int end = start;
            bool escaped = false;
            while (end < line.Length)
            {
                char character = line[end];
                if (!escaped && character == '"') break;
                if (!escaped && character == '\\') escaped = true; else escaped = false;
                end++;
            }
            if (end >= line.Length) return false;
            value = JsonUnescape(line.Substring(start, end - start));
        }
        else
        {
            int end = line.IndexOf(',', start);
            if (end < 0) end = line.Length;
            value = line.Substring(start, end - start).Trim();
            if (String.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) value = null;
        }
        return true;
    }

    private static bool IsNumericProperty(string line, int indentation, string key, string expected)
    {
        string prefix = new String(' ', indentation) + "\"" + key + "\"";
        if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;
        int colon = line.IndexOf(':', prefix.Length);
        return colon >= 0 && String.Equals(line.Substring(colon + 1).Trim().TrimEnd(','), expected, StringComparison.Ordinal);
    }

    private static void AddRecord(ConversationAudit audit, string conversationId, string conversationName, string speakerName, bool systemMessage, string timestamp, string messageType, string conversationType)
    {
        audit.MessageCount++;
        if (systemMessage) audit.SystemMessageCount++;
        if (String.IsNullOrWhiteSpace(conversationId)) { audit.MissingConversationIdCount++; return; }
        ConversationAuditGroup group;
        if (!audit.Conversations.TryGetValue(conversationId, out group)) { group = new ConversationAuditGroup(conversationId); audit.Conversations.Add(conversationId, group); }
        group.MessageCount++;
        if (!String.IsNullOrWhiteSpace(conversationName)) group.Names.Add(conversationName);
        if (!String.IsNullOrWhiteSpace(conversationType)) group.ConversationType = conversationType;
        else if (group.ConversationType == "unknown")
            group.ConversationType = conversationId.EndsWith("@chatroom", StringComparison.OrdinalIgnoreCase) ? "group"
                : conversationId.StartsWith("wxid_", StringComparison.OrdinalIgnoreCase) ? "private" : "unknown";
        if (!String.IsNullOrWhiteSpace(timestamp))
        {
            if (group.FirstTimestamp == null || CompareTimestamps(timestamp, group.FirstTimestamp) < 0) group.FirstTimestamp = timestamp;
            if (group.LastTimestamp == null || CompareTimestamps(timestamp, group.LastTimestamp) > 0) group.LastTimestamp = timestamp;
        }
        if (systemMessage)
        {
            group.SystemMessageCount++; return;
        }
        group.HumanMessageCount++;
        string typeKey = String.IsNullOrWhiteSpace(messageType) ? "unknown" : messageType;
        int typeCount; group.TypeCounts.TryGetValue(typeKey, out typeCount); group.TypeCounts[typeKey] = typeCount + 1;
        if (!String.IsNullOrWhiteSpace(speakerName))
        {
            int count; group.Speakers.TryGetValue(speakerName, out count); group.Speakers[speakerName] = count + 1;
        }
        bool self = false;
        foreach (string alias in audit.UserSpeakers)
            if (!String.IsNullOrWhiteSpace(alias) && String.Equals(alias.Trim(), speakerName, StringComparison.OrdinalIgnoreCase)) { self = true; break; }
        if (!audit.DirectionConfigured || String.IsNullOrWhiteSpace(speakerName))
        {
            audit.UnknownCount++; group.UnknownCount++;
        }
        else if (self)
        {
            audit.OutgoingCount++; group.OutgoingCount++;
        }
        else if (String.Equals(group.ConversationType, "private", StringComparison.OrdinalIgnoreCase))
        {
            audit.IncomingCount++; group.IncomingCount++;
        }
        else
        {
            audit.OtherSpeakerCount++; group.OtherSpeakerCount++;
        }
    }

    private static int CompareTimestamps(string left, string right)
    {
        decimal leftNumber, rightNumber;
        if (Decimal.TryParse(left, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out leftNumber) &&
            Decimal.TryParse(right, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out rightNumber))
            return leftNumber.CompareTo(rightNumber);
        return String.Compare(left, right, StringComparison.Ordinal);
    }

    private static string JsonUnescape(string value)
    {
        StringBuilder output = new StringBuilder(value.Length);
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character != '\\' || index + 1 >= value.Length) { output.Append(character); continue; }
            char escape = value[++index];
            switch (escape)
            {
                case '"': output.Append('"'); break; case '\\': output.Append('\\'); break; case '/': output.Append('/'); break;
                case 'b': output.Append('\b'); break; case 'f': output.Append('\f'); break; case 'n': output.Append('\n'); break;
                case 'r': output.Append('\r'); break; case 't': output.Append('\t'); break;
                case 'u':
                    if (index + 4 < value.Length)
                    {
                        int code;
                        if (Int32.TryParse(value.Substring(index + 1, 4), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out code)) { output.Append((char)code); index += 4; }
                        else output.Append('u');
                    }
                    else output.Append('u');
                    break;
                default: output.Append(escape); break;
            }
        }
        return output.ToString();
    }
}
