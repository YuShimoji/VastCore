param(
  # setup   : コンパイルし、探索シーンを用意し、Unity 経由で鳥瞰図を書き出す（バッチ、画面なし）
  # explore : 通常の Editor で探索シーンを再生し、決まった操作の画面を画像に残して終了する
  # play    : 通常の Editor で探索シーンを開いて再生を始める（人が触る用。終了は待たない）
  # view3d  : 砂岩の台地の 3D 表示（見え方の想定 docs/02_design/REGION_TERRAIN_LOOK_SPEC.md と見比べる場面）を Unity で作り、
  #           決まった 3 つの視点の画像 look-far.png・look-mid.png・look-near.png を <OutDir> へ書き出す（バッチ。-Seed を書かなければ seed 9）
  # view3d-cave : 崖と洞窟（局所の体積の試作）の 3 つの視点の画像を <OutDir> へ書き出す（バッチ）
  # build-windows : 探索シーンから起動する Windows x64 Player を <OutDir> にビルドする
  [ValidateSet('setup','explore','play','view3d','view3d-cave','build-windows')] [string]$Mode = 'setup',
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
  # view3d: 写真素材の繰り返しの崩しを切った絵（<OutDir>/tilebreak-off）と描画時間（tilebreak-timing.txt）も撮る
  [switch]$TileBreakCompare,
  # view3d: 高さによる層の混合を切った絵（<OutDir>/blend-off）と描画時間（blend-timing.txt）も撮る。-BlendDepth は境の深さ（m）
  [switch]$BlendCompare,
  [string]$BlendDepth = '',
  # view3d: 土砂の場の強さ（RegionLookSpec.Sediment、既定 1）。0 で T3 より前の地形と、層の無い絵
  [string]$Sediment = '',
  # view3d: 土砂の層を切った絵（<OutDir>/sediment-off）と、切・入の描画時間（sediment-timing.txt）も撮る（地形は同じ）
  [switch]$SedimentCompare,
  # view3d: 岩と地面のなじませと分級（T4、既定 入）。'false' で T3 までの岩の置き方と描き方
  [string]$RockGround = '',
  # view3d: 岩の下の方を地面の色へつなぐ描き方を切った絵（<OutDir>/rockground-off）と描画時間（rockground-timing.txt）も撮る（岩と地形は同じ）
  [switch]$RockGroundCompare,
  # view3d: DS-011 P0 Props Proof Slice の前後比較（props-near-before/after, props-crates, props-rubble）
  [switch]$PropsCompare,
  [switch]$PropsProof,
  [switch]$GrasslandSample,
  [switch]$GrasslandGround,
  [string]$LookBiome = '',
  [switch]$GrasslandCoverOff,
  [switch]$GrasslandTiming,
  # view3d: 表面の写真素材の候補を撮り比べる（T5）。候補の一覧（JSON）を渡すと、-SunAltitudes の太陽の高さごとに場面を作り、
  #         素材なしと各候補（1 役ずつ差し替え）の固定の視点を <OutDir>/sun<高さ>/<候補>/ へ撮る（RegionSurfaceCandidates）。
  #         -CandidateViews search は役が大きく写る近い視点を探して撮り、-Strengths '1:0,2:0.5' は写真の使い方（明暗の強さ:色を混ぜる割合）を並べる
  [string]$SurfaceCandidates = '',
  [string]$SunAltitudes = '',
  [string]$CandidateViews = '',
  [string]$Strengths = '',
  # view3d: 置かれた素材（Surfaces）のまま、-SunAltitudes の太陽の高さごとに固定の視点を <OutDir>/sun<高さ>/fixed/<名前>/ へ、
  #         -ViewsFrom の views.json（候補の撮影が選んだ近い視点）を <OutDir>/sun<高さ>/<役>/v<番号>/<名前>.png へ撮る（素材を置く前後の対）
  [string]$SurfaceCurrent = '',
  [string]$ViewsFrom = '',
  # -SurfaceCurrent の時: 同じ場面で素材を切った絵も名前 off で撮る
  [switch]$AlsoOff,
  [string]$LookSky = '',
  [string]$UnityPath = ''
)

$ErrorActionPreference = 'Stop'
if ($GrasslandGround -and $Mode -ne 'explore') { throw '-GrasslandGround は -Mode explore で使います。' }
$ProjectPath = (Resolve-Path "$PSScriptRoot/..").Path
# explore / play で探索シーンの地域を替える引数（RegionExplorerSmoke.ApplyRegionArguments が読む）
$regionArgs = @()
if ($PSBoundParameters.ContainsKey('Seed')) { $regionArgs += @('-regionSeed', $Seed) }
elseif ($GrasslandGround) { $regionArgs += @('-regionSeed', '8') }
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
$grasslandCapture = $Mode -eq 'view3d' -and ($GrasslandSample -or $LookBiome -in 'grassland', 'auto')
if ($grasslandCapture -or $GrasslandGround) { $log = Join-Path $OutFull 'capture.log' }

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
  $type = 'RegionTerrainViewMenu'
  if ($Mode -eq 'view3d' -and $SurfaceCandidates) { $type = 'RegionSurfaceCandidates' }
  if ($Mode -eq 'view3d' -and $SurfaceCurrent) { $type = 'RegionSurfaceCandidates'; $method = 'CaptureCurrentFromCommandLine' }
  $unityArgs = @(
    '-batchmode',
    '-projectPath', "`"$ProjectPath`"",
    '-executeMethod', "Vastcore.RegionMap.Editor.$type.$method",
    '-regionOut', "`"$OutFull`"",
    '-logFile', "`"$log`""
  ) + $regionArgs
  if ($Mode -eq 'view3d') {
    if ($GrasslandSample) { $unityArgs += @('-grasslandSample', 'true') }
    if ($LookBiome) { $unityArgs += @('-lookBiome', $LookBiome) }
    if ($GrasslandCoverOff) { $unityArgs += @('-grasslandCover', 'false') }
    if ($GrasslandTiming) { $unityArgs += @('-grasslandTiming', 'true') }
    if ($LookCenter) { $unityArgs += @('-lookCenter', $LookCenter) }
    if ($AlcoveReview) { $unityArgs += @('-alcoveReview', 'true') }
    if ($LookPreset) { $unityArgs += @('-lookPreset', $LookPreset) }
    if ($SunSweep) { $unityArgs += @('-sunSweep', $SunSweep) }
    if ($ReliefCompare) { $unityArgs += @('-reliefCompare', 'true') }
    if ($SurfaceCompare) { $unityArgs += @('-surfaceCompare', 'true') }
    if ($TileBreakCompare) { $unityArgs += @('-tileBreakCompare', 'true') }
    if ($BlendCompare) { $unityArgs += @('-blendCompare', 'true') }
    if ($BlendDepth) { $unityArgs += @('-blendDepth', $BlendDepth) }
    if ($Sediment) { $unityArgs += @('-lookSediment', $Sediment) }
    if ($SedimentCompare) { $unityArgs += @('-sedimentCompare', 'true') }
    if ($RockGround) { $unityArgs += @('-lookRockGround', $RockGround) }
    if ($RockGroundCompare) { $unityArgs += @('-rockGroundCompare', 'true') }
    if ($PropsCompare) { $unityArgs += @('-propsCompare', 'true') }
    if ($PropsProof) { $unityArgs += @('-propsProof', 'true') }
    if ($LookSky) { $unityArgs += @('-lookSky', $LookSky) }
    if ($SurfaceCandidates) { $unityArgs += @('-surfaceCandidates', "`"$((Resolve-Path $SurfaceCandidates).Path)`"") }
    if ($SunAltitudes) { $unityArgs += @('-sunAltitudes', $SunAltitudes) }
    if ($CandidateViews) { $unityArgs += @('-candidateViews', $CandidateViews) }
    if ($Strengths) { $unityArgs += @('-strengths', $Strengths) }
    if ($SurfaceCurrent) { $unityArgs += @('-shotName', $SurfaceCurrent) }
    if ($AlsoOff) { $unityArgs += @('-alsoOff', 'true') }
    if ($ViewsFrom) { $unityArgs += @('-viewsFrom', "`"$((Resolve-Path $ViewsFrom).Path)`"") }
  }
} elseif ($Mode -eq 'build-windows') {
  $unityArgs = @(
    '-batchmode', '-nographics', '-quit', '-buildTarget', 'Win64',
    '-projectPath', ('"' + $ProjectPath + '"'),
    '-executeMethod', 'Vastcore.RegionMap.Editor.RegionFootpathBuild.BuildExplorerFromCommandLine',
    '-regionOut', ('"' + $OutFull + '"'),
    '-logFile', ('"' + $log + '"')
  )
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
  if ($GrasslandGround) {
    $unityArgs += @('-grasslandGround', 'true')
    if ($LookCenter) { $unityArgs += @('-lookCenter', $LookCenter) }
  }
}

Write-Host "Unity: $UnityPath"
Write-Host "Log:   $log"
$startedUtc = [DateTime]::UtcNow
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -PassThru -WindowStyle Hidden
$process.WaitForExit()
Write-Host "exit code: $($process.ExitCode)"

if (Test-Path $log) {
  if ($Mode -in 'view3d', 'view3d-cave') {
    $shaderFailure = Select-String -LiteralPath $log -Pattern "Shader error in 'Vastcore/Region(Terrain|Plants|GrasslandCover)'" | Select-Object -First 1
    if ($shaderFailure) { throw $shaderFailure.Line }
  }
  Select-String -Path $log -Pattern 'error CS\d+|\[RegionMap\]|Scripts have compiler errors' |
    Select-Object -First 30 | ForEach-Object { Write-Host $_.Line }
}
if (($grasslandCapture -or $GrasslandGround) -and $process.ExitCode -eq 0) {
  $expected = if ($GrasslandGround) { @('grassland-ground.png', 'grassland-return.png') } else { @('look-far.png', 'look-mid.png', 'look-near.png') }
  if ($GrasslandSample) { $expected += @('grassland-eye.png', 'grassland-panorama.png', 'grassland-outcrop.png', 'grassland-trail.png') }
  $fresh = Get-ChildItem -LiteralPath $OutFull -Recurse -Filter '*.png' -File |
    Where-Object { $_.Length -gt 0 -and $_.LastWriteTimeUtc -ge $startedUtc }
  foreach ($name in $expected) {
    if ($name -notin $fresh.Name) { throw "今回の撮影に $name がありません。ログ: $log" }
  }
}
exit $process.ExitCode
