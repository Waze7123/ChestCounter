---
name: Contador de Baús
description: Native Windows loot ledger for Fortnite chests, warm black and gold.
colors:
  ground: "#0C0B09"
  rail: "#110F0C"
  surface: "#15130F"
  hover: "#1D1A15"
  line: "#2A251C"
  gold: "#E3B04B"
  gold-bright: "#F6D27E"
  gold-deep: "#5A4419"
  gold-wash: "#261F11"
  on-gold: "#181206"
  text: "#F1E9D8"
  muted: "#A69B85"
  faint: "#8A806C"
  dim: "#60584A"
  live: "#78D68C"
  error: "#E8705C"
  mark-rare: "#D6A0FF"
  mark-relic: "#7EB6FF"
  mark-buried: "#CD9662"
  mark-poster: "#96D6A0"
typography:
  display:
    fontFamily: "Bahnschrift SemiBold, Segoe UI"
    fontSize: "50pt"
    fontWeight: 600
  headline:
    fontFamily: "Bahnschrift SemiBold, Segoe UI"
    fontSize: "22pt"
    fontWeight: 600
  title:
    fontFamily: "Segoe UI Variable Display, Segoe UI"
    fontSize: "15pt"
    fontWeight: 700
  heading:
    fontFamily: "Segoe UI Semibold"
    fontSize: "11pt"
    fontWeight: 600
  body:
    fontFamily: "Segoe UI Variable Text, Segoe UI"
    fontSize: "9.5pt"
    fontWeight: 400
  body-strong:
    fontFamily: "Segoe UI Semibold"
    fontSize: "9.5pt"
    fontWeight: 600
  label:
    fontFamily: "Segoe UI Variable Text, Segoe UI"
    fontSize: "8.5pt"
    fontWeight: 400
rounded:
  control: "7px"
  pill: "8px"
  day: "6px"
  bar: "3px"
spacing:
  gutter: "28px"
  cell: "10px"
  ledger-col: "16px"
  chip-gap: "6px"
components:
  button-primary:
    backgroundColor: "{colors.gold}"
    textColor: "{colors.on-gold}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.control}"
    height: "32px"
    padding: "0 14px"
  button-primary-hover:
    backgroundColor: "{colors.gold-bright}"
  button-primary-disabled:
    backgroundColor: "{colors.gold-deep}"
  button-ghost:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    height: "32px"
    padding: "0 14px"
  button-ghost-hover:
    backgroundColor: "{colors.hover}"
  chip:
    backgroundColor: "{colors.ground}"
    textColor: "{colors.muted}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    height: "28px"
    padding: "0 11px"
  chip-selected:
    backgroundColor: "{colors.gold-wash}"
    textColor: "{colors.gold-bright}"
  nav-item:
    backgroundColor: "{colors.rail}"
    textColor: "{colors.muted}"
    typography: "{typography.body}"
    rounded: "{rounded.pill}"
    height: "38px"
  nav-item-active:
    backgroundColor: "{colors.gold-wash}"
    textColor: "{colors.gold-bright}"
    typography: "{typography.body-strong}"
  ledger-row:
    backgroundColor: "{colors.ground}"
    textColor: "{colors.text}"
    typography: "{typography.body}"
    height: "30px"
    padding: "0 10px"
  ledger-row-selected:
    backgroundColor: "{colors.gold-wash}"
    textColor: "{colors.gold-bright}"
---

# Design System: Contador de Baús

## Overview

**Creative North Star: "The Loot Ledger"**

A warm-black ledger in which gold is the only voice. The chest count leads every view in large condensed numerals; everything else is quiet, tinted context drawn on one flat ground and divided by hairlines rather than boxed into cards. The signature device is the loot strip: a match drawn as a timeline with one gold diamond per chest.

The app is native WinForms painted by hand in GDI+ (DPI mode DpiUnawareGdiScaled, so sizes below are authored at 96 DPI and scaled by the system). Density is compact on purpose: it sits beside a game and must not steal space or attention. Motion is short and eased (ease-out expo): the count rolls up, bars grow, hover fades in.

**Key Characteristics:**
- One flat warm-black ground; depth by tone (rail, surface, hover), never shadow.
- Gold carries selection, the count, the primary action and the chest marks, nothing else.
- Numerals in Bahnschrift, UI in Segoe UI Variable.
- Hairline dividers (1px Line) instead of cards.
- Rounded corners only on interactive controls.

## Colors

Warm blacks graded in small steps, one gold family, text tinted from the gold, and a rarity-coded mark series.

### Primary
- **Ledger Gold** (gold): the count, primary button fill, chest marks, today's bar, the rail brand word, Settings section headings.
- **Lit Gold** (gold-bright): hover of gold elements, selected text (chips, nav, ledger rows, menus), focus rings, the hovered mark.
- **Deep Gold** (gold-deep): disabled primary fill, selected chip border, today outline in the calendar, unlocked overlay border.
- **Gold Wash** (gold-wash): selection ground for nav pill, selected chip, selected ledger row, menu highlight.
- **Ink on Gold** (on-gold): text on gold fills.

### Tertiary (data series)
- **Rarity marks** (mark-rare purple, mark-relic blue, mark-buried bronze, mark-poster green): the colour of a non-plain opening in loot strips and legends, following Fortnite rarity. Plain chests stay Ledger Gold; any other extra falls back to Muted.

### Neutral
- **Ground** (ground): window, pages, ledgers, overlay.
- **Rail** (rail): navigation rail and the dark title bar caption.
- **Surface** (surface): ghost button fill, menus, calendar drop-down.
- **Hover** (hover): hover ground for rows, nav items, chips, calendar days.
- **Hairline** (line): dividers, control borders, chart axis, title-bar border.
- **Warm White** (text): primary text and secondary ledger values.
- **Muted** (muted): captions, labels, inactive nav and chip text, title-bar text.
- **Faint** (faint): the low-contrast text tier: column headers, axis labels, empty states, disabled button text, hollow (uncounted) marks.
- **Dim** (dim): strokes and disabled states only; never text.
- **Live** (live) / **Error** (error): status dot and overlay caption only, where the state is real.

**The One Gold Rule.** Gold means "this is the count, the chosen thing, or the action". Never use it for decoration, borders at rest, or icons in idle state.

**The Tinted Neutral Rule.** Every neutral leans warm toward the gold; pure greys are not in the system.

**The Text Floor Rule.** Text uses Text, Muted or Faint. Faint is the lowest text tier; Dim and Line are never text colours.

## Typography

**Display Font:** Bahnschrift SemiBold (falls back to Segoe UI)
**Body Font:** Segoe UI Variable Text (falls back to Segoe UI)
**Title Font:** Segoe UI Variable Display Bold

**Character:** a condensed DIN face with tabular figures for every number, against the native Windows UI face for words. Numbers read as instruments; words read as the OS.

### Hierarchy
- **Display** (Bahnschrift SemiBold, 50pt, gold): the main count on Painel only.
- **Headline** (Bahnschrift SemiBold, 22-24pt): ledger values on Painel, the overlay's match count; 18pt for the overlay total.
- **Title** (Segoe UI Variable Display Bold, 15pt): page title in the top bar.
- **Heading** (Segoe UI Semibold, 11pt): section titles ("Última partida", "Baús por dia").
- **Body** (Segoe UI Variable Text, 9.5pt; Semibold for buttons' primary and active nav): all running UI text.
- **Label** (Segoe UI Variable Text, 8.5pt): captions, column headers, axis labels, status. Bar values use Bahnschrift Regular 8.5pt.

**The Numbers Are Bahnschrift Rule.** Every count, average or score is set in Bahnschrift; never in Segoe UI.

## Layout

A fixed left rail (172px, Rail ground, 1px Line right edge) holds the brand mark, nav items (38px tall, 2px apart, inset 10px) and the status at its foot. The content column has a 66px top bar (title left; "Desde" date picker, period chips and the primary "Analisar" button right) closed by a hairline inset 28px each side. Pages pad 28px left/right, 8px top, 24px bottom. The Painel reads top-down: count with its period line, an inline ledger of four columns split by vertical hairlines (values inset 16px), the full-width last-match loot strip, then the daily bars. Table cells inset 10px. The overlay is a fixed 282x74 strip: chest mark, match count, vertical hairline, total.

## Elevation & Depth

Flat. No shadows anywhere. Depth is tonal: Ground < Rail < Surface < Hover, and selection is a Gold Wash fill. Hierarchy comes from type size and gold, not from lifted surfaces.

**The Flat Ledger Rule.** Group content with hairlines and spacing; never with drop shadows or boxed cards.

## Shapes

Corners are reserved for things you can press: buttons and chips 7px, nav pill and hovered nav items 8px, calendar days and the overlay lock button 6px, chart bars up to 3px at the top. Panels, ledgers, the overlay frame and dividers are square. Icons are drawn line glyphs on an 18-unit grid, 1.6 stroke, round caps and joins. Marks are diamonds: filled for counted openings, hollow (1.2px) for extras.

## Components

### Buttons
- **Shape:** gently rounded (7px), 32px tall, 1px border in the fill or Line.
- **Primary:** Ledger Gold fill, Ink on Gold Semibold text; hover blends to Lit Gold; disabled is Deep Gold.
- **Ghost:** Surface fill, Line border, Warm White text; hover blends to Hover with border warming toward Faint.
- **Focus:** inner 1.5px Lit Gold ring inset 2px (radius 5px), keyboard only.
- **Motion:** hover fades in 120ms, out 180ms.

### Chips
- **Style:** 28px tall, Ground fill, Line border, Muted text (period filters: Sempre, Hoje, 7 dias, Este mês).
- **Selected:** Gold Wash fill, Deep Gold border, Lit Gold text.

### Navigation
- Rail items: line icon (18px) + label, Muted at rest, warming to Warm White on hover over a Hover fill. Active item sits on a Gold Wash pill with Lit Gold icon and Semibold label; the pill slides between items (260ms).

### Ledger (tables)
- Owner-drawn list on Ground, 30px rows, header in Label/Faint over a hairline, faint row separators, Hover on hover, Gold Wash with Lit Gold text on selection. Dark native scrollbars.

### Toggle
- Pill track (radius = half height); knob slides 220ms; track border warms from Line toward the on colour; Lit Gold focus ring.

### Loot Strip (signature)
- A hairline track for match time with minute ticks in Faint. Each counted opening is a filled diamond in its kind colour (plain chests gold); extras are hollow diamonds (gold if counted, Faint if not); hovered mark turns Lit Gold and its caption appears top-right. Marks reveal over 700ms. Used full-width on Painel, mini in each Partidas row, and in match detail.

### Daily Bars
- Bars in a gold mix (Deep Gold to Gold at 72%), today in full Gold, hovered in Lit Gold; values above in Bahnschrift 8.5pt; bars grow over 520ms on period change.

### Overlay
- Ground strip, square 1px border (Line when locked, Deep Gold when movable), opacity 40-100%. Caption turns Live while a match is being read.

## Do's and Don'ts

### Do:
- **Do** lead every view with the number in Bahnschrift, gold only for the headline count.
- **Do** separate groups with 1px Line hairlines and the 28px gutter.
- **Do** show selection as Gold Wash fill plus Lit Gold text.
- **Do** colour non-plain openings by Fortnite rarity (rare purple, relic blue, buried bronze, poster green) and keep plain chests gold.
- **Do** ease animations out (exponential) and skip them when Windows UI effects are off.

### Don't:
- **Don't** build grids of equal stat cards with icon tiles; use the inline hairline ledger.
- **Don't** add drop shadows or raised cards.
- **Don't** round non-interactive containers.
- **Don't** use pure greys, or Dim/Line for any text.
- **Don't** spend gold on decoration or idle icons.
