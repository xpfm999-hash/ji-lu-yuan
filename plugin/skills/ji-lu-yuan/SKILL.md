---
name: ji-lu-yuan
description: Export user-provided WeChat, Instagram, or ordinary chat logs into session-isolated GPT-friendly files with media placeholders and integrity reports.
---

# 记录员

使用本地应用处理用户自己的聊天记录。Instagram 导出目录优先按线程目录和 `conversation_id` 隔离；`message_1.json`、`message_2.json` 等分页文件在同一线程内去重，不得把相同昵称的不同线程合并。

微信单会话导出必须检查最终归档是否只有一个 `conversation_id`，并核对筛选名称/ID。全量导出需要显式勾选并二次确认；不能静默把全账号数据报告为单人聊天。系统/撤回事件应与普通发言者分开统计。

只有用户填写本人发言者名称后才能推断方向。支持英文逗号分隔的历史昵称；未填写时方向为未知。全量导出、群聊或含系统通知时不要调用旧版逐条方向组件；群聊他人发言不能误标为私聊来信。

主要输出：

- `gpt_request.json`：GPT/ChatGPT 分析首选；
- `lossless_transcript.json`：用于追溯原始发送者、会话 ID、媒体和未解析项；
- `export_manifest.json`：检查 `message_count`、`duplicate_records_removed`、线程数和 `status`；
- `conversation_audit.json`：核验会话 ID 和系统事件；
- `conversation_summary.csv` / `conversation_index.json`：始终生成，按会话 ID 隔离展示真人发言者和方向统计；系统事件单列。

媒体只在本地存在时复制；正文仍使用 `[图片]`、`[视频]`、`[语音]`、`[表情包]` 等占位符。语音转写属于可选增强，没有转写器时不得把空文本当成成功转写。
