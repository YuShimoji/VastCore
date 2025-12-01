<#
.SYNOPSIS
    Unity EditMode/PlayMode テスト実行スクリプト

.DESCRIPTION
    Unity Editor をバッチモードで起動し、テストを実行します。
    テスト結果は artifacts/test-results/ に XML 形式で出力されます。

.PARAMETER TestMode
    テストモード: editmode または playmode

.EXAMPLE
    .\scripts\run-tests.ps1 -TestMode editmode
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("editmode", "playmode")]
    [string]$TestMode
)

$ErrorActionPreference = "Stop"

# パス設定
$ProjectPath = Split-Path -Parent $PSScriptRoot
$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.2.2f1\Editor\Unity.exe"
$ArtifactsDir = Join-Path $ProjectPath "artifacts"
$TestResultsDir = Join-Path $ArtifactsDir "test-results"
$LogsDir = Join-Path $ArtifactsDir "logs"
$ResultFile = Join-Path $TestResultsDir "$TestMode-results.xml"
$LogFile = Join-Path $LogsDir "$TestMode.log"

# ディレクトリ作成
New-Item -ItemType Directory -Force -Path $TestResultsDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null

# Unity 実行ファイル確認
if (-not (Test-Path $UnityPath)) {
    Write-Error "Unity Editor not found at: $UnityPath"
    exit 1
}

Write-Host "===== Unity $TestMode Test Runner =====" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath"
Write-Host "Unity: $UnityPath"
Write-Host "Results: $ResultFile"
Write-Host "Log: $LogFile"
Write-Host ""

# テスト実行
$unityArgs = @(
    "-batchmode",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", $TestMode,
    "-testResults", $ResultFile,
    "-logFile", $LogFile
)

Write-Host "Running Unity tests..." -ForegroundColor Yellow
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow

$exitCode = $process.ExitCode
Write-Host ""
Write-Host "Unity process completed. Exit code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })

# 結果解析
if (Test-Path $ResultFile) {
    Write-Host ""
    Write-Host "===== Test Results Summary =====" -ForegroundColor Cyan
    
    [xml]$results = Get-Content $ResultFile
    $testRun = $results.'test-run'
    
    $total = $testRun.total
    $passed = $testRun.passed
    $failed = $testRun.failed
    $skipped = $testRun.skipped
    $duration = $testRun.duration
    
    Write-Host "Total:   $total"
    Write-Host "Passed:  $passed" -ForegroundColor Green
    Write-Host "Failed:  $failed" -ForegroundColor $(if ([int]$failed -gt 0) { "Red" } else { "Green" })
    Write-Host "Skipped: $skipped" -ForegroundColor Yellow
    Write-Host "Duration: ${duration}s"
    
    # 失敗したテストの詳細
    if ([int]$failed -gt 0) {
        Write-Host ""
        Write-Host "===== Failed Tests =====" -ForegroundColor Red
        $failedTests = $results.SelectNodes("//test-case[@result='Failed']")
        foreach ($test in $failedTests) {
            Write-Host "  - $($test.fullname)" -ForegroundColor Red
            if ($test.failure.message) {
                Write-Host "    Message: $($test.failure.message)" -ForegroundColor DarkRed
            }
        }
    }
} else {
    Write-Host ""
    Write-Host "[WARN] Test results file not generated: $ResultFile" -ForegroundColor Yellow
    Write-Host "Check log file for details: $LogFile"
}

Write-Host ""
exit $exitCode
