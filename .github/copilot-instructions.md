# Copilot Instructions für ETS2ATS.ModlistManager

## Architekturüberblick
- Das Projekt ist eine Windows-Desktop-Anwendung (.NET, WinForms) zur Verwaltung von Modlisten für ETS2/ATS.
- Hauptkomponenten:
  - `ModlistManager/Forms/Main/MainForm.cs`: Zentrale UI-Logik und Einstiegspunkt.
  - `ModlistManager/Models/AppSettings.cs`: Modell für Anwendungseinstellungen.
  - `ModlistManager/Services/`: Service-Klassen für Sprache, Einstellungen und Theme.
  - Ressourcen (Icons, Logos, Sprachdateien) liegen unter `ModlistManager/Resources/` und `Resources/`.
  - Externe Tools wie `SII_Decrypt.exe` werden über `ModlistManager/Tools/` eingebunden.

## Entwickler-Workflows
- **Build:**
  - Standard-Build über Visual Studio oder `dotnet build ETS2ATS.ModlistManager.csproj`.
- **Debug:**
  - Debugging erfolgt typischerweise über Visual Studio.
- **Tests:**
  - Keine expliziten Testdateien gefunden; Test- und Debug-Logik ist meist in die UI integriert.

## Projektkonventionen
- **Sprachunterstützung:**
  - Sprachdateien liegen unter `ModlistManager/Resources/Languages/` und `Languages/` (z.B. `lang.de.json`).
  - Sprachlogik wird über `LanguageService.cs` und `MainForm.Language.cs` gehandhabt.
- **UI-Design:**
  - UI-Elemente werden in Designer-Dateien (`*.Designer.cs`) gepflegt.
  - Ressourcen wie Icons und Logos sind in eigenen Unterordnern organisiert.
- **Einstellungen:**
  - Einstellungen werden über `AppSettings.cs` und `SettingsService.cs` verwaltet.
- **Externe Tools:**
  - Für Mod-Entschlüsselung wird `SII_Decrypt.exe` verwendet.

## Integration & Kommunikation
- Services werden direkt in den Forms verwendet (z.B. Sprach- und Theme-Service).
- Ressourcen werden über die jeweiligen Service-Klassen geladen.
- Keine komplexe externe API-Integration; Fokus liegt auf Dateiverarbeitung und UI.

## Beispiele für typische Muster
- Sprachwechsel:
  - Siehe `MainForm.Language.cs` und `LanguageService.cs` für dynamisches Umschalten der UI-Sprache.
- Einstellungen laden/speichern:
  - Siehe `SettingsService.cs` und `AppSettings.cs`.
- Ressourcen laden:
  - Icons/Logos über Pfade in `Resources/Icons/` und `Resources/Logos/`.

## Hinweise für AI Agents
- Änderungen an UI-Elementen sollten in den Designer-Dateien und der zugehörigen Logik erfolgen.
- Neue Services oder Modelle sollten unter `ModlistManager/Services/` bzw. `ModlistManager/Models/` angelegt werden.
- Sprachdateien müssen konsistent in beiden Sprachordnern gepflegt werden.
- Externe Tools immer im `Tools/`-Verzeichnis referenzieren.

---

*Bitte Feedback geben, falls bestimmte Workflows, Konventionen oder Integrationspunkte fehlen oder unklar sind!*
