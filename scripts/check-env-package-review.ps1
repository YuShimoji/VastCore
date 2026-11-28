[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReviewDir,

    [string]$ManifestName = 'package-review.json'
)

$ErrorActionPreference = 'Stop'
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-Err([string]$msg) { $errors.Add($msg) }
function Add-Warn([string]$msg) { $warnings.Add($msg) }

if (-not (Test-Path -LiteralPath $ReviewDir -PathType Container)) {
    throw "ReviewDir not found: $ReviewDir"
}

$manifestPath = Join-Path $ReviewDir $ManifestName
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Missing manifest: $manifestPath (see docs/02_design/ENVIRONMENT_PRODUCTION_PIPELINE.md §5)"
}

try {
    $raw = [System.IO.File]::ReadAllText($manifestPath, [System.Text.UTF8Encoding]::new($false, $true))
    $m = $raw | ConvertFrom-Json
}
catch {
    throw "Failed to parse UTF-8 JSON manifest ($manifestPath): $($_.Exception.Message)"
}

foreach ($field in @('packageId', 'specPath', 'entryMenu', 'targetConceptImage', 'unityBeforeImage', 'unityAfterImage', 'stopReason')) {
    if (-not $m.PSObject.Properties[$field] -or [string]::IsNullOrWhiteSpace([string]$m.$field)) {
        Add-Err "Missing or empty required string field: $field"
    }
}

foreach ($imgField in @('targetConceptImage', 'unityBeforeImage', 'unityAfterImage')) {
    $rel = [string]$m.$imgField
    if (-not [string]::IsNullOrWhiteSpace($rel)) {
        $resolved = if ([System.IO.Path]::IsPathRooted($rel)) { $rel } else { Join-Path $ReviewDir $rel }
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            Add-Err "Image file does not exist for ${imgField}: $rel"
        }
    }
}

$refs = @($m.references)
if ($refs.Count -eq 0) {
    Add-Err "references must contain at least 1 sourced reference entry."
}
else {
    for ($i = 0; $i -lt $refs.Count; $i++) {
        if ([string]::IsNullOrWhiteSpace([string]$refs[$i].source)) {
            Add-Err "references[$i] is missing source URL/citation."
        }
    }
}

$reused = @($m.reusedAssets)
if ($reused.Count -eq 0) {
    Add-Err "reusedAssets must not be empty. Check existing shared assets/shaders/FBX first (DS-015 §2)."
}
else {
    for ($i = 0; $i -lt $reused.Count; $i++) {
        if ([string]::IsNullOrWhiteSpace([string]$reused[$i].path) -or [string]::IsNullOrWhiteSpace([string]$reused[$i].role)) {
            Add-Err "reusedAssets[$i] must specify both path and role."
        }
    }
}

$rmap = @($m.realizationMap)
if ($rmap.Count -eq 0) {
    Add-Err "realizationMap must not be empty (DS-015 §3)."
}
else {
    $rangesSeen = @{}
    for ($i = 0; $i -lt $rmap.Count; $i++) {
        $entry = $rmap[$i]
        $range = ([string]$entry.range).ToLowerInvariant()
        if ($range) { $rangesSeen[$range] = $true }
        if ([string]::IsNullOrWhiteSpace([string]$entry.element) -or [string]::IsNullOrWhiteSpace([string]$entry.method)) {
            Add-Err "realizationMap[$i] must specify element and method."
        }
        if ($entry.primitiveFallback -eq $true -and [string]::IsNullOrWhiteSpace([string]$entry.fallbackReason)) {
            Add-Err "realizationMap[$i] uses primitiveFallback=true without fallbackReason (primitive/vertex-color final look is prohibited by DS-015 §3.2)."
        }
    }
    foreach ($reqRange in @('far', 'mid', 'near')) {
        if (-not $rangesSeen.ContainsKey($reqRange)) {
            Add-Err "realizationMap is missing distance band: $reqRange"
        }
    }
}

$allowedStops = @('converged', 'converged_round1', 'escalated_boundary', 'escalated_direction', 'escalated_after_pivot')
$stopReason = [string]$m.stopReason
if ($stopReason -and ($allowedStops -notcontains $stopReason)) {
    Add-Err "Invalid stopReason '$stopReason'. Allowed: $($allowedStops -join ', ')"
}

$iters = @($m.iterations)
$gaps = @($m.remainingGaps)
if ($iters.Count -eq 0) {
    Add-Err "iterations must contain at least 1 recorded diagnosis/action round."
}
elseif ($iters.Count -lt 2) {
    if (-not ($stopReason -eq 'converged_round1' -and $gaps.Count -eq 0)) {
        Add-Err "Only $($iters.Count) iteration(s) recorded with remainingGaps=$($gaps.Count) and stopReason='$stopReason'. Complete at least 2 autonomous rounds before returning (DS-015 §4)."
    }
}

$hasPivot = $false
for ($i = 0; $i -lt $iters.Count; $i++) {
    $it = $iters[$i]
    if ([string]::IsNullOrWhiteSpace([string]$it.diagnosis) -or [string]::IsNullOrWhiteSpace([string]$it.action)) {
        Add-Err "iterations[$i] must specify diagnosis and action."
    }
    if ($it.structuralPivot -eq $true) { $hasPivot = $true }
}

if ($stopReason -eq 'escalated_after_pivot' -and -not $hasPivot) {
    Add-Err "stopReason='escalated_after_pivot' requires at least one iteration with structuralPivot=true."
}

if ($m.metrics -and ($null -eq $m.metrics.gpuMs) -and [string]::IsNullOrWhiteSpace([string]$m.metrics.gpuBudgetNote)) {
    Add-Warn "metrics.gpuMs is null and gpuBudgetNote is empty; record GPU cost or note why it is unmeasured."
}

foreach ($w in $warnings) {
    Write-Host "WARN: $w" -ForegroundColor Yellow
}

if ($errors.Count -gt 0) {
    foreach ($e in $errors) {
        Write-Host "ERROR: $e" -ForegroundColor Red
    }
    throw "Environment package review check failed with $($errors.Count) error(s)."
}

Write-Host "Environment package review OK: packageId=$($m.packageId) rounds=$($iters.Count) reused=$($reused.Count) stopReason=$stopReason"
