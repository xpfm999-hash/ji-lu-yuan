# 记录员

记录员（英文名：Ji Lu Yuan）是一个 Windows 桌面程序，用于把已登录电脑微信的数据或 Instagram 数据下载包，一键整理成可直接交给 GPT 分析的文件。

## 当前版本

`v0.3.1`

## 功能

- 微信：保留原有本地导出与微信 4.x 只读读取能力；微信 4.x 使用时请保持 `Weixin.exe` 已登录。
- Instagram：读取 `messages/inbox` 下的 JSON 分页文件，也兼容 HTML 消息文件。
- 指定会话：填写微信显示名称，或 Instagram 对话参与者/线程名称；留空 Instagram 会导出全部线程，留空微信会先提示可能包含其他联系人。
- 混聊隔离：按会话目录、会话 ID 和文件来源隔离消息，跨分页去重，避免把不相干人的记录混入当前会话。
- 媒体：图片、视频、语音、表情包和文件保留 `[图片]`、`[视频]`、`[语音]`、`[表情包]`、`[文件]` 占位符；勾选复制媒体时会复制找到的本地文件。
- 输出：自动生成 `gpt_request.json`、`gpt_finetune.jsonl`、`transcript.md`、`lossless_transcript.json`、`export_manifest.json` 和 `media` 目录。

## 使用

1. 解压发布包，双击 `记录员.exe`。
2. 选择微信数据目录，或选择 Instagram 数据下载目录。
3. 在“指定会话”填写要导出的对话名称；不确定时先不要留空，以免导出多个会话。
4. 选择输出目录并点击“一键导出”。
5. 直接把 `gpt_request.json` 或 `transcript.md` 发给 GPT；需要保留原始证据时一并保存整个输出目录。

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

微信原始读取核心以随发布包提供的只读兼容组件形式嵌入，不包含用户的聊天数据库、密钥、媒体或测试数据。
