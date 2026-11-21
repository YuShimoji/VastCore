param(
  # setup   : コンパイルし、探索シーンを用意し、Unity 経由で鳥瞰図を書き出す（バッチ、画面なし）
  # explore : 通常の Editor で探索シーンを再生し、決まった操作の画面を画像に残して終了する
  # play    : 通常の Editor で探索シーンを開いて再生を始める（人が触る用。終了は待たない）
  [ValidateSet('setup','explore','play')] [string]$Mode = 'setup',
  [int]$Seed = 7,
  [int]$Resolution = 1024,
  [string]$Style = 'satellite',
  [string]$OutDir = 'artifacts/region-map',
  [string]$UnityPath = ''
)

$ErrorActionPreference = 'Stop'
$ProjectPath = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location -LiteralPath $ProjectPath

if (-not $UnityPath) {
  $verLine = Get-Content (Join-Path $ProjectPath 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
  if ($verLine -notmatch 'm_EditorVersion:\s*(?<v>[^\s]+)') { throw 'ProjectVersion.txt から Unity の版を読めない' }
  $UnityPath = "C:\Program Files\Unity\Hub\Editor\$($Matches['v'])\Editor\Unity.exe"
}
if (-not (Test-Path $UnityPath)) { throw "Unity が見つからない: $UnityPath" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
New-Item -ItemType Directory -Force -Path 'artifacts/logs' | Out-Null
$OutFull = (Resolve-Path $OutDir).Path
$log = Join-Path $ProjectPath "artifacts/logs/region-map-$Mode.log"

if ($Mode -eq 'setup') {
  $png = Join-Path $OutFull "unity-region-seed$Seed-$Style.png"
  $unityArgs = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', "`"$ProjectPath`"",
    '-executeMethod', 'Vastcore.RegionMap.Editor.RegionMapMenu.SetupBatch',
    '-regionSeed', $Seed, '-regionRes', $Resolution, '-regionStyle', $Style,
    '-regionOut', "`"$png`"",
    '-logFile', "`"$log`""
  )
} elseif ($Mode -eq 'play') {
  $unityArgs = @(
    '-projectPath', "`"$ProjectPath`"",
    '-executeMethod', 'Vastcore.RegionMap.Editor.RegionExplorerSmoke.PlayFromCommandLine',
    '-logFile', "`"$log`""
  )
  Write-Host "Unity: $UnityPath"
  Write-Host "Log:   $log"
  $started = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -PassThru
  Write-Host "Unity を起動した（PID $($started.Id)）。地域の生成が終わると地図が出る。終了は Unity を閉じる。"
  exit 0
} else {
  $unityArgs = @(
    '-projectPath', "`"$ProjectPath`"",
    '-executeMethod', 'Vastcore.RegionMap.Editor.RegionExplorerSmoke.RunFromCommandLine',
    '-regionOut', "`"$OutFull`"",
    '-logFile', "`"$log`""
  )
}

Write-Host "Unity: $UnityPath"
Write-Host "Log:   $log"
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow
Write-Host "exit code: $($process.ExitCode)"

if (Test-Path $log) {
  Select-String -Path $log -Pattern 'error CS\d+|\[RegionMap\]|Scripts have compiler errors' |
    Select-Object -First 30 | ForEach-Object { Write-Host $_.Line }
}
exit $process.ExitCode
