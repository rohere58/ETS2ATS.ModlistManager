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
    [string]$Version
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

Write-Host "==> Self-contained Build (SingleFile)" -ForegroundColor Yellow
dotnet publish $csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o (Join-Path $publishRoot 'sc')

# ZIP
$zipPath = Join-Path $dist ("modlist-manager-$Version-self-contained-win-x64.zip")
Write-Host "==> Erzeuge ZIP" -ForegroundColor Yellow
Compress-Archive -Path (Join-Path $publishRoot 'sc' '*') -DestinationPath $zipPath

Write-Host "==> Fertig" -ForegroundColor Green
Write-Host "Ausgabe:" -ForegroundColor Green
Get-ChildItem $dist | Format-Table -AutoSize

Write-Host "Hinweis: Prüfe die self-contained EXE durch Teststart vor Upload." -ForegroundColor DarkGray
