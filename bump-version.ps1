<#
.SYNOPSIS
  Semantische Versionsanhebung für ETS2ATS.ModlistManager.
.DESCRIPTION
  Aktualisiert die <Version> im csproj und passt die CHANGELOG Referenzen an.
  Kann eine neue Release-Sektion als Platzhalter einfügen (optional mit -AddSection).
.PARAMETER NewVersion
  Explizite Zielversion (z.B. 0.2.0). Überschreibt andere Increment-Parameter.
.PARAMETER Major / Minor / Patch
  Inkrementiert die jeweilige Stelle basierend auf der aktuellen Version (Patch default, wenn nichts angegeben).
.PARAMETER AddSection
  Fügt eine neue leere Sektion im CHANGELOG für die neue Version mit heutigem Datum ein.
.PARAMETER DryRun
  Zeigt nur geplante Änderungen an, ohne Dateien zu modifizieren.
.EXAMPLE
  pwsh ./bump-version.ps1 -Patch -AddSection
.EXAMPLE
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

# Aktuelle Version lesen
[xml]$xml = Get-Content $csproj
$current = ($xml.Project.PropertyGroup | Where-Object { $_.Version }).Version
if (-not $current) { throw 'Aktuelle Version konnte nicht aus csproj gelesen werden.' }

if ($NewVersion) {
    if ($NewVersion -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') { throw "Ungültiges Versionsformat: $NewVersion (erwartet SemVer X.Y.Z)" }
    $target = $NewVersion
} else {
    if (-not ($Major -or $Minor -or $Patch)) { $Patch = $true }
    $parts = $current.Split('.')
    [int]$maj = $parts[0]; [int]$min = $parts[1]; [int]$pat = $parts[2]
    if ($Major) { $maj++; $min = 0; $pat = 0 }
    elseif ($Minor) { $min++; $pat = 0 }
    elseif ($Patch) { $pat++ }
    $target = "$maj.$min.$pat"
}

if ($target -eq $current) { Write-Warning "Zielversion entspricht aktueller Version ($current) – keine Änderung."; exit 0 }

Write-Host "Aktuelle Version: $current" -ForegroundColor Cyan
Write-Host "Neue Version:     $target" -ForegroundColor Green

# Changelog aktualisieren: Unreleased Link + ggf. neue Sektion einfügen
$changelogContent = Get-Content $changelog -Raw

# Links am Ende finden
$linkPattern = '\[Unreleased\]: .*?\n'
if ($changelogContent -notmatch $linkPattern) { throw 'Unreleased Link Referenz nicht gefunden.' }

# Bisher letzte Version identifizieren (alle Link-Zeilen am Ende einsammeln)
$versionLinkMatches = Select-String -InputObject $changelogContent -Pattern '^\[([0-9]+\.[0-9]+\.[0-9]+)\]:' -AllMatches
if ($versionLinkMatches) {
  $allVersions = @()
  foreach ($m in $versionLinkMatches.Matches) { $allVersions += $m.Groups[1].Value }
  $lastVersion = $allVersions | Sort-Object {[version]$_} -Descending | Select-Object -First 1
} else {
  $lastVersion = $current  # Fallback: falls nur eine aktuelle Version existiert
}
if (-not $lastVersion) { throw 'Konnte letzte Version für Compare-Link nicht ermitteln (leer).' }

$newUnreleased = "[Unreleased]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v$target...HEAD";
$changelogContent = [regex]::Replace($changelogContent, '\[Unreleased\]: .*', $newUnreleased)

# Neue Version Link hinzufügen (falls nicht bereits vorhanden)
if ($changelogContent -notmatch "^\[$target\]:" ) {
    $insertPos = $changelogContent.LastIndexOf("[Unreleased]:")
    if ($insertPos -gt -1) {
        # Füge Link direkt nach Unreleased-Link ein
        $before = $changelogContent.Substring(0, $insertPos)
        $after = $changelogContent.Substring($insertPos)
        $linkLine = "[${target}]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v$lastVersion...v$target`n"
        $changelogContent = $before + $after.Replace("[Unreleased]:", "[Unreleased]:`n$linkLine")
    }
}

if ($AddSection) {
    $date = (Get-Date).ToString('yyyy-MM-dd')
    if ($changelogContent -match "## \[$target\]") {
        Write-Warning "Sektion für $target existiert bereits – überspringe AddSection." 
    } else {
        $section = "## [$target] - $date`n### Added`n- (Noch nichts)\n\n### Changed\n- (Noch nichts)\n\n### Fixed\n- (Noch nichts)\n\n"
        # Neue Sektion direkt vor der letzten existierenden Version (nach Unreleased Block) einfügen
        $patternFirstVersion = '(?ms)^## \[[0-9]+\.[0-9]+\.[0-9]+\]' 
        $match = [regex]::Match($changelogContent, $patternFirstVersion)
        if ($match.Success) {
            $idx = $match.Index
            $changelogContent = $changelogContent.Insert($idx, $section)
        } else {
            $changelogContent += "`n$section"
        }
    }
}

if ($DryRun) {
    Write-Host "--- DryRun: Geändertes CHANGELOG (Ausschnitt) ---" -ForegroundColor Yellow
    $preview = ($changelogContent -split '\n') | Select-Object -First 40
    $preview | ForEach-Object { Write-Host $_ }
    Write-Host "--- Ende DryRun ---" -ForegroundColor Yellow
    exit 0
}

# Dateien wirklich schreiben
($xml.Project.PropertyGroup | Where-Object { $_.Version }).Version = $target
$xml.Save($csproj)
Set-Content -Path $changelog -Value $changelogContent -Encoding UTF8

Write-Host "Version aktualisiert & CHANGELOG angepasst." -ForegroundColor Green
Write-Host "Nächste Schritte:" -ForegroundColor Cyan
Write-Host "  git add ETS2ATS.ModlistManager.csproj CHANGELOG.md" -ForegroundColor DarkGray
Write-Host "  git commit -m 'Bump version to $target'" -ForegroundColor DarkGray
Write-Host "  git tag v$target" -ForegroundColor DarkGray
Write-Host "  git push origin main && git push origin v$target" -ForegroundColor DarkGray
