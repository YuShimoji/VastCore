<#
.SYNOPSIS
    Unity EditMode で ProBuilder CSG API スキャンを実行し、レポートを生成します。

.DESCRIPTION
    Unity Editor をバッチモードで起動し、ProBuilderCsgScannerWindow.RunBatch() を実行して
    ProBuilder CSG API のスキャンレポートを生成します。

.EXAMPLE
    .\scripts\run-csg-scan.ps1
#>

$ErrorActionPreference = "Stop"

# パス設定
$ProjectPath = Split-Path -Parent $PSScriptRoot
$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.2.2f1\Editor\Unity.exe"
$MethodName = "Vastcore.EditorTools.ProBuilderCsgScannerWindow.RunBatch"

# Unity 実行ファイル確認
if (-not (Test-Path $UnityPath)) {
    Write-Error "Unity Editor not found at: $UnityPath"
    exit 1
}

Write-Host "===== Unity ProBuilder CSG API Scan =====" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"
Write-Host "Unity: $UnityPath"
Write-Host "Method: $MethodName"
Write-Host ""

# Unity 実行
$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-executeMethod", $MethodName,
    "-quit"
)

Write-Host "Running Unity CSG scan..." -ForegroundColor Yellow
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow

$exitCode = $process.ExitCode
Write-Host ""
Write-Host "Unity process completed. Exit code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })

# レポートファイル確認
$ReportPath = Join-Path $ProjectPath "docs\CT1_PROBUILDER_CSG_API_SCAN.md"
if (Test-Path $ReportPath) {
    Write-Host ""
    Write-Host "Report generated: $ReportPath" -ForegroundColor Green
    $fileSize = (Get-Item $ReportPath).Length
    Write-Host "File size: $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "[WARN] Report file not found: $ReportPath" -ForegroundColor Yellow
}

Write-Host ""
exit $exitCode
