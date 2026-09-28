# 记录员

English identifier: `Ji Lu Yuan`.

本地聊天记录整理器。当前应用版本支持：

- Windows 微信原有导出核心与已授权的只读数据库导入；
- Instagram 数据下载中的 `messages/inbox/**/message_*.json` 和 HTML；
- 每个 Instagram 线程独立保留 `conversation_id`，分页文件去重，避免相同昵称或不同联系人混在一起；
- 图片、视频、语音、表情包和文件使用稳定占位符，并在勾选复制媒体时复制本地可用文件。

Instagram 数据下载建议选择包含 `messages` 或 `messages/inbox` 的根目录。程序会自动识别每个线程目录，按 `title`、参与者或线程目录名生成会话名称。

输出文件：

- `gpt_request.json`：普通 GPT/ChatGPT 分析首选；
- `gpt_finetune.jsonl`：JSONL 工作流或 API 微调输入；
- `lossless_transcript.json`：带原始记录和会话 ID 的可追溯归档；
- `transcript.md`：人工复核时间线；
- `export_manifest.json`：记录数量、线程数量、去重数和未解析项。

微信导出时，如果没有指定会话，图形应用会先提示确认“导出全部会话”，避免把全账号记录误当成单人聊天。要导出单个联系人或群聊，请填写微信里显示的名称；也支持内部会话 ID。
