param(
  [string]$UnityPath = '',
  [string]$ProjectPath = '',
  [string]$LogsDir = 'artifacts/logs'
)

$ErrorActionPreference = 'Stop'

if (-not $ProjectPath -or $ProjectPath -eq '') {
  $ProjectPath = $PSScriptRoot
  if ($ProjectPath.EndsWith('\scripts')) {
    $ProjectPath = $ProjectPath.Substring(0, $ProjectPath.Length - 8)
  }
}

# Ensure output dirs
if (-not (Test-Path $LogsDir)) {
    New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null
}

function Get-UnityEditorPath() {
  param([string]$Override)
  if ($Override -and (Test-Path $Override)) { return $Override }
  if ($env:UNITY_PATH -and (Test-Path $env:UNITY_PATH)) { return $env:UNITY_PATH }

  $verFile = Join-Path $ProjectPath 'ProjectSettings/ProjectVersion.txt'
  if (-not (Test-Path $verFile)) { throw "ProjectVersion.txt not found: $verFile" }
  $content = Get-Content $verFile | Select-Object -First 1
  if ($content -notmatch 'm_EditorVersion:\s*(?<v>[^\s]+)') { throw "Could not parse Unity version from $verFile" }
  $version = $Matches['v']
  
  $default = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
  if (Test-Path $default) { return $default }

  # Fallback: best-effort search under Unity Hub
  $hub = "C:\Program Files\Unity\Hub\Editor"
  if (Test-Path $hub) {
    $candidate = Get-ChildItem $hub -Directory | Sort-Object Name -Descending | Select-Object -First 1
    if ($candidate) {
      $exe = Join-Path $candidate.FullName 'Editor\Unity.exe'
      if (Test-Path $exe) { return $exe }
    }
  }
  return 'Unity.exe'
}

$unityExe = Get-UnityEditorPath -Override $UnityPath
$logFile = Join-Path $LogsDir "compile-check.log"

Write-Host "--- Unity Compile Check ---"
Write-Host "Project Path: $ProjectPath"
Write-Host "Unity Path:   $unityExe"
Write-Host "Log File:     $logFile"

$args = @(
  '-batchmode',
  '-nographics',
  '-quit',
  '-projectPath', "`"$ProjectPath`"",
  '-logFile', "`"$logFile`""
)

# Start Unity
$process = Start-Process -FilePath $unityExe -ArgumentList $args -Wait -PassThru -NoNewWindow
$exitCode = $process.ExitCode

if ($exitCode -eq 0) {
  Write-Host "✓ Compilation check passed."
  exit 0
} else {
  Write-Host "✗ Compilation check failed with exit code $exitCode."
  Write-Host "Check logs for details: $logFile"
  
  # Try to extract errors from log
  if (Test-Path $logFile) {
      $errors = Select-String -Path $logFile -Pattern "error CS\d+"
      if ($errors) {
          Write-Host "--- Detected Errors ---"
          $errors | Select-Object -First 10 | ForEach-Object { Write-Host $_.Line }
      }
  }
  exit $exitCode
}
