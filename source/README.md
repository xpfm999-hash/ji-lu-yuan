# 构建说明

`JiLuYuan.cs` 是 Windows 图形界面源码。`instagram_bridge.py` 负责 Instagram 数据下载包的解析、会话隔离、跨分页去重和媒体占位符转换。

`legacy-core/LegacyCoreExporter.exe` 是旧版只读兼容核心，构建时会和 Instagram 桥接程序一起嵌入主程序。

在包含 Python 3.12、PyInstaller 和 .NET Framework C# 编译器的 Windows 环境中执行：

```powershell
pwsh -NoProfile -File .\build.ps1
```
