param(
  [Parameter(Mandatory=$true)][string]$InputPng,
  [Parameter(Mandatory=$true)][string]$OutputIco
)

# Erstellt eine Multi-Size ICO (16/32/48/256) aus einem PNG.
# Keine externen Dependencies; nutzt System.Drawing (Windows).

Add-Type -AssemblyName System.Drawing

$sizes = @(16, 32, 48, 256)

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

# ICO Header
$bw.Write([uint16]0)   # reserved
$bw.Write([uint16]1)   # type: icon
$bw.Write([uint16]$sizes.Count)

# Platzhalter für Directory Entries
$entryPositions = @()
foreach ($s in $sizes) {
  $entryPositions += $ms.Position
  0..15 | ForEach-Object { $bw.Write([byte]0) }
}

$img = [System.Drawing.Image]::FromFile($InputPng)

# PNG-Daten je Größe erzeugen
$entries = @()
foreach ($s in $sizes) {
  $bmp = New-Object System.Drawing.Bitmap $s, $s
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
  $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
  $g.Clear([System.Drawing.Color]::Transparent)
  $g.DrawImage($img, 0, 0, $s, $s)
  $g.Dispose()

  $pngMs = New-Object System.IO.MemoryStream
  $bmp.Save($pngMs, [System.Drawing.Imaging.ImageFormat]::Png)
  $bmp.Dispose()

  $entries += ,@($s, $pngMs.ToArray())
  $pngMs.Dispose()
}

# Image data schreiben + Directory Entries auffüllen
$offset = $ms.Position
for ($i = 0; $i -lt $sizes.Count; $i++) {
  $s = [int]$entries[$i][0]
  $data = [byte[]]$entries[$i][1]

  # Directory Entry an Patch-Position schreiben
  $cur = $ms.Position
  $ms.Position = $entryPositions[$i]

  $b = if ($s -eq 256) { [byte]0 } else { [byte]$s }
  $bw.Write($b)              # width
  $bw.Write($b)              # height
  $bw.Write([byte]0)         # color count
  $bw.Write([byte]0)         # reserved
  $bw.Write([uint16]1)       # planes
  $bw.Write([uint16]32)      # bit count
  $bw.Write([uint32]$data.Length) # bytes in res
  $bw.Write([uint32]$offset)      # image offset

  $ms.Position = $cur

  # Image data am Ende schreiben
  $ms.Position = $offset
  $bw.Write($data)
  $offset = $ms.Position
}

$bw.Flush()
[System.IO.File]::WriteAllBytes($OutputIco, $ms.ToArray())

try { $img.Dispose() } catch {}
try { $bw.Dispose() } catch {}
try { $ms.Dispose() } catch {}

Get-Item -LiteralPath $OutputIco | Select-Object FullName, Length
