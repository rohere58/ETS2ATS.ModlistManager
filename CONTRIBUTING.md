# Contribution Guide
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
