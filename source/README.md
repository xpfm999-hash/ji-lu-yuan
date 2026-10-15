# 构建说明

`JiLuYuan.cs` 是 Windows 图形界面源码；`ConversationAuditor.cs` 对微信导出结果按会话 ID 流式核验，并把系统事件与普通发言者分开统计，生成三个始终可用的报告。测试入口位于 `tests/ConversationAuditorTests.cs`。

`legacy-core/LegacyCoreExporter.exe` 是只读兼容读取核心，`legacy-core/DirectionEnricher.exe` 是旧版逐条方向兼容组件。为避免把群聊其他参与者或系统通知错误标注为私聊收件人，主程序仅在单个、无系统事件的私聊中调用该组件；其他场景用审计器生成按 ID 隔离的索引与摘要。`plugin/scripts/instagram_bridge.py` 负责 Instagram 数据下载包解析、线程隔离、跨分页去重和媒体占位符转换。

在包含 Python 3.12、PyInstaller 和 .NET Framework C# 编译器的 Windows 环境中执行：

```powershell
pwsh -NoProfile -File .\build.ps1
```

构建后的单会话导出会检查只有一个 `conversation_id`。三个报告始终生成；全量导出按会话 ID 分开统计。若不填写我方发言者昵称，方向留为未知，不会把所有消息默认为收到。
