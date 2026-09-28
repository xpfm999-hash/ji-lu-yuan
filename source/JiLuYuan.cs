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
}

internal sealed class ExportResult
{
    public int ExitCode;
    public string Stdout;
    public string Stderr;
    public string OutputDir;
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
    private readonly TextBox logBox = new TextBox();
    private readonly Label statusLabel = new Label();
    private readonly Button exportButton = new Button();
    private readonly Button openButton = new Button();
    private string lastOutputDir = "";

    public MainForm()
    {
        Text = "\u8bb0\u5f55\u5458 - \u804a\u5929\u8bb0\u5f55\u5bfc\u51fa\u5668";
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
        title.Text = "\u8bb0\u5f55\u5458 - \u804a\u5929\u8bb0\u5f55\u5bfc\u51fa\u5668";
        title.Font = new Font(Font, FontStyle.Bold);
        title.Font = new Font(title.Font.FontFamily, 18F, FontStyle.Bold);
        title.AutoSize = true;
        root.Controls.Add(title, 0, row);
        root.SetColumnSpan(title, 3);
        row++;

        Label description = new Label();
        description.Text = "\u53ef\u76f4\u63a5\u9009\u62e9\u7535\u8111\u5fae\u4fe1\u6216 Instagram \u6570\u636e\u6587\u4ef6\u5939\uff0c\u4e00\u6b21\u5bfc\u51fa GPT \u6587\u4ef6\u3001\u539f\u59cb\u5f52\u6863\u3001\u5a92\u4f53\u6587\u4ef6\u548c\u5b8c\u6574\u6027\u62a5\u544a\u3002\u4e5f\u53ef\u8bfb\u53d6 TXT/JSON/HTML \u7b49\u5df2\u5bfc\u51fa\u6587\u4ef6\u3002\u5fae\u4fe1 4.x \u8bf7\u4fdd\u6301 Weixin.exe \u5df2\u767b\u5f55\uff1b\u5fae\u4fe1\u672a\u6307\u5b9a\u4f1a\u8bdd\u65f6\u4f1a\u5148\u63d0\u793a\uff0c\u9632\u6b62\u4e32\u5165\u5176\u4ed6\u8054\u7cfb\u4eba\u3002";
        description.AutoSize = true;
        description.MaximumSize = new Size(780, 0);
        root.Controls.Add(description, 0, row);
        root.SetColumnSpan(description, 3);
        row++;

        AddField(root, ref row, "\u804a\u5929\u6587\u4ef6/\u6587\u4ef6\u5939", inputBox, MakeInputButtons());
        AddField(root, ref row, "\u8f93\u51fa\u76ee\u5f55\uff08\u53ef\u9009\uff09", outputBox, new Button[] { MakeButton("\u9009\u62e9\u76ee\u5f55", ChooseOutputFolder) });
        AddField(root, ref row, "\u6307\u5b9a\u4f1a\u8bdd\uff08\u53ef\u9009\uff09", conversationBox, new Button[0]);

        Label roleLabel = MakeLabel("\u89d2\u8272\u6620\u5c04\uff08\u53ef\u9009\uff09");
        root.Controls.Add(roleLabel, 0, row);
        TableLayoutPanel roles = new TableLayoutPanel();
        roles.Dock = DockStyle.Fill;
        roles.AutoSize = true;
        roles.ColumnCount = 4;
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        roles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        roles.Controls.Add(MakeLabel("\u6211\u7684\u540d\u79f0"), 0, 0);
        roles.Controls.Add(userSpeakerBox, 1, 0);
        roles.Controls.Add(MakeLabel("\u5bf9\u65b9\u540d\u79f0"), 2, 0);
        roles.Controls.Add(assistantSpeakerBox, 3, 0);
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

        if (LooksLikeWeChatInput(input) && String.IsNullOrWhiteSpace(settings.Conversation))
        {
            DialogResult decision = MessageBox.Show(this, "你没有填写指定会话。继续将导出这个微信目录下的全部会话，可能包含其他联系人的聊天记录。\n\n选择“否”返回填写会话名称。", "防止串聊", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (decision != DialogResult.Yes)
            {
                statusLabel.Text = "请填写指定会话后再导出";
                return;
            }
            AppendLog("警告：未指定微信会话，本次将导出全部会话。");
        }

        SetBusy(true);
        AppendLog("\u5f00\u59cb\u5904\u7406\uff1a" + input);
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
        AppendLog("\u5b8c\u6574\u6027\uff1a" + (result.ExitCode == 0 ? "\u5b8c\u6210" : "\u90e8\u5206\u5b8c\u6210\uff0c\u8bf7\u67e5\u770b export_manifest.json"));
        if (!String.IsNullOrWhiteSpace(result.Stderr))
        {
            AppendLog(result.Stderr.Trim());
        }
        if (result.ExitCode == 0)
        {
            statusLabel.Text = "\u5bfc\u51fa\u5b8c\u6210";
            MessageBox.Show(this, "\u5df2\u751f\u6210 GPT \u6587\u4ef6\u548c\u5b8c\u6574\u5f52\u6863\uff1a\n" + lastOutputDir, "\u5bfc\u51fa\u5b8c\u6210", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            statusLabel.Text = "\u90e8\u5206\u5b8c\u6210";
            MessageBox.Show(this, "\u7ed3\u679c\u5df2\u4fdd\u5b58\uff0c\u4f46\u6709\u6587\u4ef6\u672a\u80fd\u89e3\u6790\uff1a\n" + lastOutputDir, "\u8bf7\u68c0\u67e5\u5b8c\u6574\u6027", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        AddArgumentIfPresent(args, "--user-speaker", settings.UserSpeaker);
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
        using (Process process = Process.Start(startInfo))
        {
            if (process == null)
            {
                throw new InvalidOperationException("\u65e0\u6cd5\u542f\u52a8\u5bfc\u51fa\u6838\u5fc3\u3002");
            }
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new ExportResult
            {
                ExitCode = process.ExitCode,
                Stdout = stdout,
                Stderr = stderr,
                OutputDir = ExtractJsonString(stdout, "output_dir") ?? DefaultOutputDir(settings.Input, settings.OutputDir)
            };
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
        return Directory.Exists(Path.Combine(input, "db_storage")) || Directory.Exists(Path.Combine(input, "Msg")) || Directory.Exists(Path.Combine(input, "msg"));
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
