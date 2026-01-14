param(
  [string]$Game = "ETS2",
  [int]$Count = 2
)

$ErrorActionPreference = 'Stop'

$rootCandidates = @(
  (Join-Path -Path $PSScriptRoot -ChildPath "..\ModlistManager\Modlists\$Game"),
  (Join-Path -Path $PSScriptRoot -ChildPath "..\bin\Debug\net8.0-windows\ModlistManager\Modlists\$Game"),
  (Join-Path -Path $PSScriptRoot -ChildPath "..\bin\Release\net8.0-windows\ModlistManager\Modlists\$Game")
)

$root = $rootCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if(-not $root){
  Write-Host "Kein Modlists-Ordner gefunden. Kandidaten:" -ForegroundColor Yellow
  $rootCandidates | ForEach-Object { Write-Host " - $_" }
  exit 1
}

$txt = Get-ChildItem -Path $root -Filter *.txt -File | Select-Object -First $Count
if(-not $txt){
  Write-Host "Keine .txt Modlisten in $root gefunden." -ForegroundColor Yellow
  exit 1
}

$zipPath = Join-Path $env:TEMP ("copilot-multizip-test-{0:yyyyMMdd_HHmmss}.zip" -f (Get-Date))

Write-Host "Root: $root"
Write-Host "TXT:"; $txt | ForEach-Object { Write-Host " - $($_.FullName)" }
Write-Host "ZIP: $zipPath"

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Build zip with a simplified version of current logic (to see if ZIP writing works at all)
if(Test-Path $zipPath){ Remove-Item $zipPath -Force }
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
  foreach($f in $txt){
    $folder = [IO.Path]::GetFileNameWithoutExtension($f.Name)
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, "$folder/$($f.Name)") | Out-Null
  }

  $entry = $zip.CreateEntry('manifest.txt')
  $sw = New-Object System.IO.StreamWriter($entry.Open(), (New-Object System.Text.UTF8Encoding($false)))
  $sw.WriteLine("Count: $($txt.Count)")
  $sw.Dispose()
}
finally {
  $zip.Dispose()
}

# Inspect entries
$zip2 = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
  Write-Host "Entries: $($zip2.Entries.Count)"
  $zip2.Entries | Select-Object FullName, Length | Format-Table -AutoSize
}
finally {
  $zip2.Dispose()
}

Write-Host "Fertig."