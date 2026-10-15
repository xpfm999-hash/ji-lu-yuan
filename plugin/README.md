# 记录员

English identifier: `Ji Lu Yuan`.

本地聊天记录整理器。当前应用版本支持：

- Windows 微信原有导出核心与已授权的只读数据库导入；
- Instagram 数据下载中的 `messages/inbox/**/message_*.json` 和 HTML；
- 每个 Instagram 线程独立保留 `conversation_id`，分页文件去重，避免相同昵称或不同联系人混在一起；
- 微信单会话导出后核对 `conversation_id`，全量导出需要显式确认；
- 按会话 ID 隔离索引与摘要；本人昵称支持多个历史昵称，系统/撤回事件不计为普通参与者；群聊他人发言不误标为私聊来信；
- 旧版逐条方向组件仅在无系统事件的单私聊中运行；其他场景仍生成可信的分会话报告，不作危险推断；
- 图片、视频、语音、表情包和文件使用稳定占位符，并在勾选复制媒体时复制本地可用文件。

Instagram 数据下载建议选择包含 `messages` 或 `messages/inbox` 的根目录。程序会自动识别每个线程目录，按 `title`、参与者或线程目录名生成会话名称。

输出文件：

- `gpt_request.json`：普通 GPT/ChatGPT 分析首选；
- `gpt_finetune.jsonl`：JSONL 工作流或 API 微调输入；
- `lossless_transcript.json`：带原始记录和会话 ID 的可追溯归档；
- `transcript.md`：人工复核时间线；
- `export_manifest.json`：记录数量、线程数量、去重数和未解析项；
- `conversation_audit.json`：核验导出范围；
- `conversation_index.json` / `conversation_summary.csv`：始终生成，按 `conversation_id` 展示会话和真人发言者计数；系统事件另计。

微信导出时，单聊请填写完整会话名称；同名联系人建议填写内部会话 ID。全量导出需显式勾选并二次确认，且会报告会话 ID 数量。填写“我方发言者”可按本人昵称统计发言；多个历史昵称以英文逗号分隔。留空时方向未知。分析全量导出时，先按索引选定一个会话，不要把多个人的对话混成一段。
