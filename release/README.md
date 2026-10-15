# 记录员

这是独立的 Windows 图形应用，不需要安装 Codex 插件或 Python。

当前迭代：`v0.3.3`。本版重点加强微信会话隔离核验，避免把全账号导出误当成单人私聊。

使用方式：

1. 双击 `记录员.exe`；英文环境或只允许英文文件名时使用 `Ji Lu Yuan.exe`。
2. 微信用户选择电脑微信数据目录；Instagram 用户选择包含 `messages` 或 `messages/inbox` 的数据下载目录。
3. 导出某个微信联系人或群聊时，在“指定会话名/ID”填写完整显示名称；如果有重名联系人，优先填写会话 ID。留空时默认不能导出；确实需要全量备份时，勾选“我确认要导出全部微信会话”并再次确认。
4. 点击“一键导出”，再点击“打开输出目录”。

Instagram 导出会按线程目录和 `conversation_id` 隔离消息，自动合并 `message_1.json`、`message_2.json` 等分页并去重。图片、视频、语音、表情包会保留占位符；勾选复制媒体时会复制本地存在的文件。

输出包括 `gpt_request.json`、`gpt_finetune.jsonl`、`lossless_transcript.json`、`transcript.md`、`conversation_audit.json`、`conversation_index.json`、`conversation_summary.csv`、`media` 和 `export_manifest.json`。全量导出时，索引和 CSV 按 `conversation_id` 分开列出每段会话，不能把全量文件当成一段私聊交给 GPT。

微信 4.x 仍使用原有只读数据库读取核心：请保持 `Weixin.exe` 已登录。要统计收发方向，在“我方发言者”填导出记录中显示的本人昵称；多个历史昵称用英文逗号分隔，例如 `Aqron,xpfm9`。程序按会话 ID 分组；私聊中的非本人消息计为收到，群聊/未识别会话中的非本人消息单列为“他人发言”。系统通知、撤回等事件单独统计，不进入人名或收发方向。

程序始终流式扫描 `lossless_transcript.json` 并生成 `conversation_audit.json`（范围与完整性）、`conversation_index.json`（按会话 ID 拆分的 JSON 索引）和 `conversation_summary.csv`（表格摘要）。为避免旧方向组件混淆标签，只有“单个私聊且不含系统事件”时才为逐条消息添加方向字段；全量导出、群聊或含系统事件时会跳过旧组件，但按会话索引/摘要仍会生成。未填本人昵称时，方向保持未知，不猜。

单会话模式若发现多个会话 ID、筛选名称不匹配、缺少会话 ID，或归档不是本次新生成的，会明确标记核验失败；文件会保留，但不要把它当作已确认的单人聊天。全量模式会报告会话 ID 数量并提示结果包含多个人。系统消息、撤回等事件单独计数，不会被当成普通聊天参与者。原始 `speaker` 和 `raw` 仍保留，方向字段是附加标注，不会覆盖原始证据。

GPT 角色映射只用于 `user` / `assistant` 角色，不会改写原始发言者名称。当前微信数据库读取核心仍是随包提供的兼容二进制组件；本版在外层增加会话范围和方向标注，不会猜测或静默合并历史昵称。

如果需要重新编译，双击 `build.bat`。构建脚本会保留旧微信核心，同时编译并嵌入 Instagram 导入桥接模块。
