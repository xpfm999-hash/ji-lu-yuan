# 记录员

记录员（英文名：Ji Lu Yuan）是一个 Windows 桌面程序，用于把已登录电脑微信的数据或 Instagram 数据下载包，一键整理成可直接交给 GPT 分析的文件。

## 当前版本

`v0.3.3`

下载：[记录员-v0.3.3.zip](release/记录员-v0.3.3.zip) · [中文 EXE](release/记录员-v0.3.3.exe) · [English EXE](release/Ji%20Lu%20Yuan-v0.3.3.exe)

## 功能

- 微信：保留原有本地导出与微信 4.x 只读读取能力；微信 4.x 使用时请保持 `Weixin.exe` 已登录。
- Instagram：读取 `messages/inbox` 下的 JSON 分页文件，也兼容 HTML 消息文件。
- 指定会话：微信单聊填写完整显示名称或会话 ID；全量导出必须显式勾选并二次确认。
- 会话核验：导出后流式检查 `conversation_id`、会话名称、消息数和系统事件；单聊若发现多个会话 ID 或筛选不匹配，会标记核验失败，不把混合文件报成单聊。
- 发言归属：填写本人昵称以按别名统计本人发言；历史昵称可用英文逗号分隔。报告按会话 ID 分开，系统/撤回事件不计为普通参与者；群聊中的其他人不误标为单聊来信。未填写时方向保持未知。
- Instagram 隔离：按会话目录、会话 ID 和文件来源隔离消息，跨分页去重，避免把不相干人的记录混入当前会话。
- 媒体：图片、视频、语音、表情包和文件保留 `[图片]`、`[视频]`、`[语音]`、`[表情包]`、`[文件]` 占位符；勾选复制媒体时会复制找到的本地文件。
- 输出：自动生成 GPT 文件、原始归档、`export_manifest.json`、`conversation_audit.json`、`conversation_index.json`、`conversation_summary.csv` 和媒体目录。全量文件须按会话 ID 分开分析，不能当成单段聊天。

## 使用

1. 解压发布包，双击 `记录员.exe`。
2. 选择微信数据目录，或选择 Instagram 数据下载目录。
3. 在“指定会话名/ID”填写完整名称；有重名时使用会话 ID。若要备份全账号，显式勾选“导出全部微信会话”。
4. 若需要区分本人和他人发言，填写“我方发言者”；多个历史昵称用英文逗号分隔，例如 `Aqron,xpfm9`。
5. 选择输出目录并点击“一键导出”。单聊导出完成后确认应用提示的会话 ID 数为 1。
6. 单会话可把 `gpt_request.json` 或 `transcript.md` 发给 GPT；全量导出先按 `conversation_index.json` / `conversation_summary.csv` 选定会话，不要把全量内容当作一段对话。

## 隐私和安全

- 数据在本机处理，不上传聊天内容。
- 微信 4.x 的读取只在本地已登录的 `Weixin.exe` 进程中进行，并只为导出用户自己的聊天记录读取必要信息。
- 程序不会绕过登录，也不会修改微信数据库。
- 发送给 GPT 前请自行确认聊天内容、媒体和隐私信息。

## 仓库结构

- `release/`：可直接使用的发布压缩包。
- `source/`：桌面程序源码、Instagram 解析桥接脚本和构建脚本。
- `plugin/`：对应的 Codex 插件技能和插件清单。

## 构建

需要 Windows .NET Framework C# 编译器、Python 3.12 和 PyInstaller。执行：

```powershell
pwsh -NoProfile -File .\source\build.ps1
```

构建结果为 `source\Ji Lu Yuan.exe`。发布包中的 `记录员.exe` 是同一程序的中文用户文件名；只允许英文字符的环境使用 `Ji Lu Yuan.exe`。

## 说明

微信原始读取和方向标注核心以随发布包提供的兼容组件形式嵌入；不包含用户的聊天数据库、密钥、媒体或测试数据。
