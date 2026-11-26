param(
  # setup   : コンパイルし、探索シーンを用意し、Unity 経由で鳥瞰図を書き出す（バッチ、画面なし）
  # explore : 通常の Editor で探索シーンを再生し、決まった操作の画面を画像に残して終了する
  # play    : 通常の Editor で探索シーンを開いて再生を始める（人が触る用。終了は待たない）
  # view3d  : 砂岩の台地の 3D 表示（見え方の想定 docs/02_design/REGION_TERRAIN_LOOK_SPEC.md と見比べる場面）を Unity で作り、
  #           決まった 3 つの視点の画像 look-far.png・look-mid.png・look-near.png を <OutDir> へ書き出す（バッチ。-Seed を書かなければ seed 9）
  # view3d-cave : 崖と洞窟（局所の体積の試作）の 3 つの視点の画像を <OutDir> へ書き出す（バッチ）
  [ValidateSet('setup','explore','play','view3d','view3d-cave')] [string]$Mode = 'setup',
  [int]$Seed = 7,
  [int]$Resolution = 1024,
  [string]$Style = 'satellite',
  [string]$OutDir = 'artifacts/region-map',
  # 現代試作（2都市・幹線・空港）を載せる。
  #   setup   : 鳥瞰図と 4 か所の拡大図を書き出す
  #   explore : 街を ×64〜×512 まで拡大した画面を撮る（通常の操作の手順の代わり）
  #   play    : 現代試作の地域で探索シーンを開く（シーンは保存しない）
  # explore / play では、-Seed を書いた時だけその seed に替える（書かなければシーンの seed のまま）
  [switch]$ModernDevelopment,
  # explore の時だけ: 開始直後の画面を、この秒数ぶん連続で <OutDir>/clip/ へ撮る（雲の流れを動画にする材料）
  [int]$ClipSeconds = 0,
  # view3d: 地図上の中心 X,Y、アルコーブの前後・裾から棚への連続画像
  [string]$LookCenter = '',
  [switch]$AlcoveReview,
  # view3d: 光と空気の案（clear・golden・dust）。コンマで並べると案ごとに <OutDir>/<案> へ撮る
  [string]$LookPreset = '',
  # view3d: 場面を作り直さずに太陽だけを回して中景を撮る高さ（度）の並び。例 '60,38,20,10'
  [string]$SunSweep = '',
  # view3d: 表面の凹凸を切った絵（<OutDir>/relief-off）と、切・入の描画時間（relief-timing.txt）も撮る
  [switch]$ReliefCompare,
  # view3d: 表面の写真素材を切った絵（<OutDir>/surface-off）と、切・入の描画時間（surface-timing.txt）も撮る
  [switch]$SurfaceCompare,
  # view3d: 高さによる層の混合を切った絵（<OutDir>/blend-off）と描画時間（blend-timing.txt）も撮る。-BlendDepth は境の深さ（m）
  [switch]$BlendCompare,
  [string]$BlendDepth = '',
  [string]$UnityPath = ''
)

$ErrorActionPreference = 'Stop'
$ProjectPath = (Resolve-Path "$PSScriptRoot/..").Path
# explore / play で探索シーンの地域を替える引数（RegionExplorerSmoke.ApplyRegionArguments が読む）
$regionArgs = @()
if ($PSBoundParameters.ContainsKey('Seed')) { $regionArgs += @('-regionSeed', $Seed) }
if ($ModernDevelopment) { $regionArgs += @('-regionModern', 'true') }
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
  $kind = if ($ModernDevelopment) { 'modern' } else { 'region' }
  $png = Join-Path $OutFull "unity-$kind-seed$Seed-$Style.png"
  $unityArgs = @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', "`"$ProjectPath`"",
    '-executeMethod', 'Vastcore.RegionMap.Editor.RegionMapMenu.SetupBatch',
    '-regionSeed', $Seed, '-regionRes', $Resolution, '-regionStyle', $Style,
    '-regionOut', "`"$png`"",
    '-logFile', "`"$log`""
  )
  if ($ModernDevelopment) { $unityArgs += @('-regionModern', 'true', '-regionDevelopmentDetails', 'true') }
} elseif ($Mode -eq 'view3d' -or $Mode -eq 'view3d-cave') {
  # 画面を描くので -nographics は付けない
  $method = if ($Mode -eq 'view3d') { 'CaptureFromCommandLine' } else { 'CaptureCaveFromCommandLine' }
  $unityArgs = @(
    '-batchmode', '-quit',
    '-projectPath', "`"$ProjectPath`"",
    '-executeMethod', "Vastcore.RegionMap.Editor.RegionTerrainViewMenu.$method",
    '-regionOut', "`"$OutFull`"",
    '-logFile', "`"$log`""
  ) + $regionArgs
  if ($Mode -eq 'view3d') {
    if ($LookCenter) { $unityArgs += @('-lookCenter', $LookCenter) }
    if ($AlcoveReview) { $unityArgs += @('-alcoveReview', 'true') }
    if ($LookPreset) { $unityArgs += @('-lookPreset', $LookPreset) }
    if ($SunSweep) { $unityArgs += @('-sunSweep', $SunSweep) }
    if ($ReliefCompare) { $unityArgs += @('-reliefCompare', 'true') }
    if ($SurfaceCompare) { $unityArgs += @('-surfaceCompare', 'true') }
    if ($BlendCompare) { $unityArgs += @('-blendCompare', 'true') }
    if ($BlendDepth) { $unityArgs += @('-blendDepth', $BlendDepth) }
  }
} elseif ($Mode -eq 'play') {
  $unityArgs = @(
    '-projectPath', "`"$ProjectPath`"",
    '-executeMethod', 'Vastcore.RegionMap.Editor.RegionExplorerSmoke.PlayFromCommandLine',
    '-logFile', "`"$log`""
  ) + $regionArgs
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
    '-regionClip', $ClipSeconds,
    '-logFile', "`"$log`""
  ) + $regionArgs
}

Write-Host "Unity: $UnityPath"
Write-Host "Log:   $log"
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow
Write-Host "exit code: $($process.ExitCode)"

if (Test-Path $log) {
  if ($Mode -in 'view3d', 'view3d-cave') {
    $shaderFailure = Select-String -LiteralPath $log -Pattern "Shader error in 'Vastcore/Region(Terrain|Plants)'" | Select-Object -First 1
    if ($shaderFailure) { throw $shaderFailure.Line }
  }
  Select-String -Path $log -Pattern 'error CS\d+|\[RegionMap\]|Scripts have compiler errors' |
    Select-Object -First 30 | ForEach-Object { Write-Host $_.Line }
}
exit $process.ExitCode
