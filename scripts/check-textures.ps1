[CmdletBinding()]
param(
    # 検査そのものの試験（tools/TextureAudit の --self-test）
    [switch]$SelfTest,
    # warning も失敗にする
    [switch]$Strict,
    # Unity のバッチで画素の検査（継ぎ目・白飛び・明るさの範囲・照明のむら・法線の向き）も行う。Unity Hub の起動が要り、同じプロジェクトを Editor で開いている間は動かない
    [switch]$Pixels,
    [string]$OutDir = 'artifacts/texture-audit',
    [string]$UnityPath = ''
)

# テクスチャの検査の入口。規則は docs/02_design/texture-rules.json、説明は docs/02_design/TEXTURE_PIPELINE_SPEC.md。
# 終了コード: 0 = 通過、1 = 指摘あり、2 = 実行できない。
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'tools/TextureAudit/TextureAudit.csproj'

if (-not $Pixels) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host 'dotnet が見つからない（.NET SDK 9 以上が要る）。' -ForegroundColor Red
        exit 2
    }
    & dotnet build $project -c Release -v q -nologo | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & dotnet build $project -c Release -nologo
        exit 2
    }
    $arguments = @('run', '--project', $project, '-c', 'Release', '--no-build', '--')
    if ($SelfTest) {
        $arguments += '--self-test'
    }
    else {
        $arguments += @('--root', $repoRoot, '--out', $OutDir)
        if ($Strict) { $arguments += '--strict' }
    }
    & dotnet @arguments
    exit $LASTEXITCODE
}

# 画素の検査（Unity のバッチ）
$unity = $UnityPath
if (-not $unity -and $env:UNITY_PATH) { $unity = $env:UNITY_PATH }
if (-not $unity) {
    $versionLine = Get-Content (Join-Path $repoRoot 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
    if ($versionLine -notmatch 'm_EditorVersion:\s*(?<v>\S+)') { throw 'ProjectVersion.txt から Unity の版を読めない' }
    $unity = "C:\Program Files\Unity\Hub\Editor\$($Matches['v'])\Editor\Unity.exe"
}
if (-not (Test-Path $unity)) {
    Write-Host "Unity が見つからない: $unity（-UnityPath か UNITY_PATH で指定）" -ForegroundColor Red
    exit 2
}
$logDir = Join-Path $repoRoot 'artifacts/logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir 'texture-audit-unity.log'
$unityArgs = @('-batchmode', '-nographics', '-projectPath', "`"$repoRoot`"", '-logFile', "`"$log`"",
    '-executeMethod', 'Vastcore.TexturePipeline.Editor.TextureAuditMenu.RunBatch')
if ($Strict) { $unityArgs += '-strict' }
$process = Start-Process -FilePath $unity -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow
if (Test-Path $log) {
    Get-Content $log | Where-Object { $_ -match '^(ERROR|WARNING) T[XQ]\d{3} |^texture audit' } | ForEach-Object { Write-Host $_ }
}
Write-Host "Unity の終了コード $($process.ExitCode)。ログ: $log"
exit $process.ExitCode
