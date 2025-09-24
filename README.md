# ETS2 / ATS Modlist Manager

<!-- Badges -->
<!-- Repository: rohere58/ETS2ATS.ModlistManager -->
[![Release](https://img.shields.io/github/v/release/rohere58/ETS2ATS.ModlistManager)](https://github.com/rohere58/ETS2ATS.ModlistManager/releases)
[![Build](https://github.com/rohere58/ETS2ATS.ModlistManager/actions/workflows/build.yml/badge.svg)](https://github.com/rohere58/ETS2ATS.ModlistManager/actions/workflows/build.yml)
[![CodeQL](https://github.com/rohere58/ETS2ATS.ModlistManager/actions/workflows/codeql.yml/badge.svg)](https://github.com/rohere58/ETS2ATS.ModlistManager/actions/workflows/codeql.yml)
[![Downloads](https://img.shields.io/github/downloads/rohere58/ETS2ATS.ModlistManager/total)](https://github.com/rohere58/ETS2ATS.ModlistManager/releases)
[![License: MPL-2.0](https://img.shields.io/badge/License-MPL%202.0-brightgreen.svg)](LICENSE)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-5C2D91)

> Ein schlanker Windows (.NET 8 / WinForms) Helfer zum Erstellen, Verwalten und Teilen von Modlisten für **Euro Truck Simulator 2** und **American Truck Simulator**.

> Hinweis/Notice: Aktuelle Version 0.1.16 enthält Quick Search & gebündeltes SII_Decrypt (MPL-2.0) sowie den Hotfix aus 0.1.15.
> Current version 0.1.16 includes Quick Search & bundled SII_Decrypt (MPL-2.0) plus the 0.1.15 hotfix.
> Downloads:
> - Multi-File (empfohlen / recommended): https://github.com/rohere58/ETS2ATS.ModlistManager/releases/download/v0.1.16/modlist-manager-0.1.16-win-x64.zip
> - Single-File (kompakte Einzel-EXE): https://github.com/rohere58/ETS2ATS.ModlistManager/releases/download/v0.1.16/modlist-manager-0.1.16-self-contained-win-x64.zip

## Inhaltsverzeichnis / Table of Contents
**Deutsch**
- [Funktionen](#funktionen)
- [Warum dieses Tool?](#warum-dieses-tool)
- [Screenshots](#screenshots)
- [Installation (Benutzer)](#installation-benutzer)
- [Erste Schritte](#erste-schritte)
- [Struktur & Dateien](#struktur--dateien)
- [Modlisten & Begleitdateien](#modlisten--begleitdateien)
- [Backup & Wiederherstellung](#backup--wiederherstellung)
- [Anpassen (Logos, Sprache, Theme)](#anpassen-logos-sprache-theme)
- [FAQ](#faq)
- [Build (Entwickler)](#build-entwickler)
- [Roadmap / Ideen](#roadmap--ideen)
- [Bekannte Einschränkungen](#bekannte-einschränkungen)
- [Lizenz / Hinweise](#lizenz--hinweise)

**English**
- [Features](#features)
- [Why this tool?](#why-this-tool)
- [Screenshots (EN)](#screenshots-en)
- [Installation (Users)](#installation-users)
- [Getting Started](#getting-started)
- [Structure & Files](#structure--files)
- [Modlists & Companion Files](#modlists--companion-files)
- [Backup & Restore](#backup--restore)
- [Customization (Logos, Language, Theme)](#customization-logos-language-theme)
- [FAQ (EN)](#faq-en)
- [Build (Developers)](#build-developers)
- [Roadmap / Ideas (EN)](#roadmap--ideas-en)
- [Known Limitations](#known-limitations)
- [License / Notices](#license--notices)

---
## Funktionen
- Spielumschaltung: ETS2 ↔ ATS
- Automatische Profilerkennung / Auswahl
- Erstellen, Laden, Umbenennen & Teilen von Modlisten
- Export / Teilen (kompakte, weitergebbare Form)
- Beschreibung / Notiz pro Modliste (separate Datei, auto-save)
- Optionale Link-Verwaltung (Download- / Referenz-Links je Mod)
- Automatisch generiertes Inhaltsverzeichnis in der integrierten FAQ (Markdown)
- Anpassbares Banner mit Transparenz & Logo-Overrides
- Mehrsprachigkeit (DE/EN) – leicht erweiterbar
- Portabel (ein Ordner, keine Registry-Abhängigkeit)

Nicht (mehr) enthalten: frühere, unzuverlässige Prüfung ob ein Mod physisch / im Workshop vorhanden ist (bewusst entfernt für UI-Klarheit).

## Warum dieses Tool?
Wer viele Profile / Modsets (Konvois, verschiedene Savegames, Test-Konfigurationen) hat, verliert schnell den Überblick. Dieses Tool fokussiert auf organisieren & dokumentieren – **kein** automatischer Download / Sync, sondern klare, reproduzierbare Listen.

## Screenshots
<img src="https://i.ibb.co/LdCtJD91/Screenshot-2025-09-22-054325.png" alt="Screenshot-2025-09-22-054325" border="0">
```
![Hauptfenster](docs/images/main-window.png)
![FAQ](docs/images/faq.png)
```

## Installation (Benutzer)
1. Release (ZIP) herunterladen:
   - Empfohlen (enthält Tools-Ordner direkt): [Multi-File v0.1.16](https://github.com/rohere58/ETS2ATS.ModlistManager/releases/download/v0.1.16/modlist-manager-0.1.16-win-x64.zip)
   - Alternative (eine einzelne EXE): [Single-File v0.1.16](https://github.com/rohere58/ETS2ATS.ModlistManager/releases/download/v0.1.16/modlist-manager-0.1.16-self-contained-win-x64.zip)
2. ZIP in einen beliebigen Ordner entpacken (z.B. `D:\Tools\ModlistManager`)
3. `ETS2ATS.ModlistManager.exe` starten
4. (Optional) Benutzerrechte: Stelle sicher, dass der Ordner beschreibbar ist (für Modlisten-Dateien)

Portabel: Zum Umzug einfach den gesamten Ordner kopieren.

## Erste Schritte
1. Spiel oben links auswählen
2. Profil wählen (Dropdown)
3. Neue Modliste erstellen oder bestehende laden
4. Mods hinzufügen (aus Profilbestand – abhängig von `profile.sii` / gespeicherten Daten)
5. Notiz ergänzen, Links pflegen
6. Liste teilen / sichern

## Struktur & Dateien
Beispiel (auszug):
```
<Installationsordner>
  ETS2ATS.ModlistManager.exe
  modlists/
    <Name>.txt
    <Name>.json
    <Name>.note
    <Name>.links.json
  ModlistManager/Resources/
    Languages/
      lang.de.json
      lang.en.json
    Logos/
      ets2.png
      ats.png
      (optional: logo.png -> überschreibt ETS2 Standard)
    ...
  ModlistManager/Tools/
    SII_Decrypt.exe                <- Gebündeltes Drittanbieter-Tool für profile.sii Entschlüsselung
    LICENSES/
      SII_Decrypt.MPL-2.0.txt
```

## Modlisten & Begleitdateien
| Datei | Zweck |
|-------|-------|
| `<Name>.txt` | Exportierte, einfache Textliste (lesbar / teilbar) |
| `<Name>.json` | Interne strukturierte Darstellung |
| `<Name>.note` | Freitext-Beschreibung / Notizen zur Liste |
| `<Name>.links.json` | Optionale Download-/Referenzlinks pro Mod |

Löschst du die Notiz (leer), wird die `.note` Datei entfernt.

## Backup & Wiederherstellung
Funktion (Menü): Backup & Restore – archiviert alle relevanten Dateien.  
Schnell-Notfall: Nur `profile.sii` wiederherstellen.  
Regelmäßiges externes Sichern des gesamten Ordners empfohlen.

## Anpassen (Logos, Sprache, Theme)
- Logo Override (ETS2): `Resources/Logos/logo.png` (falls vorhanden, hat Vorrang vor `ets2.png`)
- Sprache: Optionen → Sprache (Neustart empfohlen)
- Theme / Banner: Transparenz im Code via Eigenschaft `BackgroundOpacity`
- Weitere Lokalisierung: Sprachdatei kopieren & Schlüssel ergänzen

## FAQ
Die ausführliche FAQ (DE & EN) wird im Programm über Hilfe → FAQ angezeigt.  
Quellen: `ModlistManager/Resources/faq.de.md`, `faq.en.md` (Markdown mit automatischem Inhaltsverzeichnis).

## Build (Entwickler)
Voraussetzungen:
- .NET 8 SDK (Windows)
- Visual Studio 2022 oder `dotnet` CLI

Build (CLI):
```powershell
dotnet build .\ETS2ATS.ModlistManager.csproj -c Release
```

Start (Debug):
```powershell
dotnet run --project .\ETS2ATS.ModlistManager.csproj
```

## Roadmap / Ideen
- ATS spezifisches Logo-Override (`logo_ats.png`)
- Erweiterter Export (inkl. Notiz & Links als gebündeltes Archiv)
- Markdown-Unterstützung für Modlisten-Notizfeld
- Schnellsuche / Filter über der Tabelle
- Favoriten / Zuletzt verwendet
- Erweiterte Themes / Dark Mode Verfeinerung

## Bekannte Einschränkungen
- Keine physische Mod-Datei-Prüfung (bewusst entfernt)
- Kein Steam Workshop API Zugriff
- Keine automatische Aktualisierung / Download von Mods
- Kein Versionsvergleich zwischen zwei Listen

## Lizenz / Hinweise
- Hauptprojekt: Mozilla Public License 2.0 (siehe `LICENSE`)
- Gebündeltes Drittanbieter-Tool: `SII_Decrypt.exe` (unverändert, MPL-2.0) – siehe `ThirdPartyNotices.md` & Original: https://github.com/TheLazyTomcat/SII_Decrypt
- Lizenztext des Tools liegt unter `ModlistManager/Tools/LICENSES/SII_Decrypt.MPL-2.0.txt`
- Quelle / Quellcode (unverändert): upstream Repository; kein Fork, keine Modifikationen. Falls künftig Änderungen: werden in `ThirdPartyNotices.md` dokumentiert.
- ETS2 & ATS sind Marken von SCS Software – dieses Projekt steht in **keiner** offiziellen Verbindung
- Beiträge (PRs) gelten als unter MPL-2.0 bereitgestellt

Siehe auch: `ThirdPartyNotices.md` für vollständige Drittanbieter-Hinweise.

### Release & Versionierung
- Version im `.csproj` (SemVer) anpassen
- Änderungen in `CHANGELOG.md` unter "Unreleased" sammeln
- Tag erstellen: `vX.Y.Z` → Build Workflow erzeugt Artefakte
- (Optional) Separater Release Workflow kann ZIP automatisch an Release anhängen

Empfohlene Schritte für neuen Release:
1. `CHANGELOG.md` aktualisieren
2. Version bump im `.csproj`
3. Commit + Tag (`vX.Y.Z`)
4. Push Tag
5. Release Page auf GitHub beschreiben

#### Integritätsprüfung (SHA256)
Jedes Release enthält zusätzlich eine `.sha256` Datei.

Prüfen unter PowerShell:
```powershell
# Beispiel für v0.1.14
Get-FileHash .\ETS2ATS.ModlistManager_v0.1.14_win-x64.zip -Algorithm SHA256
Get-Content .\ETS2ATS.ModlistManager_v0.1.14_win-x64.zip.sha256
```
Die Hashes müssen identisch sein (Datei enthält Format: `<hash><2 spaces><zipname>`).

#### Lokaler Release-Build (ZIPs erzeugen)
Skript: `build-release.ps1`

Varianten (Beispiele PowerShell):
```powershell
# Version aus csproj lesen (Default = Single-File)
pwsh ./build-release.ps1

# Explizite Version Single-File
pwsh ./build-release.ps1 -Version 0.1.16 -SingleFile:$true

# Multi-File (mit Tools-Ordner)
pwsh ./build-release.ps1 -Version 0.1.16 -SingleFile:$false
```
Ergebnis: Verzeichnis `dist/` mit einem oder zwei ZIPs:
- `modlist-manager-<Version>-self-contained-win-x64.zip` (Single-File)
- `modlist-manager-<Version>-win-x64.zip` (Multi-File)

---

# English Version
> A lightweight Windows (.NET 8 / WinForms) helper to create, maintain and share mod lists for **Euro Truck Simulator 2** and **American Truck Simulator**.

## Features
- Switch between ETS2 / ATS
- Automatic profile detection & selection
- Create, load, rename & share modlists
- Export / share (compact human-readable form)
- Per-list description / note (separate file, auto-save)
- Optional link management (download / reference links per mod)
- Quick search bar with provider selection (Google / Steam Workshop / TruckyMods) incl. Enter trigger
- Automatic table of contents in integrated Markdown FAQ
- Customizable banner (transparency & logo overrides)
- Multi-language (DE/EN) – easy to extend
- Portable (single folder, no registry dependencies)

Removed (by design): previous unreliable mod file / workshop presence detection to keep the UI lean.

## Why this tool?
Managing many profiles / convoys / experimental mod setups becomes messy. This tool focuses on organization & documentation – **no** auto download / sync, just clear reproducible lists.

## Screenshots (EN)
*(Placeholder – add real screenshots as needed)*
```
![Main Window](docs/images/main-window.png)
![FAQ](docs/images/faq.png)
```

## Installation (Users)
1. Download release (ZIP):
   - Recommended (includes Tools folder with SII_Decrypt.exe): [Multi-File v0.1.16](https://github.com/rohere58/ETS2ATS.ModlistManager/releases/download/v0.1.16/modlist-manager-0.1.16-win-x64.zip)
   - Alternative (single executable): [Single-File v0.1.16](https://github.com/rohere58/ETS2ATS.ModlistManager/releases/download/v0.1.16/modlist-manager-0.1.16-self-contained-win-x64.zip)
2. Extract to any folder (e.g. `D:\Tools\ModlistManager`)
3. Launch `ETS2ATS.ModlistManager.exe`
4. Ensure folder is writable (for modlist files)

Portable: Move the whole folder to migrate.

## Getting Started
1. Select game (top left)
2. Choose profile (dropdown)
3. Create or load a modlist
4. Add mods (from profile data – depending on `profile.sii` / stored info)
5. Add a note, manage links
6. Share / export

## Structure & Files
Example:
```
<Install Folder>
  ETS2ATS.ModlistManager.exe
  modlists/
    <Name>.txt
    <Name>.json
    <Name>.note
    <Name>.links.json
  ModlistManager/Resources/
    Languages/
      lang.de.json
      lang.en.json
    Logos/
      ets2.png
      ats.png
      (optional: logo.png -> overrides default ETS2 logo)
  Tools/
    SII_Decrypt.exe                <- Bundled third-party tool (profile.sii decryption)
    LICENSES/
      SII_Decrypt.MPL-2.0.txt
```

## Modlists & Companion Files
| File | Purpose |
|------|---------|
| `<Name>.txt` | Exported plain text list (shareable) |
| `<Name>.json` | Internal structured representation |
| `<Name>.note` | Free-form description / notes |
| `<Name>.links.json` | Optional download / reference links per mod |

Deleting the note (empty) removes the `.note` file.

## Backup & Restore
Menu function archives relevant files.  
Quick emergency: restore only `profile.sii`.  
External periodic backups recommended.

## Customization (Logos, Language, Theme)
- Logo override (ETS2): `Resources/Logos/logo.png` (priority over `ets2.png`)
- Language: Options → Language (restart recommended)
- Theme / Banner: transparency via `BackgroundOpacity` property
- Add localization: copy a language file & extend keys

## FAQ (EN)
Full FAQ (DE & EN) accessible via Help → FAQ inside the app.  
Sources: `ModlistManager/Resources/faq.de.md`, `faq.en.md` (Markdown with auto ToC).

## Build (Developers)
Requirements:
- .NET 8 SDK (Windows)
- Visual Studio 2022 or `dotnet` CLI

Build:
```powershell
dotnet build .\ETS2ATS.ModlistManager.csproj -c Release
```

Run (Debug):
```powershell
dotnet run --project .\ETS2ATS.ModlistManager.csproj
```

## Roadmap / Ideas (EN)
- ATS specific logo override (`logo_ats.png`)
- Enhanced export (bundle note + links)
- Markdown support for list note field
- Quick search / filter above grid
- Favorites / recent lists
- Extended theming / refined dark mode

## Known Limitations
- No physical mod file presence check (removed)
- No Steam Workshop API usage
- No automatic mod downloads / updates
- No diff between two lists

## License / Notices
- Main project: Mozilla Public License 2.0 (see `LICENSE`)
- Bundled third-party tool: `SII_Decrypt.exe` (unmodified, MPL-2.0) – see `ThirdPartyNotices.md` & upstream: https://github.com/TheLazyTomcat/SII_Decrypt
- Tool license text: `ModlistManager/Tools/LICENSES/SII_Decrypt.MPL-2.0.txt`
- Source (unmodified): upstream repository; no fork / changes. Future modifications (if any) will be documented in `ThirdPartyNotices.md`.
- ETS2 & ATS are trademarks of SCS Software – no affiliation
- Contributions provided under MPL-2.0

See also: `ThirdPartyNotices.md` for complete third-party notices.

### Release & Versioning (EN)
- Update version in `.csproj` (SemVer)
- Collect changes in `CHANGELOG.md` (Unreleased)
- Create tag `vX.Y.Z` → workflow builds artifacts
- Draft release with artifacts

Recommended release flow:
1. Update changelog
2. Bump version
3. Commit + tag
4. Push tag
5. Publish release description

#### Local release build (create ZIPs)
Script: `build-release.ps1`
```powershell
pwsh ./build-release.ps1
pwsh ./build-release.ps1 -Version 0.1.16 -SingleFile:$true
pwsh ./build-release.ps1 -Version 0.1.16 -SingleFile:$false
```
Artifacts in `dist/`:
- `modlist-manager-<Version>-self-contained-win-x64.zip` (Single-File)
- `modlist-manager-<Version>-win-x64.zip` (Multi-File)

---
*Feedback & issues welcome – please include clear reproduction steps and affected files.*

---
*Feedback & Issues willkommen – bitte klare Repro-Schritte und ggf. betroffene Dateien anhängen.*
