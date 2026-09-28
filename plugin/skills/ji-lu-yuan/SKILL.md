---
name: ji-lu-yuan
description: Export user-provided WeChat, Instagram, or ordinary chat logs into session-isolated GPT-friendly files with media placeholders and integrity reports.
---

# 记录员

使用本地应用处理用户自己的聊天记录。Instagram 导出目录优先按线程目录和 `conversation_id` 隔离；`message_1.json`、`message_2.json` 等分页文件在同一线程内去重，不得把相同昵称的不同线程合并。

微信导出必须明确会话范围。若用户未提供会话名称，应用应提示其确认是否真的要导出全部会话；不能静默把全账号数据报告为单人聊天。

主要输出：

- `gpt_request.json`：GPT/ChatGPT 分析首选；
- `lossless_transcript.json`：用于追溯原始发送者、会话 ID、媒体和未解析项；
- `export_manifest.json`：检查 `message_count`、`duplicate_records_removed`、线程数和 `status`。

媒体只在本地存在时复制；正文仍使用 `[图片]`、`[视频]`、`[语音]`、`[表情包]` 等占位符。语音转写属于可选增强，没有转写器时不得把空文本当成成功转写。
