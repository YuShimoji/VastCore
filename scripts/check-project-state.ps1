[CmdletBinding()]
param(
    [string]$StatePath = 'docs/runtime-state.md',
    [string]$ExpectedBranch = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$errors = [System.Collections.Generic.List[string]]::new()
$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$commonMojibakeChars = @(
    [char]0x7E3A,
    [char]0x7E67,
    [char]0x8B41,
    [char]0x873F,
    [char]0x8B07
)

function Add-StateError {
    param([string]$Message)
    $errors.Add($Message)
}

function Read-StrictUtf8 {
    param([string]$RelativePath)

    $fullPath = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        Add-StateError "Missing canonical file: $RelativePath"
        return ''
    }

    try {
        return $strictUtf8.GetString([System.IO.File]::ReadAllBytes($fullPath))
    }
    catch {
        Add-StateError "File is not valid UTF-8: $RelativePath ($($_.Exception.Message))"
        return ''
    }
}

$canonicalPaths = @(
    'AGENTS.md',
    'docs/REPO_LOCAL_RULES.md',
    'docs/runtime-state.md',
    'docs/INVARIANTS.md',
    'docs/USER_REQUEST_LEDGER.md',
    'docs/OPERATOR_WORKFLOW.md',
    'docs/INTERACTION_NOTES.md',
    'docs/project-context.md',
    'docs/ai/CORE_RULESET.md',
    'docs/ai/DECISION_GATES.md',
    'docs/ai/STATUS_AND_HANDOFF.md',
    'docs/ai/WORKFLOWS_AND_PHASES.md'
)

foreach ($path in $canonicalPaths) {
    $text = Read-StrictUtf8 $path
    $hasCommonMojibake = $commonMojibakeChars | Where-Object { $text.Contains($_) }
    if ($text.Contains([char]0xFFFD) -or $hasCommonMojibake) {
        Add-StateError "Possible mojibake in canonical file: $path"
    }
}

$state = Read-StrictUtf8 $StatePath
foreach ($heading in @('# VastCore Runtime State', '## Current Position', '## Current Trust Assessment', '## Next Action')) {
    if (-not $state.Contains($heading)) {
        Add-StateError "runtime-state is missing required heading: $heading"
    }
}

$dateMatch = [regex]::Match($state, '(?m)^Last Updated:\s*(\d{4}-\d{2}-\d{2})\s*$')
if (-not $dateMatch.Success) {
    Add-StateError 'runtime-state must contain Last Updated: YYYY-MM-DD.'
}
else {
    $updated = [DateTime]::MinValue
    if (-not [DateTime]::TryParseExact(
            $dateMatch.Groups[1].Value,
            'yyyy-MM-dd',
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::None,
            [ref]$updated)) {
        Add-StateError "Invalid Last Updated date: $($dateMatch.Groups[1].Value)"
    }
    elseif ($updated.Date -gt [DateTime]::UtcNow.Date.AddDays(1)) {
        Add-StateError "Last Updated is unexpectedly in the future: $($updated.ToString('yyyy-MM-dd'))"
    }
}

$branchMatch = [regex]::Match($state, '(?m)^\|\s*Branch\s*\|\s*`([^`]+)`\s*\|')
if (-not $branchMatch.Success) {
    Add-StateError 'runtime-state Current Position must contain a Branch table row.'
}
elseif ($ExpectedBranch -and $branchMatch.Groups[1].Value -ne $ExpectedBranch) {
    Add-StateError "runtime-state branch '$($branchMatch.Groups[1].Value)' does not match '$ExpectedBranch'."
}

$artifactMatch = [regex]::Match($state, '(?m)^\|\s*Active artifact\s*\|\s*`([^`]+)`\s*\|')
if (-not $artifactMatch.Success) {
    Add-StateError 'runtime-state Current Position must contain an Active artifact table row.'
}
else {
    $artifact = $artifactMatch.Groups[1].Value.Replace('/', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $artifact) -PathType Leaf)) {
        Add-StateError "Active artifact does not exist: $($artifactMatch.Groups[1].Value)"
    }
}

foreach ($field in @('Project', 'Current bottleneck', 'Change relation')) {
    if ($state -notmatch "(?m)^\|\s*$([regex]::Escape($field))\s*\|") {
        Add-StateError "runtime-state Current Position is missing table field: $field"
    }
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Host "ERROR: $_" -ForegroundColor Red }
    throw "Project state validation failed with $($errors.Count) error(s)."
}

$reportedBranch = $branchMatch.Groups[1].Value
$reportedDate = $dateMatch.Groups[1].Value
Write-Host "Project state OK: branch=$reportedBranch updated=$reportedDate artifact=$($artifactMatch.Groups[1].Value)"
