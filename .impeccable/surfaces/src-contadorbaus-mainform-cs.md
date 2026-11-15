---
version: 1
slug: "src-contadorbaus-mainform-cs"
primary_target: "src/ContadorBaus/MainForm.cs"
related_targets: ["src/ContadorBaus/OverlayForm.cs"]
---

# Surface: Contador de Baús — janela principal + overlay

Mode: Operate. Native Windows app (WinForms, GDI+). Users: Fortnite player between matches (main window) and during matches (overlay). Job: know how many chests were opened, per match/day/mode/team, from a chosen date. Constraints: black & gold pinned by user; compact; Portuguese (pt-PT); counts update only when a match ends.

## Direction contract

THESIS: A loot ledger, not a stat-card dashboard. The count leads and every match is drawn as its own rhythm of gold marks over time; refuses the grid of equal stat cards with icon tiles.

OWN-WORLD: Warm black ground #0C0B09, raised surface #15130F, hover #1D1A15, hairlines #2A251C. Gold #E3B04B carries selection, the count and the marks; bright gold #F6D27E for hover/focus; deep gold #5A4419 for fills. Text warm white #F1E9D8, secondary #A69B85 (tinted, never grey). Bahnschrift for numerals (condensed, tabular), Segoe UI Variable for UI. Hairline dividers instead of boxed cards; 8px corners only on interactive controls.

STORY: Open the app, read the number, see the last match's rhythm, then drill into matches, modes, team, or adjust what counts.

FIRST VIEWPORT: Left rail 168px (drawn line icons + labels, active item on a dim-gold pill). Top bar: period ("Desde [date ▾]" + chips Sempre/Hoje/7 dias/Mês) and status at right. Painel: count 60pt gold at left with its period line; inline ledger of Hoje/Partidas/Média/Melhor separated by hairlines; full-width last-match loot strip; daily bars below.

FORM: Pinned by user brief (black & gold, clean, smooth, compact); a pinned direction beats the roll, so concept-seed was not run. Status moved to the rail foot and a primary "Analisar" button added for manual rescans (product truth). Mark colours follow Fortnite rarity (rare purple, relic blue, buried bronze, poster green) as a data series; plain chests stay gold. Signature move: the loot strip — a match timeline with a gold diamond per chest and hollow marks for extras, used on Painel, in every Partidas row (mini) and in the match detail. Motion: count-up of the main number and bars easing in on period change (ease-out expo, ~500ms), hover fades 120ms.
seed: none (unavailable)

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
