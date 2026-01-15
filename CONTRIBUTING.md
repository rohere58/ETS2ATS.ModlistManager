# Contributing / Mitwirken

Vielen Dank für dein Interesse! This project is a .NET 8 WinForms desktop app. Below you’ll find short guidelines and a practical, copy-friendly release checklist (DE/EN).

---

## Quick Guidelines

- Branching
  - feature/<topic>, bugfix/<topic>, release/<version>
- Commits
  - Klar und prägnant; Gegenwartsform. Gruppiere verwandte Änderungen. Deutsche oder englische Messages sind ok; Release/öffentliche Texte bevorzugt EN/DE konsistent.
- Code Style
  - .NET 8, WinForms. Designer-Dateien (*.Designer.cs) nicht manuell editieren (nur über den Designer); Logik in die Code-Behind (*.cs).
  - Lokalisierung: Neue Keys in beiden Dateien pflegen: `ModlistManager/Resources/Languages/lang.de.json` und `lang.en.json` (Fallback ist EN). Kurze, präzise Schlüssel, bestehende nicht brechen.
  - UI-Lokalisierung: Controls erhalten `Tag` für LanguageService, dynamische Menüs ebenso.
- Ressourcen
  - Logos/Icons unter `ModlistManager/Resources/Logos` bzw. `Resources/Icons` ablegen; Namenskonventionen beibehalten.
- Tests/QA
  - Es gibt keine Unit-Tests. Bitte vor PR: Build (Release), kurze Smoke-Tests (Start, Sprache wechseln, Theme wechseln, Modliste laden/speichern, Export/Import).

---

## Release-Checkliste (DE)

1) Changelog & Notizen
- `CHANGELOG.md` Einträge aus „Unreleased“ nach `vX.Y.Z` verschieben.
- `RELEASE_NOTES_vX.Y.Z.md` erstellen/aktualisieren (DE/EN kurz).

2) Version anheben
- `ETS2ATS.ModlistManager.csproj`: `<Version>X.Y.Z</Version>` setzen.

3) Lokalisierung prüfen
- Neue/angepasste Keys in `lang.de.json` und `lang.en.json` vorhanden? Fallbacks ok?

4) Build & Publish (win-x64)
- Release-Publish erzeugen (framework-dependent):
  - `dotnet publish ETS2ATS.ModlistManager.csproj -c Release -r win-x64 --self-contained false`
- ZIP bauen und SHA256 generieren (Dateiname: `ETS2ATS.ModlistManager_vX.Y.Z_win-x64.zip`).
 - ZIP(s) bauen und SHA256 generieren (Dateinamen):
   - `modlist-manager-X.Y.Z-win-x64.zip`
   - `modlist-manager-X.Y.Z-self-contained-win-x64.zip`

5) Commit, Branch, Tag
- Änderungen committen (inkl. Changelog/Notes).
- Release-Branch `release/vX.Y.Z` pushen.
- Annotated Tag setzen `vX.Y.Z` und pushen.

6) GitHub Release
- Release für `vX.Y.Z` anlegen (Titel: „ETS2ATS.ModlistManager vX.Y.Z“).
- Notes aus `RELEASE_NOTES_vX.Y.Z.md` übernehmen.
- Assets hochladen: ZIP + `.sha256`.
 - Assets hochladen: ZIP(s) + `.sha256`.

7) README aktualisieren
- Direktdownload-Link auf neues Asset (DE/EN Installation) aktualisieren.

8) Merge nach main
- PR `release/vX.Y.Z` → `main` erstellen und mergen, Release-Branch löschen.

9) Nachkontrolle
- Release-Seite, Download-Link, SHA256, Start des Programms (Smoke-Test) prüfen.

Optional
- Self-contained Build zusätzlich veröffentlichen.
- FAQ/Docs (DE/EN) und Screenshots aktualisieren.

---

## Release Checklist (EN)

1) Changelog & Notes
- Move entries from “Unreleased” to `vX.Y.Z` in `CHANGELOG.md`.
- Create/update `RELEASE_NOTES_vX.Y.Z.md` (short DE/EN summary).

2) Bump Version
- Set `<Version>X.Y.Z</Version>` in `ETS2ATS.ModlistManager.csproj`.

3) Localization
- Ensure new/changed keys exist in both `lang.en.json` and `lang.de.json`; EN acts as fallback.

4) Build & Publish (win-x64)
- Create framework-dependent publish:
  - `dotnet publish ETS2ATS.ModlistManager.csproj -c Release -r win-x64 --self-contained false`
- Create ZIP(s) and SHA256 (names):
  - `modlist-manager-X.Y.Z-win-x64.zip`
  - `modlist-manager-X.Y.Z-self-contained-win-x64.zip`

5) Commit, Branch, Tag
- Commit changes (incl. changelog/notes).
- Push release branch `release/vX.Y.Z`.
- Create annotated tag `vX.Y.Z` and push.

6) GitHub Release
- Create release for `vX.Y.Z` with title “ETS2ATS.ModlistManager vX.Y.Z”.
- Use `RELEASE_NOTES_vX.Y.Z.md` as notes.
- Upload assets: ZIP + `.sha256`.
- Upload assets: ZIP(s) + `.sha256`.

7) README Update
- Update direct download link (DE/EN installation sections).

8) Merge to main
- Open PR `release/vX.Y.Z` → `main`, merge, delete branch.

9) Final checks
- Verify release page, download link, SHA256 and a quick smoke test (app starts, language/theme switch, load/save modlist).

Optional
- Publish self-contained build variant.
- Update FAQ/Docs and screenshots.

---

## Useful CLI (optional)

PowerShell examples:

```powershell
# Tag + Release (gh CLI)
git tag -a vX.Y.Z -m "ETS2ATS.ModlistManager vX.Y.Z"
git push origin vX.Y.Z

# Create release with notes and upload assets
gh release create vX.Y.Z --title "ETS2ATS.ModlistManager vX.Y.Z" --notes-file "RELEASE_NOTES_vX.Y.Z.md"
gh release upload vX.Y.Z `
  "artifacts/vX.Y.Z/modlist-manager-X.Y.Z-win-x64.zip" `
  "artifacts/vX.Y.Z/modlist-manager-X.Y.Z-win-x64.zip.sha256" `
  "artifacts/vX.Y.Z/modlist-manager-X.Y.Z-self-contained-win-x64.zip" `
  "artifacts/vX.Y.Z/modlist-manager-X.Y.Z-self-contained-win-x64.zip.sha256" `
  --clobber
```

Bitte halte Releases klein, nachvollziehbar und dokumentiert. Danke! 🙌# Contribution Guide
Danke für dein Interesse, zum ETS2 / ATS Modlist Manager beizutragen!

## Grundprinzipien
- **Einfachheit & Klarheit**: UI bleibt schlank – keine halbfertigen oder schwer wartbaren Heuristiken.
- **Portabilität**: Keine Registry-Abhängigkeiten oder unnötige globale Zustände.
- **Lesbarkeit vor cleverness**: Bevorzuge verständlichen Code.

## Ablauf für Beiträge
1. Issue anlegen (Feature / Bug) – beschreibe Motivation & Use Case.
2. Auf das Issue verlinken, wenn du einen Branch erstellst.
3. Branch-Namensschema (Vorschlag):
   - `feat/<kurzbeschreibung>`
   - `fix/<kurzbeschreibung>`
   - `docs/<kurzbeschreibung>`
4. Pull Request öffnen mit:
   - Kurzer Beschreibung
   - Warum Änderung nötig ist
   - Screenshots bei UI-Änderungen
   - Hinweis auf Breaking Changes (falls vorhanden)
5. Review abwarten – ggf. Anpassungen vornehmen.

## Commit Style (empfohlen)
Format: `<type>: <kurze Aussage>`
Types: `feat`, `fix`, `refactor`, `docs`, `chore`, `build`.

Beispiele:
```
feat: add markdown FAQ with auto ToC
fix: remove gradient exception on banner
refactor: simplify profile loading logic
```

## Coding Guidelines
- Zielplattform: .NET 8, WinForms
- Nullable aktiviert – vermeide `!` wo möglich
- Verwende `var` nur wenn Typ rechts eindeutig
- Methoden kurz halten; Hilfsmethoden extrahieren bei > ~40 Zeilen Logik
- Keine externen großen Dependencies für Kleinigkeiten

## UI / Ressourcen
- Neue Strings in beide Sprachdateien (`lang.de.json`, `lang.en.json`)
- Ressourcen unter `ModlistManager/Resources/...` ablegen (automatischer Copy via csproj Include)
- Keine gigantischen eingebetteten Dateien – lieber optionaler Download

## Tests
Aktuell keine automatisierten Tests – bei Logikänderungen gern kleine Helper-Klassen so bauen, dass spätere Tests möglich wären.

## Lizenz
Mit dem Einreichen eines PR stimmst du zu, dass dein Beitrag unter MPL-2.0 lizenziert wird.

## Security / Vertrauen
Keine externen Netzwerkaufrufe ohne vorherige Diskussion. Kein automatisches Herunterladen / Ausführen fremder Binärdateien.

## Release Flow (geplant)
1. `CHANGELOG.md` Eintrag unter "Unreleased" ergänzen
2. Version im `.csproj` erhöhen (SemVer: PATCH für Fixes, MINOR für neue Features, MAJOR für Breaking Changes)
3. Tag erstellen: `vX.Y.Z`
4. Release Workflow erzeugt Artefakte (ZIP) – manuell beschriften

Danke für deine Unterstützung! 🙌
