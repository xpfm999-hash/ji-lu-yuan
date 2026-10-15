using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class ExportSettings
{
    public string Input;
    public string OutputDir;
    public string Conversation;
    public string UserSpeaker;
    public string AssistantSpeaker;
    public string TranscriptMap;
    public string Transcriber;
    public bool CopyMedia;
    public bool AllConversations;
}

internal sealed class ExportResult
{
    public int ExitCode;
    public string Stdout;
    public string Stderr;
    public string OutputDir;
    public bool AllConversations;
    public bool ValidationFailed;
    public string ValidationMessage;
    public ConversationAudit Audit;
    public DateTime ExportStartedUtc;
    public string DirectionSummary;
}

internal sealed class MainForm : Form
{
    private readonly TextBox inputBox = new TextBox();
    private readonly TextBox outputBox = new TextBox();
    private readonly TextBox conversationBox = new TextBox();
    private readonly TextBox userSpeakerBox = new TextBox();
    private readonly TextBox assistantSpeakerBox = new TextBox();
    private readonly TextBox transcriptMapBox = new TextBox();
    private readonly TextBox transcriberBox = new TextBox();
    private readonly CheckBox copyMediaBox = new CheckBox();
    private readonly CheckBox allConversationsBox = new CheckBox();
    private readonly ToolTip roleHelp = new ToolTip();
    private readonly TextBox logBox = new TextBox();
    private readonly Label statusLabel = new Label();
    private readonly Button exportButton = new Button();
    private readonly Button openButton = new Button();
    private string lastOutputDir = "";

    public MainForm()
    {
        Text = "\u8bb0\u5f55\u5458 v0.3.3 - \u804a\u5929\u8bb0\u5f55\u5bfc\u51fa\u5668";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 600);
        Size = new Size(1050, 720);
        Font = new Font("Microsoft YaHei UI", 9F);

        TableLayoutPanel root = new TableLayoutPanel();
        root.Dock = DockStyle.Fill;
        root.Padding = new Padding(18);
        root.ColumnCount = 3;
        root.RowCount = 1;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 265F));
        Controls.Add(root);

        int row = 0;
        Label title = new Label();
        title.Text = "\u8bb0\u5f55\u5458 v0.3.3 - \u804a\u5929\u8bb0\u5f55\u5bfc\u51fa\u5668";
        title.Font = new Font(Font, FontStyle.Bold);
        title.Font = new Font(title.Font.FontFamily, 18F, FontStyle.Bold);
        title.AutoSize = true;
        root.Controls.Add(title, 0, row);
        root.SetColumnSpan(title, 3);
        row++;

        Label description = new Label();
        description.Text = "\u53ef\u76f4\u63a5\u9009\u62e9\u7535\u8111\u5fae\u4fe1\u6216 Instagram \u6570\u636e\u6587\u4ef6\u5939\uff0c\u4e00\u6b21\u5bfc\u51fa GPT \u6587\u4ef6\u3001\u539f\u59cb\u5f52\u6863\u3001\u5a92\u4f53\u6587\u4ef6\u548c\u5b8c\u6574\u6027\u62a5\u544a\u3002\u5fae\u4fe1\u5355\u804a\u8bf7\u586b\u51c6\u786e\u4f1a\u8bdd\u540d\u6216\u4f1a\u8bdd ID\uff1b\u5168\u4f1a\u8bdd\u5bfc\u51fa\u9700\u5355\u72ec\u52fe\u9009\u786e\u8ba4\u3002\u5bfc\u51fa\u540e\u4f1a\u6838\u5bf9\u4f1a\u8bdd ID\u5e76\u5355\u72ec\u7edf\u8ba1\u7cfb\u7edf\u4e8b\u4ef6\u3002\u586b\u5199\u6211\u65b9\u53d1\u8a00\u8005\u540d\u79f0\u624d\u4f1a\u81ea\u52a8\u6807\u6ce8\u6536\u53d1\u65b9\u5411\uff1b\u591a\u4e2a\u5386\u53f2\u6635\u79f0\u7528\u82f1\u6587\u9017\u53f7\u5206\u9694\u3002";
        description.AutoSize = true;
        description.MaximumSize = new Size(780, 0);
        root.Controls.Add(description, 0, row);
        root.SetColumnSpan(description, 3);
        row++;

        AddField(root, ref row, "\u804a\u5929\u6587\u4ef6/\u6587\u4ef6\u5939", inputBox, MakeInputButtons());
        AddField(root, ref row, "\u8f93\u51fa\u76ee\u5f55\uff08\u53ef\u9009\uff09", outputBox, new Button[] { MakeButton("\u9009\u62e9\u76ee\u5f55", ChooseOutputFolder) });
        AddField(root, ref row, "\u6307\u5b9a\u4f1a\u8bdd\u540d/ID", conversationBox, new Button[0]);

        allConversationsBox.Text = "\u6211\u786e\u8ba4\u8981\u5bfc\u51fa\u5168\u90e8\u5fae\u4fe1\u4f1a\u8bdd\uff08\u5305\u542b\u5176\u4ed6\u8054\u7cfb\u4eba\uff09";
        allConversationsBox.AutoSize = true;
        allConversationsBox.Checked = false;
        root.Controls.Add(allConversationsBox, 1, row);
        root.SetColumnSpan(allConversationsBox, 2);
        row++;

        Label roleLabel = MakeLabel("GPT\u89d2\u8272\u6620\u5c04\uff08\u53ef\u9009\uff09");
        root.Controls.Add(roleLabel, 0, row);
        TableLayoutPanel roles = new TableLayoutPanel();
        roles.Dock = DockStyle.Fill;
        roles.AutoSize = true;
        roles.ColumnCount = 4;
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        roles.Controls.Add(MakeLabel("\u6211\u65b9\u53d1\u8a00\u8005"), 0, 0);
        roles.Controls.Add(userSpeakerBox, 1, 0);
        roles.Controls.Add(MakeLabel("\u5bf9\u65b9\u53d1\u8a00\u8005"), 2, 0);
        roles.Controls.Add(assistantSpeakerBox, 3, 0);
        roleHelp.SetToolTip(userSpeakerBox, "\u586b\u5bfc\u51fa\u4e2d\u6211\u65b9\u53d1\u8a00\u8005\u7684\u539f\u59cb\u6635\u79f0\uff1b\u591a\u4e2a\u5386\u53f2\u6635\u79f0\u7528\u82f1\u6587\u9017\u53f7\u5206\u9694\uff08\u4f8b\u5982 name1,name2\uff09\u3002\u7528\u4e8e\u6807\u6ce8\u53d1\u9001/\u6536\u5230\u65b9\u5411\uff1b\u4e0d\u586b\u5c31\u4e0d\u731c\u65b9\u5411\u3002\u4e0d\u4f1a\u6539\u5199\u539f\u59cb\u53d1\u8a00\u8005\u5b57\u6bb5\u3002");
        roleHelp.SetToolTip(assistantSpeakerBox, "\u586b\u5bfc\u51fa\u8bb0\u5f55\u4e2d\u5bf9\u65b9\u53d1\u8a00\u8005\u7684\u6635\u79f0\uff1b\u53ea\u7528\u4e8e GPT user/assistant \u89d2\u8272\u6620\u5c04\uff0c\u4e0d\u4f1a\u6539\u5199\u539f\u59cb\u53d1\u8a00\u8005\u5b57\u6bb5\u3002");
        root.Controls.Add(roles, 1, row);
        root.SetColumnSpan(roles, 2);
        row++;

        AddField(root, ref row, "\u8bed\u97f3\u8f6c\u5199\u6620\u5c04", transcriptMapBox, new Button[] { MakeButton("\u9009\u62e9 JSON", ChooseTranscriptMap) });
        AddField(root, ref row, "\u672c\u5730\u8f6c\u5199\u7a0b\u5e8f", transcriberBox, new Button[] { MakeButton("\u9009\u62e9\u7a0b\u5e8f", ChooseTranscriber) });

        copyMediaBox.Text = "\u590d\u5236\u80fd\u627e\u5230\u7684\u56fe\u7247\u3001\u89c6\u9891\u3001\u8bed\u97f3\u5230\u8f93\u51fa\u76ee\u5f55";
        copyMediaBox.Checked = true;
        copyMediaBox.AutoSize = true;
        root.Controls.Add(copyMediaBox, 1, row);
        root.SetColumnSpan(copyMediaBox, 2);
        row++;

        FlowLayoutPanel actions = new FlowLayoutPanel();
        actions.AutoSize = true;
        actions.Dock = DockStyle.Fill;
        exportButton.Text = "\u4e00\u952e\u5bfc\u51fa";
        exportButton.AutoSize = true;
        exportButton.Click += delegate { StartExport(); };
        openButton.Text = "\u6253\u5f00\u8f93\u51fa\u76ee\u5f55";
        openButton.AutoSize = true;
        openButton.Enabled = false;
        openButton.Click += delegate { OpenOutput(); };
        statusLabel.Text = "\u8bf7\u5148\u9009\u62e9\u804a\u5929\u6587\u4ef6\u6216\u6587\u4ef6\u5939";
        statusLabel.AutoSize = true;
        statusLabel.Padding = new Padding(12, 7, 0, 0);
        actions.Controls.Add(exportButton);
        actions.Controls.Add(openButton);
        actions.Controls.Add(statusLabel);
        root.Controls.Add(actions, 0, row);
        root.SetColumnSpan(actions, 3);
        row++;

        Label logLabel = MakeLabel("\u8fd0\u884c\u65e5\u5fd7");
        root.Controls.Add(logLabel, 0, row);
        logBox.Multiline = true;
        logBox.ScrollBars = ScrollBars.Vertical;
        logBox.ReadOnly = true;
        logBox.BackColor = Color.WhiteSmoke;
        logBox.Dock = DockStyle.Fill;
        root.Controls.Add(logBox, 1, row);
        root.SetColumnSpan(logBox, 2);
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        row++;
    }

    private static Label MakeLabel(string text)
    {
        Label label = new Label();
        label.Text = text;
        label.AutoSize = true;
        label.Padding = new Padding(0, 7, 0, 0);
        return label;
    }

    private static Button MakeButton(string text, EventHandler handler)
    {
        Button button = new Button();
        button.Text = text;
        button.AutoSize = true;
        button.Click += handler;
        return button;
    }

    private Button[] MakeInputButtons()
    {
        return new Button[]
        {
            MakeButton("\u9009\u6587\u4ef6", ChooseFile),
            MakeButton("\u9009\u6587\u4ef6\u5939", ChooseFolder),
            MakeButton("\u81ea\u52a8\u627e\u5fae\u4fe1", FindWeChatFolder)
        };
    }

    private void AddField(TableLayoutPanel root, ref int row, string labelText, TextBox box, Button[] buttons)
    {
        root.Controls.Add(MakeLabel(labelText), 0, row);
        box.Dock = DockStyle.Fill;
        root.Controls.Add(box, 1, row);
        FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
        buttonPanel.AutoSize = true;
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.WrapContents = false;
        foreach (Button button in buttons)
        {
            buttonPanel.Controls.Add(button);
        }
        root.Controls.Add(buttonPanel, 2, row);
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void ChooseFile(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "\u9009\u62e9\u804a\u5929\u5bfc\u51fa\u6587\u4ef6";
            dialog.Filter = "\u804a\u5929\u6587\u4ef6|*.txt;*.md;*.markdown;*.json;*.jsonl;*.csv;*.tsv;*.html;*.htm;*.db;*.sqlite;*.sqlite3|\u6240\u6709\u6587\u4ef6|*.*";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                inputBox.Text = dialog.FileName;
            }
        }
    }

    private void ChooseFolder(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "\u9009\u62e9\u5fae\u4fe1\u6570\u636e\u6587\u4ef6\u5939\uff08\u5305\u542b Msg \u6216 db_storage\uff09\u3001Instagram \u6570\u636e\u6587\u4ef6\u5939\uff0c\u6216\u5df2\u5bfc\u51fa\u6587\u4ef6\u5939";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                inputBox.Text = dialog.SelectedPath;
            }
        }
    }

    private void FindWeChatFolder(object sender, EventArgs e)
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] candidates = new string[]
        {
            Path.Combine(documents, "WeChat Files"),
            Path.Combine(documents, "xwechat_files"),
            Path.Combine(localAppData, "Tencent", "WeChat"),
            Path.Combine(appData, "Tencent", "WeChat")
        };
        foreach (string candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                inputBox.Text = candidate;
                AppendLog("已找到可能的微信数据目录：" + candidate);
                statusLabel.Text = "已选择微信数据目录";
                return;
            }
        }
        MessageBox.Show(this, "没有在常见位置找到微信数据目录。请在电脑微信“设置 → 文件管理 → 打开文件夹”后，手动选择包含 Msg 或 db_storage 的目录。", "未找到微信目录", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ChooseOutputFolder(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "\u9009\u62e9\u5bfc\u51fa\u7ed3\u679c\u4fdd\u5b58\u4f4d\u7f6e";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                outputBox.Text = dialog.SelectedPath;
            }
        }
    }

    private void ChooseTranscriptMap(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "\u9009\u62e9\u8bed\u97f3\u8f6c\u5199\u6620\u5c04 JSON";
            dialog.Filter = "JSON|*.json|\u6240\u6709\u6587\u4ef6|*.*";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                transcriptMapBox.Text = dialog.FileName;
            }
        }
    }

    private void ChooseTranscriber(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "\u9009\u62e9\u672c\u5730\u8bed\u97f3\u8f6c\u5199\u7a0b\u5e8f";
            dialog.Filter = "\u7a0b\u5e8f|*.exe;*.cmd;*.bat;*.ps1|\u6240\u6709\u6587\u4ef6|*.*";
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                transcriberBox.Text = dialog.FileName;
            }
        }
    }

    private void StartExport()
    {
        string input = inputBox.Text.Trim();
        if (input.Length == 0)
        {
            MessageBox.Show(this, "\u8bf7\u5148\u9009\u62e9\u804a\u5929\u6587\u4ef6\u6216\u6587\u4ef6\u5939\u3002", "\u7f3a\u5c11\u8f93\u5165", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!File.Exists(input) && !Directory.Exists(input))
        {
            MessageBox.Show(this, "\u9009\u62e9\u7684\u8f93\u5165\u4e0d\u5b58\u5728\u3002", "\u8f93\u5165\u9519\u8bef", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ExportSettings settings = new ExportSettings();
        settings.Input = input;
        settings.OutputDir = outputBox.Text.Trim();
        settings.Conversation = conversationBox.Text.Trim();
        settings.UserSpeaker = userSpeakerBox.Text.Trim();
        settings.AssistantSpeaker = assistantSpeakerBox.Text.Trim();
        settings.TranscriptMap = transcriptMapBox.Text.Trim();
        settings.Transcriber = transcriberBox.Text.Trim();
        settings.CopyMedia = copyMediaBox.Checked;
        bool wechatInput = LooksLikeWeChatInput(input);
        settings.AllConversations = wechatInput && allConversationsBox.Checked;
        if (wechatInput && String.IsNullOrWhiteSpace(settings.Conversation) && !settings.AllConversations)
        {
            MessageBox.Show(this, "\u8bf7\u586b\u5199\u51c6\u786e\u7684\u4f1a\u8bdd\u540d\u6216\u4f1a\u8bdd ID\u3002\u5982\u679c\u786e\u5b9e\u9700\u8981\u5bfc\u51fa\u5168\u90e8\u804a\u5929\uff0c\u8bf7\u5148\u52fe\u9009\u201c\u6211\u786e\u8ba4\u8981\u5bfc\u51fa\u5168\u90e8\u5fae\u4fe1\u4f1a\u8bdd\u201d\u3002", "\u8bf7\u6307\u5b9a\u4f1a\u8bdd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            statusLabel.Text = "\u8bf7\u586b\u4f1a\u8bdd\u540d/ID\uff0c\u6216\u663e\u5f0f\u9009\u62e9\u5168\u91cf\u5bfc\u51fa";
            return;
        }
        if (wechatInput && !String.IsNullOrWhiteSpace(settings.Conversation) && settings.AllConversations)
        {
            MessageBox.Show(this, "\u201c\u5355\u4e2a\u4f1a\u8bdd\u201d\u548c\u201c\u5168\u90e8\u4f1a\u8bdd\u201d\u4e0d\u80fd\u540c\u65f6\u9009\u62e9\u3002\u8bf7\u6e05\u7a7a\u4f1a\u8bdd\u6846\u6216\u53d6\u6d88\u5168\u91cf\u9009\u9879\u3002", "\u9009\u9879\u51b2\u7a81", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (wechatInput && settings.AllConversations)
        {
            DialogResult decision = MessageBox.Show(this, "\u672c\u6b21\u4f1a\u5bfc\u51fa\u8fd9\u4e2a\u5fae\u4fe1\u8d26\u53f7\u7684\u5168\u90e8\u4f1a\u8bdd\uff0c\u53ef\u80fd\u5305\u542b\u5927\u91cf\u5176\u4ed6\u8054\u7cfb\u4eba\u3002\u5bfc\u51fa\u540e\u4ecd\u4f1a\u6309\u4f1a\u8bdd ID \u68c0\u67e5\uff0c\u4f46\u4e0d\u80fd\u628a\u5168\u91cf\u6587\u4ef6\u5f53\u6210\u5355\u4eba\u79c1\u804a\u3002\n\n\u8981\u7ee7\u7eed\u5417\uff1f", "\u660e\u786e\u786e\u8ba4\u5168\u91cf\u5bfc\u51fa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (decision != DialogResult.Yes) return;
        }

        SetBusy(true);
        AppendLog("\u5f00\u59cb\u5904\u7406\uff1a" + input);
        if (wechatInput) AppendLog("\u5bfc\u51fa\u540e\u5c06\u9010\u6761\u6838\u9a8c\u4f1a\u8bdd ID\uff1b\u5927\u578b\u8bb0\u5f55\u9700\u8981\u4e00\u4e9b\u626b\u63cf\u65f6\u95f4\u3002");
        Task.Run(delegate { return RunExporter(settings); }).ContinueWith(delegate(Task<ExportResult> task)
        {
            BeginInvoke((Action)delegate
            {
                SetBusy(false);
                if (task.IsFaulted)
                {
                    ShowFailure(task.Exception == null ? "\u672a\u77e5\u9519\u8bef" : task.Exception.GetBaseException().Message);
                }
                else
                {
                    FinishExport(task.Result);
                }
            });
        });
    }

    private void SetBusy(bool busy)
    {
        exportButton.Enabled = !busy;
        statusLabel.Text = busy ? "\u6b63\u5728\u5bfc\u51fa\u2026" : statusLabel.Text;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void FinishExport(ExportResult result)
    {
        lastOutputDir = result.OutputDir;
        outputBox.Text = lastOutputDir;
        openButton.Enabled = Directory.Exists(lastOutputDir);
        AppendLog("\u6d88\u606f\u6570\uff1a" + ExtractSummaryValue(result.Stdout, "message_count"));
        AppendLog("\u6570\u636e\u6e90\uff1a" + (ExtractSummaryValue(result.Stdout, "source_type") ?? "?"));
        string protectedDatabases = ExtractSummaryValue(result.Stdout, "protected_databases");
        if (protectedDatabases != null && protectedDatabases != "0")
        {
            AppendLog("\u672a\u89e3\u5bc6\u6570\u636e\u5e93\uff1a" + protectedDatabases + " \uff08\u672a\u9759\u9ed8\u4e22\u5931\uff09");
        }
        if (result.Audit != null)
        {
            AppendLog("\u4f1a\u8bdd ID \u6570\uff1a" + result.Audit.Conversations.Count + "\uff1b\u6d88\u606f\u6570\uff1a" + result.Audit.MessageCount + "\uff1b\u7cfb\u7edf\u4e8b\u4ef6\uff08\u4e0d\u7b97\u4f5c\u4eba\u5458\uff09\uff1a" + result.Audit.SystemMessageCount);
            AppendLog("\u4f1a\u8bdd\u6838\u9a8c\u62a5\u544a\uff1aconversation_audit.json");
        }
        if (!String.IsNullOrWhiteSpace(result.DirectionSummary)) AppendLog("\u6536\u53d1\u65b9\u5411\uff1a" + result.DirectionSummary);
        if (!String.IsNullOrWhiteSpace(result.ValidationMessage)) AppendLog("\u6838\u9a8c\u63d0\u793a\uff1a" + result.ValidationMessage);
        bool complete = result.ExitCode == 0 && !result.ValidationFailed;
        AppendLog("\u5b8c\u6574\u6027\uff1a" + (complete ? "\u5b8c\u6210" : "\u672a\u901a\u8fc7\u6838\u9a8c\uff0c\u8bf7\u5148\u9605\u8bfb\u63d0\u793a\u548c conversation_audit.json"));
        if (!String.IsNullOrWhiteSpace(result.Stderr))
        {
            AppendLog(result.Stderr.Trim());
        }
        if (complete)
        {
            statusLabel.Text = result.AllConversations ? "\u5df2\u5bfc\u51fa\u5168\u90e8\u4f1a\u8bdd" : "\u5355\u4f1a\u8bdd\u6821\u9a8c\u901a\u8fc7";
            string message = result.AllConversations
                ? "\u5df2\u5bfc\u51fa\u5168\u90e8\u4f1a\u8bdd\uff08" + result.Audit.Conversations.Count + " \u4e2a\u4f1a\u8bdd ID\uff09\u3002\u8bf7\u52ff\u628a\u8fd9\u4e2a\u5168\u91cf\u6587\u4ef6\u5f53\u6210\u5355\u4eba\u804a\u5929\u3002\n\n" + lastOutputDir
                : "\u5df2\u786e\u8ba4\u8f93\u51fa\u53ea\u5305\u542b 1 \u4e2a\u4f1a\u8bdd ID\u3002\n\n" + lastOutputDir;
            if (!String.IsNullOrWhiteSpace(result.DirectionSummary)) message = message.Replace("\n\n" + lastOutputDir, "\n\n" + result.DirectionSummary + "\n\n" + lastOutputDir);
            else if (result.Audit != null) message = message.Replace("\n\n" + lastOutputDir, "\n\n\u6536\u53d1\u65b9\u5411\u672a\u6807\u6ce8\uff08\u6211\u65b9\u53d1\u8a00\u8005\u672a\u8bbe\u7f6e\uff09\u3002\n\n" + lastOutputDir);
            MessageBox.Show(this, message, result.AllConversations ? "\u5168\u91cf\u5bfc\u51fa\u5b8c\u6210" : "\u5355\u4f1a\u8bdd\u6838\u9a8c\u901a\u8fc7", MessageBoxButtons.OK, result.AllConversations ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
        else
        {
            statusLabel.Text = result.ValidationFailed ? "\u4f1a\u8bdd\u6838\u9a8c\u672a\u901a\u8fc7" : "\u90e8\u5206\u5b8c\u6210";
            string detail = String.IsNullOrWhiteSpace(result.ValidationMessage) ? "\u8bf7\u67e5\u770b export_manifest.json\u3002" : result.ValidationMessage;
            MessageBox.Show(this, "\u7ed3\u679c\u5df2\u4fdd\u5b58\uff0c\u4f46\u672a\u80fd\u786e\u8ba4\u4f1a\u8bdd\u8303\u56f4\u3002\u8bf7\u52ff\u628a\u8fd9\u4efd\u7ed3\u679c\u4f5c\u4e3a\u5355\u4eba\u804a\u5929\u4f7f\u7528\u3002\n\n" + detail + "\n\n" + lastOutputDir, "\u4f1a\u8bdd\u6838\u9a8c\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowFailure(string message)
    {
        statusLabel.Text = "\u5bfc\u51fa\u5931\u8d25";
        AppendLog("\u5bfc\u51fa\u5931\u8d25\uff1a" + message);
        MessageBox.Show(this, message, "\u5bfc\u51fa\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void AppendLog(string message)
    {
        logBox.AppendText(message.TrimEnd() + Environment.NewLine);
    }

    private void OpenOutput()
    {
        if (!String.IsNullOrWhiteSpace(lastOutputDir) && Directory.Exists(lastOutputDir))
        {
            Process.Start(new ProcessStartInfo(lastOutputDir) { UseShellExecute = true });
        }
    }

    private static ExportResult RunExporter(ExportSettings settings)
    {
        string legacyCorePath = EnsureCoreExporter();
        bool instagram = LooksLikeInstagramInput(settings.Input);
        string corePath = instagram ? EnsureInstagramBridge() : legacyCorePath;
        List<string> args = new List<string>();
        AddArgument(args, "--input", settings.Input);
        if (instagram)
        {
            AddArgument(args, "--legacy-core", legacyCorePath);
        }
        AddArgumentIfPresent(args, "--output-dir", settings.OutputDir);
        AddArgumentIfPresent(args, "--conversation", settings.Conversation);
        string[] userSpeakers = SplitSpeakerNames(settings.UserSpeaker);
        if (userSpeakers.Length == 1) AddArgumentIfPresent(args, "--user-speaker", userSpeakers[0]);
        AddArgumentIfPresent(args, "--assistant-speaker", settings.AssistantSpeaker);
        AddArgumentIfPresent(args, "--transcript-map", settings.TranscriptMap);
        AddArgumentIfPresent(args, "--transcriber", settings.Transcriber);
        if (settings.CopyMedia)
        {
            args.Add("--copy-media");
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = corePath;
        startInfo.Arguments = String.Join(" ", args.ToArray());
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        DateTime exportStartedUtc = DateTime.UtcNow;
        using (Process process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new InvalidOperationException("\u65e0\u6cd5\u542f\u52a8\u5bfc\u51fa\u6838\u5fc3\u3002");
            }
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            ExportResult result = new ExportResult
            {
                ExitCode = process.ExitCode,
                Stdout = stdout,
                Stderr = stderr,
                OutputDir = ExtractJsonString(stdout, "output_dir") ?? DefaultOutputDir(settings.Input, settings.OutputDir),
                AllConversations = settings.AllConversations,
                ExportStartedUtc = exportStartedUtc
            };
            if (LooksLikeWeChatInput(settings.Input))
            {
                AuditWeChatResult(result, settings);
                if (!result.ValidationFailed)
                {
                    string[] configuredUserSpeakers = SplitSpeakerNames(settings.UserSpeaker);
                    if (configuredUserSpeakers.Length == 0)
                    {
                        RunDirectionEnricher(result, settings);
                    }
                    else if (CanUseLegacyDirectionEnricher(result, settings))
                    {
                        RunDirectionEnricher(result, settings);
                    }
                    else
                    {
                        result.ValidationMessage = "已跳过旧版逐条方向标注：全量导出、群聊或含系统通知时，旧模块会把不同会话/系统事件误当普通收件人。请以按会话 ID 分开的 conversation_index.json 和 conversation_summary.csv 为准。";
                    }
                }
                if (result.Audit != null)
                {
                    ConversationAuditor.WriteReports(result.OutputDir, result.Audit, settings.Conversation, settings.AllConversations);
                    result.DirectionSummary = result.Audit.DirectionConfigured
                        ? "我方发出 " + result.Audit.OutgoingCount + " 条，私聊收到 " + result.Audit.IncomingCount + " 条，群聊/其他会话他人发言 " + result.Audit.OtherSpeakerCount + " 条，未能识别 " + result.Audit.UnknownCount + " 条（系统事件已排除）"
                        : "方向未推断：未配置我方昵称；系统事件已排除，人物统计按会话 ID 分开";
                }
            }
            return result;
        }
    }

    private static void RunDirectionEnricher(ExportResult result, ExportSettings settings)
    {
        string[] userSpeakers = SplitSpeakerNames(settings.UserSpeaker);
        if (userSpeakers.Length == 0)
        {
            result.ValidationMessage = "\u672a\u586b\u6211\u65b9\u53d1\u8a00\u8005\u540d\u79f0\uff0c\u4e3a\u907f\u514d\u8bef\u6807\u6536\u53d1\u65b9\uff0c\u672c\u6b21\u4fdd\u7559\u539f\u59cb\u53d1\u8a00\u8005\u5e76\u8df3\u8fc7\u65b9\u5411\u63a8\u65ad\u3002";
            return;
        }

        string helperPath = EnsureDirectionEnricher();
        List<string> args = new List<string>();
        AddArgument(args, "--output-dir", result.OutputDir);
        AddArgument(args, "--user-speaker", String.Join(",", userSpeakers));
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = helperPath;
        startInfo.Arguments = String.Join(" ", args.ToArray());
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        using (Process process = Process.Start(startInfo))
        {
            if (process == null) throw new InvalidOperationException("\u65e0\u6cd5\u542f\u52a8\u53d1\u8a00\u65b9\u5411\u6807\u6ce8\u5668\u3002");
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            string status = ExtractJsonString(stdout, "status");
            if (process.ExitCode != 0 || !String.Equals(status, "complete", StringComparison.OrdinalIgnoreCase))
            {
                result.ValidationFailed = true;
                result.ValidationMessage = "\u65b9\u5411\u6807\u6ce8\u5931\u8d25\uff1a" + (String.IsNullOrWhiteSpace(stderr) ? stdout.Trim() : stderr.Trim());
                return;
            }
            string outgoing = ExtractSummaryValue(stdout, "outgoing_count");
            string incoming = ExtractSummaryValue(stdout, "incoming_count");
            string unknown = ExtractSummaryValue(stdout, "unknown_count");
            result.DirectionSummary = "\u6211\u65b9\u53d1\u51fa " + outgoing + " \u6761\uff0c\u6536\u5230 " + incoming + " \u6761\uff0c\u672a\u77e5 " + unknown + " \u6761";
            if (!String.IsNullOrWhiteSpace(stderr)) result.ValidationMessage = stderr.Trim();
        }
    }

    private static bool CanUseLegacyDirectionEnricher(ExportResult result, ExportSettings settings)
    {
        if (settings.AllConversations || result.Audit == null || result.Audit.SystemMessageCount > 0 || result.Audit.Conversations.Count != 1)
            return false;
        foreach (ConversationAuditGroup group in result.Audit.Conversations.Values)
            return String.Equals(group.ConversationType, "private", StringComparison.OrdinalIgnoreCase);
        return false;
    }

    private static string[] SplitSpeakerNames(string names)
    {
        if (String.IsNullOrWhiteSpace(names)) return new string[0];
        List<string> result = new List<string>();
        foreach (string name in names.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = name.Trim();
            if (trimmed.Length > 0 && !result.Contains(trimmed)) result.Add(trimmed);
        }
        return result.ToArray();
    }

    private static void AuditWeChatResult(ExportResult result, ExportSettings settings)
    {
        string transcriptPath = Path.Combine(result.OutputDir, "lossless_transcript.json");
        if (!File.Exists(transcriptPath))
        {
            result.ValidationFailed = true;
            result.ValidationMessage = "\u627e\u4e0d\u5230 lossless_transcript.json\uff0c\u65e0\u6cd5\u6838\u9a8c\u4f1a\u8bdd ID\u3002";
            return;
        }
        if (File.GetLastWriteTimeUtc(transcriptPath) < result.ExportStartedUtc.AddSeconds(-3))
        {
            result.ValidationFailed = true;
            result.ValidationMessage = "\u8f93\u51fa\u76ee\u5f55\u4e2d\u7684 lossless_transcript.json \u6ca1\u6709\u5728\u672c\u6b21\u8fd0\u884c\u65f6\u66f4\u65b0\uff0c\u4e3a\u907f\u514d\u628a\u4e0a\u6b21\u5bfc\u51fa\u8bef\u5f53\u6210\u672c\u6b21\u7ed3\u679c\uff0c\u5df2\u4e2d\u6b62\u901a\u8fc7\u3002";
            return;
        }

        try
        {
            result.Audit = ConversationAuditor.Inspect(transcriptPath, SplitSpeakerNames(settings.UserSpeaker));
            ConversationAuditor.WriteReports(result.OutputDir, result.Audit, settings.Conversation, settings.AllConversations);

            if (result.Audit.MessageCount == 0)
            {
                result.ValidationFailed = true;
                result.ValidationMessage = "\u5f52\u6863\u4e2d\u6ca1\u6709\u53ef\u6838\u9a8c\u7684\u6d88\u606f\u3002";
            }
            else if (result.Audit.MissingConversationIdCount > 0)
            {
                result.ValidationFailed = true;
                result.ValidationMessage = "\u6709 " + result.Audit.MissingConversationIdCount + " \u6761\u6d88\u606f\u7f3a\u5c11\u4f1a\u8bdd ID\uff0c\u8fd9\u4efd\u5bfc\u51fa\u4e0d\u9002\u5408\u5f53\u4f5c\u5355\u4eba\u804a\u5929\u5206\u6790\u3002";
            }
            else if (settings.AllConversations)
            {
                if (!String.IsNullOrWhiteSpace(settings.Conversation))
                {
                    result.ValidationFailed = true;
                    result.ValidationMessage = "\u5168\u91cf\u6a21\u5f0f\u4e0b\u4e0d\u5e94\u8bbe\u7f6e\u5355\u4e2a\u4f1a\u8bdd\u7b5b\u9009\u6761\u4ef6\u3002";
                }
            }
            else if (result.Audit.Conversations.Count != 1)
            {
                result.ValidationFailed = true;
                result.ValidationMessage = "\u8f93\u5165\u4e86\u5355\u4e2a\u4f1a\u8bdd\uff0c\u4f46\u8f93\u51fa\u5305\u542b " + result.Audit.Conversations.Count + " \u4e2a\u4e0d\u540c\u4f1a\u8bdd ID\uff1b\u5efa\u8bae\u6539\u7528\u552f\u4e00\u4f1a\u8bdd ID \u91cd\u8bd5\u3002";
            }
            else if (!result.Audit.ContainsRequestedConversation(settings.Conversation))
            {
                result.ValidationFailed = true;
                result.ValidationMessage = "\u8f93\u51fa\u4f1a\u8bdd\u540d/ID \u4e0e\u8f93\u5165\u7b5b\u9009\u6761\u4ef6\u4e0d\u4e00\u81f4\uff1b\u8bf7\u4f7f\u7528\u5b8c\u6574\u7684\u4f1a\u8bdd\u663e\u793a\u540d\u6216\u4f1a\u8bdd ID\u3002";
            }
        }
        catch (Exception exception)
        {
            result.ValidationFailed = true;
            result.ValidationMessage = "\u626b\u63cf\u4f1a\u8bdd\u5f52\u6863\u65f6\u51fa\u9519\uff1a" + exception.Message;
        }
    }

    private static void AddArgument(List<string> args, string name, string value)
    {
        args.Add(name);
        args.Add(QuoteArgument(value));
    }

    private static void AddArgumentIfPresent(List<string> args, string name, string value)
    {
        if (!String.IsNullOrWhiteSpace(value))
        {
            AddArgument(args, name, value);
        }
    }

    private static string QuoteArgument(string value)
    {
        StringBuilder result = new StringBuilder();
        result.Append('"');
        int slashes = 0;
        foreach (char character in value)
        {
            if (character == '\\')
            {
                slashes++;
            }
            else if (character == '"')
            {
                result.Append('\\', slashes * 2 + 1);
                result.Append('"');
                slashes = 0;
            }
            else
            {
                result.Append('\\', slashes);
                result.Append(character);
                slashes = 0;
            }
        }
        result.Append('\\', slashes * 2);
        result.Append('"');
        return result.ToString();
    }

    private static string DefaultOutputDir(string input, string explicitOutput)
    {
        if (!String.IsNullOrWhiteSpace(explicitOutput))
        {
            return Path.GetFullPath(explicitOutput);
        }
        return Path.Combine(File.Exists(input) ? Path.GetDirectoryName(Path.GetFullPath(input)) : Path.GetFullPath(input), "gpt-export");
    }

    private static string EnsureCoreExporter()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "ChatHistoryGPTExporter");
        Directory.CreateDirectory(tempDir);
        string target = Path.Combine(tempDir, "CoreExporter.exe");
        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("CoreExporter.exe"))
        {
            if (resource == null)
            {
                throw new FileNotFoundException("\u5e94\u7528\u5305\u4e2d\u6ca1\u6709\u5bfc\u51fa\u6838\u5fc3\u3002");
            }
            if (!File.Exists(target) || new FileInfo(target).Length != resource.Length)
            {
                resource.Position = 0;
                string temporary = target + ".tmp";
                using (FileStream output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resource.CopyTo(output);
                }
                File.Copy(temporary, target, true);
                File.Delete(temporary);
            }
        }
        return target;
    }

    private static string EnsureDirectionEnricher()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "ChatHistoryGPTExporter");
        Directory.CreateDirectory(tempDir);
        string target = Path.Combine(tempDir, "DirectionEnricher.exe");
        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("DirectionEnricher.exe"))
        {
            if (resource == null)
            {
                throw new FileNotFoundException("\u5e94\u7528\u5305\u4e2d\u6ca1\u6709\u53d1\u8a00\u65b9\u5411\u6807\u6ce8\u5668\u3002");
            }
            if (!File.Exists(target) || new FileInfo(target).Length != resource.Length)
            {
                resource.Position = 0;
                string temporary = target + ".tmp";
                using (FileStream output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resource.CopyTo(output);
                }
                File.Copy(temporary, target, true);
                File.Delete(temporary);
            }
        }
        return target;
    }

    private static string EnsureInstagramBridge()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "ChatHistoryGPTExporter");
        Directory.CreateDirectory(tempDir);
        string target = Path.Combine(tempDir, "InstagramBridge.exe");
        using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("InstagramBridge.exe"))
        {
            if (resource == null)
            {
                throw new FileNotFoundException("应用包中没有 Instagram 导入模块。");
            }
            if (!File.Exists(target) || new FileInfo(target).Length != resource.Length)
            {
                resource.Position = 0;
                string temporary = target + ".tmp";
                using (FileStream output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resource.CopyTo(output);
                }
                File.Copy(temporary, target, true);
                File.Delete(temporary);
            }
        }
        return target;
    }

    private static bool LooksLikeWeChatInput(string input)
    {
        if (!Directory.Exists(input))
        {
            return false;
        }
        if (HasWeChatDatabaseFolder(input)) return true;
        try
        {
            foreach (string child in Directory.GetDirectories(input, "*", SearchOption.TopDirectoryOnly))
            {
                if (HasWeChatDatabaseFolder(child)) return true;
                foreach (string nested in Directory.GetDirectories(child, "*", SearchOption.TopDirectoryOnly))
                {
                    if (HasWeChatDatabaseFolder(nested)) return true;
                }
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return false;
    }

    private static bool HasWeChatDatabaseFolder(string root)
    {
        return Directory.Exists(Path.Combine(root, "db_storage")) || Directory.Exists(Path.Combine(root, "Msg")) || Directory.Exists(Path.Combine(root, "msg"));
    }

    private static bool LooksLikeInstagramInput(string input)
    {
        string normalised = input.Replace('/', '\\').ToLowerInvariant();
        if (normalised.Contains("instagram"))
        {
            return true;
        }
        if (File.Exists(input))
        {
            string name = Path.GetFileName(input).ToLowerInvariant();
            return name.StartsWith("message_") && (name.EndsWith(".json") || name.EndsWith(".html") || name.EndsWith(".htm"));
        }
        if (!Directory.Exists(input))
        {
            return false;
        }
        foreach (string file in Directory.EnumerateFiles(input, "message_*.json", SearchOption.AllDirectories))
        {
            string path = file.Replace('/', '\\').ToLowerInvariant();
            if (path.Contains("\\messages\\") || path.Contains("\\inbox\\"))
            {
                return true;
            }
        }
        return false;
    }

    private static string ExtractJsonString(string output, string key)
    {
        Match match = Regex.Match(output ?? "", "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"");
        if (!match.Success)
        {
            return null;
        }
        return Regex.Unescape(match.Groups["value"].Value);
    }

    private static string ExtractSummaryValue(string output, string key)
    {
        string value = ExtractJsonString(output, key);
        if (value != null)
        {
            return value;
        }
        Match number = Regex.Match(output ?? "", "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*(?<value>[0-9]+)");
        return number.Success ? number.Groups["value"].Value : "?";
    }
}

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
