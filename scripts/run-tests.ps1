param(
  [ValidateSet('editmode','playmode','all')] [string]$TestMode = 'all',
  [string]$UnityPath = '',
  [string]$ProjectPath = '',
  [string]$ResultsDir = 'artifacts/test-results',
  [string]$LogsDir = 'artifacts/logs',
  [switch]$RequireNonZeroTests
)

$ErrorActionPreference = 'Stop'

if (-not $ProjectPath -or $ProjectPath -eq '') {
  $ProjectPath = (Resolve-Path "$PSScriptRoot/.." ).Path
}

# Ensure output dirs
New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null
$ResultsDir = (Resolve-Path $ResultsDir).Path
$LogsDir = (Resolve-Path $LogsDir).Path

function Get-UnityEditorPath() {
  param([string]$Override)
  if ($Override -and (Test-Path $Override)) { return $Override }
  if ($env:UNITY_PATH -and (Test-Path $env:UNITY_PATH)) { return $env:UNITY_PATH }

  $verFile = Join-Path $ProjectPath 'ProjectSettings/ProjectVersion.txt'
  if (-not (Test-Path $verFile)) { throw "ProjectVersion.txt not found: $verFile" }
  $verLine = Get-Content $verFile | Select-Object -First 1
  if ($verLine -notmatch 'm_EditorVersion:\s*(?<v>[^\s]+)') { throw "Could not parse Unity version from $verFile" }
  $version = $Matches['v']
  $default = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
  if (Test-Path $default) { return $default }

  # Fallback: best-effort search under Unity Hub
  $hub = "C:\Program Files\Unity\Hub\Editor"
  if (Test-Path $hub) {
    $candidate = Get-ChildItem $hub -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if ($candidate) {
      $exe = Join-Path $candidate.FullName 'Editor/Unity.exe'
      if (Test-Path $exe) { return $exe }
    }
  }

  # Final fallback: rely on PATH
  return 'Unity.exe'
}

$unityExe = Get-UnityEditorPath -Override $UnityPath
Write-Host "Using Unity Editor: $unityExe"

function Run-UnityTests() {
  param([string]$mode)

  $normalizedMode = $mode.ToLowerInvariant()
  $platformArg = switch ($normalizedMode) {
    'editmode' { 'EditMode' }
    'playmode' { 'PlayMode' }
    default { throw "Unsupported test mode: $mode" }
  }

  $res = Join-Path $ResultsDir ("$normalizedMode-results.xml")
  $log = Join-Path $LogsDir ("$normalizedMode.log")

  $args = @(
    '-batchmode','-nographics',
    '-projectPath', "`"$ProjectPath`"",
    '-runTests','-testPlatform', $platformArg,
    '-testResults', "`"$res`"",
    '-logFile', "`"$log`""
  )

  Write-Host "Running $normalizedMode tests..."
  $process = Start-Process -FilePath $unityExe -ArgumentList $args -NoNewWindow -PassThru -Wait
  $code = $process.ExitCode

  if ($code -ne 0) { throw "Unity returned non-zero exit code ($code) for $normalizedMode" }
  if (-not (Test-Path $res)) { throw "Results not found: $res" }

  if ($RequireNonZeroTests) {
    [xml]$xml = Get-Content -Raw -Path $res
    $totalAttr = $xml.'test-run'.total
    $total = 0
    if (-not [int]::TryParse($totalAttr, [ref]$total)) {
      throw "Could not parse total test count from results: $res"
    }
    if ($total -le 0) {
      throw "No tests were executed for $normalizedMode (total=$total)."
    }
  }

  Write-Host "PASS: $normalizedMode tests passed. Results: $res"
}

switch ($TestMode) {
  'editmode' { Run-UnityTests 'editmode' }
  'playmode' { Run-UnityTests 'playmode' }
  'all' { Run-UnityTests 'editmode'; Run-UnityTests 'playmode' }
}
