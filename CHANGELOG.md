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
- Noch nichts

### Entfernt
- Noch nichts

### Behoben
- Noch nichts

## [0.1.4] - 2025-09-17
### Added
- (Noch nichts)\n\n### Changed\n- (Noch nichts)\n\n### Fixed\n- (Noch nichts)\n\n## [0.1.3] - 2025-09-17
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

[Unreleased]:
[0.1.4]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.3...v0.1.4
 https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.4...HEAD
[0.1.3]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/rohere58/ETS2ATS.ModlistManager/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/rohere58/ETS2ATS.ModlistManager/releases/tag/v0.1.0

