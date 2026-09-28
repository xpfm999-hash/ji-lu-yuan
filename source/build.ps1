$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonPath = if ($env:CHAT_EXPORT_PYTHON) {
    $env:CHAT_EXPORT_PYTHON
} else {
    "C:\Users\qq070\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
}
$cscPath = if ($env:CHAT_EXPORT_CSC) {
    $env:CHAT_EXPORT_CSC
} else {
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
}
$bridgeSource = (Resolve-Path (Join-Path $appRoot "..\plugin\scripts\instagram_bridge.py")).Path
$legacyExe = Join-Path $appRoot "legacy-core\LegacyCoreExporter.exe"
$sourcePath = Join-Path $appRoot "JiLuYuan.cs"
$buildPath = Join-Path $appRoot "..\build"
$bridgeDistPath = Join-Path $buildPath "instagram-dist"
$bridgeWorkPath = Join-Path $buildPath "instagram-work"
$exePath = Join-Path $appRoot "Ji Lu Yuan.exe"

if (-not (Test-Path -LiteralPath $pythonPath)) { throw "Python was not found: $pythonPath" }
if (-not (Test-Path -LiteralPath $cscPath)) { throw "C# compiler was not found: $cscPath" }
if (-not (Test-Path -LiteralPath $bridgeSource)) { throw "Instagram bridge was not found: $bridgeSource" }
if (-not (Test-Path -LiteralPath $legacyExe)) { throw "Legacy exporter was not found: $legacyExe" }
if (-not (Test-Path -LiteralPath $sourcePath)) { throw "Application source was not found: $sourcePath" }

New-Item -ItemType Directory -Force $bridgeDistPath | Out-Null
New-Item -ItemType Directory -Force $bridgeWorkPath | Out-Null
& $pythonPath -m PyInstaller --noconfirm --clean --onefile --console `
    --name "InstagramBridge" `
    --distpath $bridgeDistPath `
    --workpath $bridgeWorkPath `
    --specpath $buildPath `
    $bridgeSource
if ($LASTEXITCODE -ne 0) { throw "Instagram bridge build failed with exit code $LASTEXITCODE" }

$bridgeExe = Join-Path $bridgeDistPath "InstagramBridge.exe"
if (-not (Test-Path -LiteralPath $bridgeExe)) { throw "Instagram bridge was not built: $bridgeExe" }

$legacyResource = "/resource:{0},CoreExporter.exe" -f $legacyExe
$bridgeResource = "/resource:{0},InstagramBridge.exe" -f $bridgeExe
& $cscPath /nologo /target:winexe /platform:x64 /optimize+ /out:$exePath `
    /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll `
    $legacyResource $bridgeResource $sourcePath
if ($LASTEXITCODE -ne 0) { throw "GUI application build failed with exit code $LASTEXITCODE" }

if (-not (Test-Path -LiteralPath $exePath)) { throw "Application EXE was not built: $exePath" }
Write-Host "Build complete: $exePath"
