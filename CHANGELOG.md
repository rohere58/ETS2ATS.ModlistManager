# Changelog
Alle nennenswerten Änderungen dieses Projekts werden in dieser Datei dokumentiert.

Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/) und dieses Projekt hält sich (wo sinnvoll) an [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]
### Hinzugefügt
- (Geplant) Erweiterte Export-Option (Notiz + Links gebündelt)
- (Geplant) ATS spezifisches Logo-Override `logo_ats.png`
- (Geplant) Schnellsuche / Filter
- (Geplant) Markdown im Notizfeld

### Geändert
- (Geplant) –

### Entfernt
- (Geplant) –

### Behoben
- (Geplant) –

## [0.1.12] - 2025-09-19
### Geändert
- Info-Spalte speichert nun kanonisch unter dem Package-Token (nicht mehr unter Modnamen)
- Laden der Kurzinfos: Priorität Token > Modname > volles Package (Legacy-Fallback)
- Build: Modlists Copy-Policy auf PreserveNewest (verhindert versehentliches Überschreiben)

### Hinzugefügt
- Diagnose-Logs beim Speichern zeigen den geschriebenen info.json-Pfad
- VS Code Launch-Profile zum Starten der EXE aus bin (inkl. "no build")

### Behoben
- Persistenz-Diskrepanz zwischen EXE-Start und IDE-Start (durch einheitliche Laufumgebung + Copy-Policy)

## [0.1.10] - 2025-09-17
### Fixed
- CI: Release Workflow PowerShell Syntaxfehler in Ressourcen-Prüfung behoben (Test-Path -and Klammerung)
- CI: Robustere Environment Variable Setzung (Add-Content statt echo >>) für ZIP/Checksum Variablen

### Changed
- Interner Build-Prozess stabilisiert für Releases ohne externes Tool

## [0.1.9] - 2025-09-17
### Removed
- Entferntes Bundling von `SII_Decrypt.exe` (rechtliche / Distributions-Klarheit)

### Changed
- Release Workflow vereinfacht (Tool-Prüfungen komplett gestrichen)

### Notes
- Nutzer platzieren optional `SII_Decrypt.exe` manuell unter `ModlistManager/Tools/` oder `Tools/`

## [0.1.8] - 2025-09-17
### Changed
- CI: Release-Workflow Verifikation entschärft (Tool optional, akzeptiert alte und neue modlists-Pfadvarianten)

### Fixed
- Verhindert unnötige Release-Abbrüche wenn nur das optionale Tool fehlt

## [0.1.7] - 2025-09-17
### Changed
- Modlisten-Pfad vereinheitlicht: Nutzung nur noch unter `ModlistManager/modlists` (Migration & Flatten-Logik)

### Fixed
- Entferntes versehentlich verschachteltes `modlists/modlists` Verzeichnis wird beim Start automatisch bereinigt

## [0.1.6] - 2025-09-17
### Added
- (Noch nichts)

### Changed
- (Noch nichts)

### Fixed
- (Noch nichts)

## [0.1.5] - 2025-09-17
### Added
- (Noch nichts)

### Changed
- (Noch nichts)

### Fixed
- (Noch nichts)

## [0.1.4] - 2025-09-17
### Added
- (Noch nichts)

### Changed
- (Noch nichts)

### Fixed
- (Noch nichts)

## [0.1.3] - 2025-09-17
### Fixed
- Release Packaging: Leere Strukturordner `modlists/ETS2` und `modlists/ATS` erscheinen nun zuverlässig im ZIP (Platzhalter `.gitkeep` + Content Einträge)
- Release Workflow: Fehlende Ressourcen (z.B. `SII_Decrypt.exe`) werden jetzt vor dem Zipping erkannt (Verify-Step)

### Changed
- Build/Dev: Dokumentation im csproj zu Platzhalterordnern ergänzt

### Internal
- Added CI safety check to fail fast if expected tool / folders are missing

## [0.1.2] - 2025-09-16
### Fixed
- Release: Vollständiges `modlists/` Verzeichnis jetzt im ZIP (Auto-Erstellung der Unterordner bei Start)
- Tools: Sicher gestellt, dass `SII_Decrypt.exe` immer in Publish enthalten ist (`Always` Copy)

### Changed
- Interne Pfad-Initialisierung (`EnsureModlistsDirectories()`) für robusteren First-Run

## [0.1.1] - 2025-09-16
### Fixed
- FAQ: Links wurden als reiner Text angezeigt (doppeltes HTML-Escaping entfernt)
- FAQ: Robuste Pfadsuche für Markdown / TXT nach Publish (mehrere Kandidatenverzeichnisse)
- Release-ZIP: `SII_Decrypt.exe` und `modlists/` fehlten (Publish-Konfiguration erweitert)

### Changed
- Minor: HTML/CSS für FAQ Rendering leicht bereinigt

## [0.1.0] - 2025-09-16
### Added
- Grundfunktionalität: Modlisten erstellen, laden, teilen
- Notiz- und Link-Dateien je Modliste
- Mehrsprachigkeit (DE/EN)
- FAQ mit Markdown + automatischem Inhaltsverzeichnis
- Anpassbares Banner (Logo-Override + Transparenz)
- GitHub: Build Workflow & CodeQL Analyse, MPL-2.0 Lizenz

### Removed
- Frühere heuristische Mod-Verfügbarkeitsprüfung (vereinfachte UI)

[Unreleased]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.12...HEAD
[0.1.12]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.11...v0.1.12
[0.1.11]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.10...v0.1.11
[0.1.10]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.9...v0.1.10
[0.1.9]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.8...v0.1.9
[0.1.8]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.7...v0.1.8
[0.1.7]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.6...v0.1.7
[0.1.6]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.5...v0.1.6
[0.1.5]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.4...v0.1.5
[0.1.4]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/rohere58/ETS2ATS.ModlistManager/releases/tag/v0.1.0


