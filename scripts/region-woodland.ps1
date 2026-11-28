param(
  [ValidateSet('All', 'Forest', 'Rainforest', 'NaturePark')] [string]$Preset = 'All',
  [ValidatePattern('^-?\d+(,-?\d+)*$')] [string]$Seeds = '8,9',
  [ValidatePattern('^\d+(,\d+)*$')] [string]$SunAltitudes = '60,38,20,10',
  [int]$LayoutSeed,
  [int]$ShapeSeed,
  [string]$OutDir = 'artifacts/region-map/woodland',
  [string]$UnityPath = ''
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $UnityPath) {
  $version = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
  if ($version -notmatch '^m_EditorVersion:\s*(\S+)') { throw 'Unity version is missing from ProjectVersion.txt' }
  $UnityPath = "C:\Program Files\Unity\Hub\Editor\$($Matches[1])\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity Editor was not found at $UnityPath" }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$output = (Resolve-Path -LiteralPath $OutDir).Path
$log = Join-Path $output 'capture.log'
$regionSeeds = [int[]]$Seeds.Split(',')
$arguments = @(
  '-batchmode', '-projectPath', ('"' + $project + '"'),
  '-executeMethod', 'Vastcore.RegionMap.Editor.RegionWoodlandMenu.CaptureFromCommandLine',
  '-woodlandSeeds', ($regionSeeds -join ','), '-woodlandPreset', $Preset, '-sunAltitudes', $SunAltitudes,
  '-regionOut', ('"' + $output + '"'), '-logFile', ('"' + $log + '"')
)
if ($PSBoundParameters.ContainsKey('LayoutSeed')) { $arguments += @('-woodlandLayoutSeed', $LayoutSeed) }
if ($PSBoundParameters.ContainsKey('ShapeSeed')) { $arguments += @('-woodlandShapeSeed', $ShapeSeed) }

$captureStarted = [DateTime]::UtcNow
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity exit code $($process.ExitCode). See $log" }
$shaderError = Select-String -LiteralPath $log -Pattern "^Shader error in 'Vastcore/Region(Woodland|Terrain|Prop|Plants)'" | Select-Object -First 1
if ($shaderError) { throw $shaderError.Line }

$profiles = if ($Preset -eq 'All') { 'Forest', 'Rainforest', 'NaturePark' } else { $Preset }
foreach ($seed in $regionSeeds) {
  foreach ($profile in $profiles) {
    foreach ($sun in $SunAltitudes.Split(',')) {
      $layers = if ([int]$sun -eq 38) { 'baseline', 'canopy', 'full' } else { 'baseline', 'full' }
      foreach ($view in @('far', 'mid', 'near')) {
        foreach ($layer in $layers) {
          $image = Get-Item -LiteralPath (Join-Path $output "seed-$seed/$profile/sun-$sun/$view-$layer.png") -ErrorAction SilentlyContinue
          if (-not $image -or $image.Length -le 0 -or $image.LastWriteTimeUtc -lt $captureStarted) {
            throw "The current capture did not produce $seed/$profile/$sun/$view-$layer.png. See $log"
          }
        }
      }
      if ($profile -eq 'NaturePark' -and [int]$sun -eq 38) {
        foreach ($view in @('walk-start', 'walk-meadow', 'walk-woods')) {
          $image = Get-Item -LiteralPath (Join-Path $output "seed-$seed/$profile/sun-$sun/$view.png") -ErrorAction SilentlyContinue
          if (-not $image -or $image.Length -le 0 -or $image.LastWriteTimeUtc -lt $captureStarted) {
            throw "The current capture did not produce $seed/$profile/$sun/$view.png. See $log"
          }
        }
      }
    }
  }
}
Select-String -LiteralPath $log -SimpleMatch '[Woodland]' | ForEach-Object { Write-Host $_.Line }
Write-Host $output
