[CmdletBinding()]
param(
    [int64]$LargeFileLimitBytes = 95MB
)

$ErrorActionPreference = 'Stop'

$root = (& git rev-parse --show-toplevel).Trim()
if (-not $root) {
    throw 'The current directory is not inside a Git repository.'
}

Set-Location -LiteralPath $root

$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

$candidatePaths = (& git ls-files --cached --others --exclude-standard -z) -split "`0" |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

foreach ($relativePath in $candidatePaths) {
    $fullPath = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        continue
    }

    $file = Get-Item -LiteralPath $fullPath
    if ($file.Length -ge $LargeFileLimitBytes) {
        $attribute = (& git check-attr filter -- $relativePath) -join "`n"
        if ($attribute -notmatch ': filter: lfs\s*$') {
            $errors.Add("File exceeds $([math]::Round($file.Length / 1MB, 2)) MB and is not covered by Git LFS: $relativePath")
        }
    }
}

$forbiddenRoots = @(
    'Library',
    'Temp',
    'Obj',
    'Build',
    'Builds',
    'Logs',
    'UserSettings',
    'Release',
    '.codex',
    '.agents'
)

foreach ($forbiddenRoot in $forbiddenRoots) {
    $tracked = @(& git ls-files -- $forbiddenRoot)
    if ($tracked.Count -gt 0) {
        $errors.Add("Forbidden generated directory is tracked: $forbiddenRoot/ ($($tracked.Count) files)")
    }
}

$assetFiles = Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -File -Recurse -Force |
    Where-Object { $_.Extension -ne '.meta' -and -not $_.Name.StartsWith('.') }

foreach ($assetFile in $assetFiles) {
    $metaPath = "$($assetFile.FullName).meta"
    if (-not (Test-Path -LiteralPath $metaPath -PathType Leaf)) {
        $relativePath = $assetFile.FullName.Substring($root.Length).TrimStart('\', '/')
        $warnings.Add("Unity asset is missing its .meta file: $relativePath")
    }
}

$secretNamePattern = '(?i)(^\.env$|secret|credential|private[_-]?key|id_rsa|\.pfx$|\.p12$|\.jks$|\.keystore$)'
foreach ($relativePath in $candidatePaths) {
    if ([IO.Path]::GetFileName($relativePath) -match $secretNamePattern -and
        [IO.Path]::GetFileName($relativePath) -ne '.env.example') {
        $warnings.Add("Review possible sensitive file before publishing: $relativePath")
    }
}

Write-Host "Candidate files: $($candidatePaths.Count)"
Write-Host "GitHub file warning threshold: $([math]::Round($LargeFileLimitBytes / 1MB)) MB"

foreach ($warning in $warnings) {
    Write-Warning $warning
}

if ($errors.Count -gt 0) {
    foreach ($validationError in $errors) {
        Write-Error $validationError -ErrorAction Continue
    }
    exit 1
}

Write-Host 'Repository validation passed: no tracked build/cache directories or oversized non-LFS files were found.' -ForegroundColor Green
