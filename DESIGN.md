# Design — MoneyRight

The durable visual system. Product truth lives in [PRODUCT.md](PRODUCT.md); strategy for a single
route lives in its surface brief under `.impeccable/surfaces/`.

Direction round seed key `5b555ef5` (scope: direction, mode: operate). The roll assigned
The Approach Plate; the user chose the pick card, **The Ruled Cash Book**. Established on
2026-08-27 with the budget-status home screen, replacing Pico.css.

## The world

The bound household cash book the app replaces. **Identity lives in the ruling system, not the
type** — the page is recognizable with all content removed:

- Pale **ledger-green** stock, the accounting paper's real material (deliberately not cream).
- **Oxblood** vertical column rules and correction marks.
- Hairline horizontal rules between entries; a **double rule** under every figure that closes.
- Rank carried by weight, case, width and rule — never by a coloured container.

### The rule is the gauge

The signature device. The hairline beneath a ledger entry doubles as its measure: an ink segment
spans the proportion of the limit spent, and an oxblood tick marks where the month says you should
be today. Nothing is a progress bar added on top of the row — the rule was already there.

Over the limit, the segment switches to oxblood hatching and the entry's relation line reads in
oxblood. **The row is never tinted.**

## Palette

Light-only. Forced by the physical scene: a bright kitchen table and a bright grocery aisle.
Strategy is **Restrained** — neutrals plus one signal, correct for an Operate surface whose
household has legibility on record as a real preference.

| Token | Value | Role | Contrast on `--paper-field` |
|---|---|---|---|
| `--paper` | `#e6ebe0` | page ground | — |
| `--paper-field` | `#f2f5ee` | ruled field, panels | — |
| `--paper-raised` | `#fafbf7` | inputs | — |
| `--paper-sunk` | `#dde4d5` | wells, scrollbar track | — |
| `--ink` | `#16180f` | body, figures | 16.3:1 |
| `--ink-2` | `#43483a` | secondary | 8.6:1 |
| `--ink-3` | `#5e6553` | tracked caps labels | 5.5:1 |
| `--rule-hair` | `#c3cdb8` | entry rules, gauge track | non-text |
| `--rule` | `#a9b79c` | panel borders | non-text |
| `--rule-strong` | `#7e8f70` | input borders, section rules | non-text |
| `--signal` | `#8e2a1b` | over-limit, focus, caret, selection | 7.6:1 |
| `--signal-soft` | `#e9d8d3` | column rules, alert ground | non-text |

Every text token clears WCAG AA at 4.5:1. Secondary tones are **tinted from the paper's hue**,
never gray.

**Colour is quarantined** to rules, marks and the exception counts. The money column stays
achromatic at full contrast — the single rule that keeps this from becoming a fintech dashboard.

## Type

One self-hosted variable family: **Archivo Variable** (`@fontsource-variable/archivo/wdth.css`),
weight 100–900, width 62–125%. Self-hosted is a hard requirement, not a preference: this app runs
on a LAN with no internet, so a webfont CDN would simply fail.

- Base **17px** / 1.55. Larger than convention because readable type is a stated household need.
- `tabular-nums lining-nums` globally, so money columns align everywhere.
- Column heads, labels and buttons: 0.705rem, weight 650, width 86%, tracking 0.105em, uppercase.
- Headline figure: `clamp(2.6rem, 1.6rem + 4.4vw, 4.1rem)`, weight 700, tracking -0.032em.
- Measure capped at 68ch.

Rank is made from weight, case, width and rule — never from more type sizes.

## Component vocabulary

- **`.spread`** — a month of the ledger: head band, headline figure, exceptions, ruled sections.
- **`.entry` / `.gauge`** — one ruled row and its rule-gauge. `data-over` switches the hatching.
- **`.exceptions`** — what the book does not yet know, printed as rows. Never badges or toasts.
- **`.memo`** — a marginal note, set off by a single left rule.
- **`article`** — a ruled panel; its first heading is separated by a rule.
- **`.masthead`** — the spine: wordmark, sections, household member. Closed by a double rule.
- **`.slip`** — the login surface, a single ruled slip.

Buttons are tracked caps with square corners; `.secondary` is outlined, `.contrast` is oxblood.
Work in progress is a **struck rule** drawn under the control, not a spinner.

## Browser surfaces

Themed rather than left to the browser: text selection (oxblood ground), caret, focus rings
(2px oxblood, offset), scrollbars (rule-strong on sunk paper), select chevrons drawn in CSS,
placeholder colour, and link underline offset.

## Motion

One authored moment: `spread-turn`, the ledger spread arriving when the month changes —
420ms exponential ease-out from an already-visible default. Gauge fills interpolate at 520ms.
Everything collapses under `prefers-reduced-motion`.

## Adaptation

Mobile carries **budget status**; reporting and data entry are desk work.

Below 46rem the three money columns collapse to the actual figure plus a relation line
("155.35 left · 77.93 under pace"), section heads stack rather than fight their descriptors,
and sections separate by **real void** instead of by rule alone. Verified with no horizontal
overflow at 360px.

## Refused by this world

Card grids, donut charts, delta arrows, progress rings, sparklines, coloured status pills,
tinted rows, gradient text, glass and blur, kickers above headings, rounded containers,
emoji or unicode glyphs standing in for icons, and dark mode (the use scene is bright;
light-only is a decision, not an omission).

## Provenance

Six catalog challengers were fused and judged before any borrowing. Two held as competitive
(The Departure Board, The Lexicon Spread); four were declined, and each donated one discipline
now built into this system:

- **Scale-contrast courage** (variable type specimen) — the headline figure sets at display
  scale while labels drop to tracked caps. No uniform ledger grey.
- **States print themselves** (phosphor terminal) — exceptions are entries in the document.
- **Colour quarantine** (iridescent cloud edge) — band edges and marks only; text stays achromatic.
- **Real void between sections** (cloud quarry) — the narrow-screen separation rule.
- **One type size, rank by weight/case/rule** (departure board, competitive) — the tabular discipline.

No shipping rasters: this build contains no bitmap assets. The typeface is bundled from
`@fontsource-variable/archivo` (SIL Open Font License).
