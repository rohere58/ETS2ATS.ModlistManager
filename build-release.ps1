<#
.SYNOPSIS
  Lokales Release-Build Skript (nur self-contained) für ETS2 / ATS Modlist Manager.
.DESCRIPTION
  Baut ausschließlich eine self-contained win-x64 Single-File Version und zippt sie nach ./dist.
.PARAMETER Version
  Optionale Versionsnummer (Default: liest <Version> aus csproj).
.EXAMPLE
  pwsh ./build-release.ps1
.EXAMPLE
  pwsh ./build-release.ps1 -Version 0.1.1
#>
param(
  [string]$Version,
  [switch]$SingleFile = $true
)

$ErrorActionPreference = 'Stop'

Write-Host "==> Starte lokalen Release-Build" -ForegroundColor Cyan

$csproj = Join-Path $PSScriptRoot 'ETS2ATS.ModlistManager.csproj'
if (!(Test-Path $csproj)) { throw "csproj nicht gefunden: $csproj" }

# Version extrahieren falls nicht gesetzt
if (-not $Version) {
    [xml]$xml = Get-Content $csproj
    $Version = ($xml.Project.PropertyGroup | Where-Object { $_.Version }).Version
    if (-not $Version) { $Version = '0.0.0-local' }
}
Write-Host "Verwendung Version: $Version"

$publishRoot = Join-Path $PSScriptRoot 'publish'
$dist = Join-Path $PSScriptRoot 'dist'
if (Test-Path $publishRoot) { Remove-Item $publishRoot -Recurse -Force }
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Path $dist | Out-Null

Write-Host "==> dotnet restore" -ForegroundColor Yellow
dotnet restore $csproj

if ($SingleFile) {
  Write-Host "==> Self-contained Build (SingleFile)" -ForegroundColor Yellow
  dotnet publish $csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o (Join-Path $publishRoot 'pkg')
  $zipPath = Join-Path $dist ("modlist-manager-$Version-self-contained-win-x64.zip")
  Write-Host "==> Erzeuge ZIP (SingleFile)" -ForegroundColor Yellow
  Compress-Archive -Path (Join-Path $publishRoot 'pkg' '*') -DestinationPath $zipPath -Force
} else {
  Write-Host "==> Framework-Ordner Build (MultiFile, mit Tools)" -ForegroundColor Yellow
  dotnet publish $csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o (Join-Path $publishRoot 'pkg')
  # Sicherstellen, dass Tools wirklich da sind
  if (-not (Test-Path (Join-Path $publishRoot 'pkg' 'ModlistManager/Tools/SII_Decrypt.exe'))) {
    Write-Warning "SII_Decrypt.exe fehlt im Publish-Ausgabepfad!"
  }
  $zipPath = Join-Path $dist ("modlist-manager-$Version-win-x64.zip")
  Write-Host "==> Erzeuge ZIP (MultiFile)" -ForegroundColor Yellow
  Compress-Archive -Path (Join-Path $publishRoot 'pkg' '*') -DestinationPath $zipPath -Force
}

# SHA256 Checksumme erzeugen
Write-Host "==> Erzeuge SHA256" -ForegroundColor Yellow
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower()
$hashFile = "$zipPath.sha256"
"$hash  $(Split-Path -Leaf $zipPath)" | Out-File -FilePath $hashFile -Encoding ASCII -NoNewline
Write-Host "SHA256: $hash" -ForegroundColor Green

Write-Host "==> Fertig" -ForegroundColor Green
Write-Host "Ausgabe:" -ForegroundColor Green
Get-ChildItem $dist | Format-Table -AutoSize

Write-Host "Hinweis: Optional beide Varianten bauen: SingleFile (+Tools ggf. extrahiert) und MultiFile (mit unverändertem SII_Decrypt.exe)." -ForegroundColor DarkGray
