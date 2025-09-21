# FAQ – ETS2/ATS Modlist Manager

Willkommen zur **FAQ**. Diese Datei unterstützt *Markdown* und ein automatisches Inhaltsverzeichnis.

## Inhalt
- [Allgemein](#allgemein)
- [Installation & Ordnerstruktur](#installation--ordnerstruktur)
- [Erste Schritte](#erste-schritte)
- [Modlisten](#modlisten)
- [Backup & Restore](#backup--restore)
- [Bearbeiten & Notizen](#bearbeiten--notizen)
- [Links & Download-Verwaltung](#links--download-verwaltung)
- [Logos & Branding](#logos--branding)
- [Sprache & Theme](#sprache--theme)
- [Performance](#performance)
- [Fehlerbehebung](#fehlerbehebung)
- [Limitierungen](#limitierungen)
- [Roadmap / Ideen](#roadmap--ideen)

## Allgemein
**F:** Warum werden manche Mods nicht mehr als vorhanden/fehlend markiert?  
**A:** Die frühere Erkennungslogik wurde entfernt, um die Oberfläche zu vereinfachen.

**F:** Unterstützt das Tool automatische Workshop-Synchronisation?  
**A:** Nein – Fokus liegt auf Verwaltung / Teilen von Modlisten, nicht auf Downloads.

**F:** Ist das Programm portable?  
**A:** Ja. Einfach gesamten Programmordner kopieren (inkl. `modlists/`).

## Installation & Ordnerstruktur
```
<Programmordner>
	ETS2ATS.ModlistManager.exe
	modlists/              <- Modlisten (.txt, .json, .note, .links.json)
	Resources/             <- Logos, Sprache
		Logos/
		Languages/
	(optional) Tools/      <- Externes Zusatztool (SII_Decrypt.exe) falls manuell genutzt
```
Updates: EXE ersetzen. Eigene Dateien bleiben erhalten.

## Erste Schritte
1. Spiel auswählen (links oben)  
2. Profil wählen  
3. Modliste erstellen oder importieren  
4. Beschreibung / Notiz ergänzen  
5. Optional Links hinterlegen (Kontextmenü im Grid)

## Modlisten
**F:** Wo liegen meine Modlisten?**  
Im Unterordner `modlists` neben der EXE.

**F:** Wie teile ich eine Modliste?**  
Menü: Modlisten → *Weitergeben…*

**Dateien pro Modliste:**
- `<name>.txt` – Zeilenbasierte Liste  
- `<name>.json` – Interne strukturierte Daten  
- `<name>.note` – Beschreibung / Notiz  
- `<name>.links.json` – Download-Links

## Backup & Restore
Unter "Backup & Restore" ganze Profile sichern / wiederherstellen.  
`profile.sii`-Restore = schneller Minimalimport.

## Bearbeiten & Notizen
Notizfeld unten speichert automatisch (mit Verzögerung).  
Leeren führt zum Entfernen der `.note` Datei.  
Undo-Link erscheint nach bestimmten Aktionen.

## Links & Download-Verwaltung
Kontextmenü im Grid: Link hinzufügen / entfernen.  
Links zusammenführbar über Listen.  
Validierung ist einfach (Schema + grober Host).

## Logos & Branding
Priorität ETS2: `Resources/Logos/logo.png` → `ets2.png` → Fallback.  
ATS (geplant) analog.  
Banner: Transparenz einstellbar (Code-Property `BackgroundOpacity`).

## Sprache & Theme
Optionen → Sprache / Theme. Neustart empfohlen für konsistente UI.

## Performance
- DataGridView – performant bis einige hundert Zeilen  
- Animation moderat (Timer 60 ms)  
- Kein permanenter Hintergrundscan

## Fehlerbehebung
Typische Ursachen:
- Profilpfad nicht gefunden
- `profile.sii` nicht entschlüsselbar
- Fehlende Schreibrechte

Log-Bereich prüfen. (Geplante Option: `--debug` Startparameter.)

## Limitierungen
- Keine physische Mod-Datei-Prüfung (Feature entfernt)  
- Keine Steam Workshop API  
- Kein Diff zwischen zwei Listen  
- Keine Multiplayer-Synchronisierung

## Roadmap / Ideen
- ATS Logo Override (`logo_ats.png`)  
- Erweiterter ZIP-Export (Notiz + Links)  
- Markdown im Notizfeld  
- Filter/Schnellsuche im Grid  
- Favoriten / Zuletzt benutzt  
- Erweiterte Themes

---
*Diese FAQ wird laufend erweitert – Feedback willkommen.*
