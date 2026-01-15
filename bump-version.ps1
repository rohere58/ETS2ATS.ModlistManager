<#
  Bump-Skript für ETS2ATS.ModlistManager
  Nutzung:
    pwsh ./bump-version.ps1 -Patch -AddSection
    pwsh ./bump-version.ps1 -NewVersion 0.2.0 -AddSection
#>
param(
  [string]$NewVersion,
  [switch]$Major,
  [switch]$Minor,
  [switch]$Patch,
  [switch]$AddSection,
  [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$csproj = Join-Path $PSScriptRoot 'ETS2ATS.ModlistManager.csproj'
$changelog = Join-Path $PSScriptRoot 'CHANGELOG.md'
if (!(Test-Path $csproj)) { throw "csproj nicht gefunden: $csproj" }
if (!(Test-Path $changelog)) { throw "CHANGELOG.md nicht gefunden: $changelog" }

[xml]$xml = Get-Content $csproj
$current = ($xml.Project.PropertyGroup | Where-Object { $_.Version }).Version
if (-not $current) { throw 'Aktuelle Version konnte nicht aus csproj gelesen werden.' }

if ($NewVersion) {
  if ($NewVersion -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$') { throw "Ungültiges Versionsformat: $NewVersion" }
  $target = $NewVersion
} else {
  if (-not ($Major -or $Minor -or $Patch)) { $Patch = $true }
  $parts = $current.Split('.')
  [int]$maj=$parts[0]; [int]$min=$parts[1]; [int]$pat=$parts[2]
  [int]$rev=0
  if ($parts.Length -ge 4) { [int]$rev=$parts[3] }
  if ($Major) { $maj++; $min=0; $pat=0 }
  elseif ($Minor) { $min++; $pat=0 }
  elseif ($Patch) {
    if ($parts.Length -ge 4) { $rev++ } else { $pat++ }
  }
  $target = ($parts.Length -ge 4) ? "$maj.$min.$pat.$rev" : "$maj.$min.$pat"
}

if ($target -eq $current) { Write-Warning "Version unverändert ($current)"; exit 0 }

Write-Host "Aktuelle Version: $current" -ForegroundColor Cyan
Write-Host "Neue Version:     $target" -ForegroundColor Green

$content = Get-Content $changelog -Raw
if ($content -notmatch '\[Unreleased\]: .*') { throw 'Kein [Unreleased] Link gefunden.' }

# Version Links einsammeln
$matches = Select-String -InputObject $content -Pattern '^\[([0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?)\]:' -AllMatches
$lastVersion = $current
if ($matches) {
  $versions = $matches.Matches | ForEach-Object { $_.Groups[1].Value }
  $lastVersion = ($versions | Sort-Object {[version]$_} | Select-Object -Last 1)
}

$content = [regex]::Replace($content, '\[Unreleased\]: .*', "[Unreleased]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v$target...HEAD")
if ($content -notmatch "^\[$target\]:") {
  $content = $content -replace '\[Unreleased\]:', "[Unreleased]:`n[$target]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v$lastVersion...v$target"
}

if ($AddSection -and $content -notmatch "## \[$target\]") {
  $date = (Get-Date).ToString('yyyy-MM-dd')
  $section = @"
## [$target] - $date
### Added
- (Noch nichts)

### Changed
- (Noch nichts)

### Fixed
- (Noch nichts)

"@
  $patternFirstVersion = '(?m)^## \[[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?\]'
  $m = [regex]::Match($content, $patternFirstVersion)
  if ($m.Success) {
    $content = $content.Insert($m.Index, $section)
  } else {
    $content += $section
  }
}

if ($DryRun) {
  Write-Host '--- DryRun Vorschau ---' -ForegroundColor Yellow
  ($content -split "\n") | Select-Object -First 40 | ForEach-Object { Write-Host $_ }
  Write-Host '--- Ende ---' -ForegroundColor Yellow
  exit 0
}

($xml.Project.PropertyGroup | Where-Object { $_.Version }).Version = $target
$xml.Save($csproj)
Set-Content -Path $changelog -Value $content -Encoding UTF8

Write-Host 'Version aktualisiert & CHANGELOG angepasst.' -ForegroundColor Green
Write-Host 'Nächste Schritte:' -ForegroundColor Cyan
Write-Host "  git add ETS2ATS.ModlistManager.csproj CHANGELOG.md" -ForegroundColor DarkGray
Write-Host "  git commit -m 'Bump version to $target'" -ForegroundColor DarkGray
Write-Host "  git tag v$target" -ForegroundColor DarkGray
Write-Host "  git push origin main && git push origin v$target" -ForegroundColor DarkGray
