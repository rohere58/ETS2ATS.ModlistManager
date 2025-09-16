# FAQ – ETS2/ATS Modlist Manager

Welcome to the **FAQ**. This document supports *Markdown* and automatic table of contents.

## Table of Contents
- [General](#general)
- [Installation & Folder Layout](#installation--folder-layout)
- [Getting Started](#getting-started)
- [Modlists](#modlists)
- [Backup & Restore](#backup--restore)
- [Editing & Notes](#editing--notes)
- [Links & Download Management](#links--download-management)
- [Logos & Branding](#logos--branding)
- [Language & Theme](#language--theme)
- [Performance](#performance)
- [Troubleshooting](#troubleshooting)
- [Limitations](#limitations)
- [Roadmap / Ideas](#roadmap--ideas)

## General
**Q:** Why are mods no longer marked as available/missing?  
**A:** That detection was removed to keep the UI lean.

**Q:** Does the tool auto-sync Steam Workshop mods?  
**A:** No. It focuses on managing / sharing lists, not acquiring content.

**Q:** Is it portable?  
**A:** Yes. Copy the whole folder (including `modlists/`).

## Installation & Folder Layout
```
<App folder>
	ETS2ATS.ModlistManager.exe
	modlists/              <- Lists (.txt, .json, .note, .links.json)
	Resources/             <- Logos, languages
		Logos/
		Languages/
	Tools/                 <- External helpers (SII_Decrypt.exe)
```
Updates: replace the EXE; user files stay.

## Getting Started
1. Choose game (top left)  
2. Select profile  
3. Create or import a modlist  
4. Add a description / note  
5. Optionally add download links

## Modlists
**Q:** Where are my modlists stored?**  
In the `modlists` folder.

**Q:** How do I share a modlist?**  
Menu: Modlists → *Share…*

**Files per modlist:**
- `<name>.txt` – plain list  
- `<name>.json` – internal structure  
- `<name>.note` – note / description  
- `<name>.links.json` – download links

## Backup & Restore
Use "Backup & Restore" to archive or re-import whole profiles.  
`profile.sii` restore = quick emergency import.

## Editing & Notes
Bottom note field auto-saves (debounced).  
Empty note deletes the `.note` file.  
Undo link appears after certain actions.

## Links & Download Management
Context menu in the grid: add/remove link.  
Links can be merged across lists.  
Validation is minimal (scheme + rough host pattern).

## Logos & Branding
ETS2 priority: `Resources/Logos/logo.png` → `ets2.png` → fallback.  
ATS override (planned) similar.  
Banner transparency: code property `BackgroundOpacity`.

## Language & Theme
Options → Language / Theme. Restart recommended for consistent visuals.

## Performance
- DataGridView scales well to a few hundred rows  
- Animation is lightweight (60 ms timer)  
- No background scanning threads

## Troubleshooting
Common causes:
- Profile path not found
- `profile.sii` not decrypted
- Missing write permissions

Check log area. (Planned: `--debug` flag.)

## Limitations
- No physical mod file presence check (removed)  
- No Steam Workshop API integration  
- No diff between two lists  
- No multiplayer sync

## Roadmap / Ideas
- ATS logo override (`logo_ats.png`)  
- Enhanced ZIP export (note + links)  
- Markdown note editor  
- Filter / quick search above grid  
- Favorites / recent lists  
- Extended theming

---
*This FAQ will evolve – feedback welcome.*
