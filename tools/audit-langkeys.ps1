$ErrorActionPreference = 'Stop'

# Audit:
# - sammelt alle Übersetzungs-Keys aus ModlistManager/Resources/Languages/lang.*.json
# - sammelt alle im Code/Designer verwendeten Keys (Control.Tag="..." + T("..."))
# - reportet fehlende Keys pro Sprache

$repoRoot = Split-Path -Parent $PSScriptRoot
$srcRoot  = Join-Path $repoRoot 'ModlistManager'
$langRoot = Join-Path $srcRoot  'Resources\Languages'

if (-not (Test-Path $langRoot)) {
  throw "Language folder not found: $langRoot"
}

$langFiles = Get-ChildItem -Path $langRoot -Filter 'lang.*.json' -File
if ($langFiles.Count -eq 0) {
  throw "No language files found in: $langRoot"
}

function Get-LangCodeFromFileName([string]$baseName) {
  # baseName is like 'lang.de'
  if ($baseName -notmatch '^lang\.(?<code>[a-z]{2})$') { return $null }
  return $Matches.code
}

function Get-JsonKeys([string]$path) {
  $obj = Get-Content -Path $path -Raw -Encoding UTF8 | ConvertFrom-Json
  $keys = @()
  foreach ($p in $obj.PSObject.Properties) {
    if ($null -ne $p -and -not [string]::IsNullOrWhiteSpace($p.Name)) {
      $keys += $p.Name
    }
  }
  return $keys
}

Write-Host "Languages folder: $langRoot" -ForegroundColor Cyan

# 1) All keys per language
$keysByLang = @{}
$knownLangs = @()
foreach ($f in $langFiles) {
  $code = Get-LangCodeFromFileName $f.BaseName
  if ($null -eq $code) {
    Write-Warning "Skipping unexpected language filename: $($f.Name)"
    continue
  }

  $knownLangs += $code
  $keysByLang[$code] = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach ($k in (Get-JsonKeys $f.FullName)) {
    [void]$keysByLang[$code].Add($k)
  }
}

$knownLangs = $knownLangs | Sort-Object -Unique
Write-Host ("Languages: " + ($knownLangs -join ', ')) -ForegroundColor Cyan

# 2) Collect used keys from code
$usedKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

# 2a) Control.Tag = "..." (Designer files are the main source)
$tagPattern = 'Tag\s*=\s*"(?<key>[^"]+)"'
$tagFiles = Get-ChildItem -Path $srcRoot -Recurse -Include '*.cs' -File
foreach ($f in $tagFiles) {
  $text = Get-Content -Path $f.FullName -Raw -Encoding UTF8
  foreach ($m in [regex]::Matches($text, $tagPattern)) {
    $k = $m.Groups['key'].Value
    if (-not [string]::IsNullOrWhiteSpace($k)) { [void]$usedKeys.Add($k) }
  }
}

# 2b) T("...") usage
$tPattern = '\bT\(\s*"(?<key>[^"]+)"'
foreach ($f in $tagFiles) {
  $text = Get-Content -Path $f.FullName -Raw -Encoding UTF8
  foreach ($m in [regex]::Matches($text, $tPattern)) {
    $k = $m.Groups['key'].Value
    if (-not [string]::IsNullOrWhiteSpace($k)) { [void]$usedKeys.Add($k) }
  }
}

$usedKeyList = @()
$tmpUsed = New-Object string[] $usedKeys.Count
$usedKeys.CopyTo($tmpUsed)
$usedKeyList = $tmpUsed | Sort-Object
Write-Host ("Used keys found in code: " + $usedKeyList.Count) -ForegroundColor Cyan

# 3) Compute missing keys per language
$report = @()
foreach ($lang in $knownLangs) {
  $missing = @()
  foreach ($k in $usedKeyList) {
    if (-not $keysByLang[$lang].Contains($k)) {
      $missing += $k
    }
  }
  $report += [pscustomobject]@{
    Lang = $lang
    MissingCount = $missing.Count
    MissingKeys = ($missing -join ';')
  }
}

$reportSorted = $report | Sort-Object MissingCount -Descending

Write-Host "\n=== Missing keys per language ===" -ForegroundColor Yellow
$reportSorted | Format-Table -AutoSize Lang, MissingCount

# 4) Write report files to /artifacts
$artifactDir = Join-Path $repoRoot 'artifacts'
if (-not (Test-Path $artifactDir)) { New-Item -ItemType Directory -Path $artifactDir | Out-Null }

$csvPath = Join-Path $artifactDir 'lang-audit-missing.csv'
$jsonPath = Join-Path $artifactDir 'lang-audit-missing.json'

$reportSorted | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $csvPath
$reportSorted | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -Path $jsonPath

Write-Host "\nWrote:" -ForegroundColor Green
Write-Host "- $csvPath"
Write-Host "- $jsonPath"

# 5) Optional: list keys that exist but are unused (can be noisy, so only output counts)
$allKeysUnion = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($lang in $knownLangs) {
  $tmp = New-Object string[] $keysByLang[$lang].Count
  $keysByLang[$lang].CopyTo($tmp)
  foreach ($k in $tmp) { [void]$allKeysUnion.Add($k) }
}

$unused = @()
 $tmpAll = New-Object string[] $allKeysUnion.Count
 $allKeysUnion.CopyTo($tmpAll)
foreach ($k in ($tmpAll | Sort-Object)) {
  if (-not $usedKeys.Contains($k)) { $unused += $k }
}

Write-Host ("\nUnused keys (union across languages): " + $unused.Count) -ForegroundColor DarkGray