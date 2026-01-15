# ETS2ATS.ModlistManager v0.1.13

## Highlights
- Notiz-Fix: Footer-Notiz wird nicht mehr von der Lokalisierung überschrieben und korrekt in `<ModlistName>.note` gespeichert/geladen.
- Sauberer Statusbereich: Meldungen erscheinen nur noch im Log-Bereich (kein doppelter/abgeschnittener Label-Text).
- Robustes Grid-Parsing: `Package` = vor `|`, `Modname` = nach `|` (inkl. Quotes-Handling für `active_mods`).

## Änderungen
- Lokalisierung: Englische Basis wird überlagert; Directory-Priorität bevorzugt jetzt `Resources\Languages` vor `Languages` und gepackten Ressourcen.
- Notizfeld: Übersetzter Platzhaltertext, ohne den Benutzerinhalt zu verändern.

## Hinweise
- Optionales Tool `SII_Decrypt.exe` wird nicht mitgeliefert. Bei Bedarf manuell unter `ModlistManager/Tools/` oder `Tools/` ablegen.
- Modlisten liegen standardmäßig unter `Dokumente/<Game>/modlists` oder in dem in den Optionen gewählten Ordner.

## Assets
- GitHub Release: https://github.com/rohere58/ETS2ATS.ModlistManager/releases/tag/v0.1.13