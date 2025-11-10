param(
  [ValidateSet('editmode','playmode','all')] [string]$TestMode = 'all',
  [string]$UnityPath = '',
  [string]$ProjectPath = '',
  [string]$ResultsDir = 'artifacts/test-results',
  [string]$LogsDir = 'artifacts/logs'
)

$ErrorActionPreference = 'Stop'

if (-not $ProjectPath -or $ProjectPath -eq '') {
  $ProjectPath = (Resolve-Path "$PSScriptRoot/.." ).Path
}

# Ensure output dirs
New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null

function Get-UnityEditorPath() {
  param([string]$Override)
  if ($Override -and (Test-Path $Override)) { return $Override }
  if ($env:UNITY_PATH -and (Test-Path $env:UNITY_PATH)) { return $env:UNITY_PATH }

  $verFile = Join-Path $ProjectPath 'ProjectSettings/ProjectVersion.txt'
  if (-not (Test-Path $verFile)) { throw "ProjectVersion.txt not found: $verFile" }
  $verLine = Get-Content $verFile | Select-Object -First 1
  if ($verLine -notmatch 'm_EditorVersion:\s*(?<v>[^\s]+)') { throw "Could not parse Unity version from $verFile" }
  $version = $Matches['v']
  $default = "C:\\Program Files\\Unity\\Hub\\Editor\\$version\\Editor\\Unity.exe"
  if (Test-Path $default) { return $default }
  # Fallback: best-effort search under Unity Hub
  $hub = "C:\\Program Files\\Unity\\Hub\\Editor"
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
  $res = Join-Path $ResultsDir ("$mode-results.xml")
  $log = Join-Path $LogsDir ("$mode.log")
  $args = @(
    '-batchmode','-nographics','-quit',
    '-projectPath', "$ProjectPath",
    '-runTests','-testPlatform', $mode,
    '-testResults', "$res",
    '-logFile', "$log"
  )
  Write-Host "Running $mode tests..."
  & $unityExe @args
  $code = $LASTEXITCODE
  if ($code -ne 0) { throw "Unity returned non-zero exit code ($code) for $mode" }
  if (-not (Test-Path $res)) { throw "Results not found: $res" }
  Write-Host "✓ $mode tests passed. Results: $res"
}

switch ($TestMode) {
  'editmode' { Run-UnityTests 'editmode' }
  'playmode' { Run-UnityTests 'playmode' }
  'all' { Run-UnityTests 'editmode'; Run-UnityTests 'playmode' }
}
