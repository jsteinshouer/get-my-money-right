---
version: 1
slug: "client-src-pages-shellpage-tsx"
primary_target: "client/src/pages/ShellPage.tsx"
related_targets: []
---

# Surface brief — Budget status home screen (`/`)

Confirmed by the user on 2026-08-27. Produced by `shape`; direction round seed key `5b555ef5`
(scope: direction, mode: operate). The roll assigned The Approach Plate; the user selected the
pick card, The Ruled Cash Book. A user-pinned decision beats the roll.

## Job and audience

**Visitor mode: Operate.**

Two people, one household, self-hosted on their own network. Four confirmed arrival contexts:

- **Sunday desk session** (laptop) — dense work, long focus.
- **Grocery-aisle glance** (phone) — three seconds, one question.
- **Joint review** — one screen, two people, discussing discretionary spending.
- **Ad hoc investigation** — a question that came up once.

This surface becomes `/`: what login lands on, and what the phone opens to.

## Outcome and proof

The household learns **whether it is on track this month, and what the app does not yet know.**

Success is that a glance answers it and the answer is *complete* — never "all green" while real money
left the house unseen. The surface owns two product truths no neighboring product carries: a Need/Want
judgment on every transaction, and honest accounting of the two blind spots ticket 06 left open.

**Scope decisions confirmed in discovery** (all three added beyond ticket 06):

1. **Unbudgeted spending** is surfaced — categories with real spend but no limit set.
2. **The uncategorized queue** is surfaced — transactions with no category yet.
3. **The Need/Want split** is surfaced — absent from the current view entirely.

## Selected direction — The Ruled Cash Book

**World.** The bound household cash book the app replaces. Identity lives in the **ruling system**, not
the type, and is recognizable with all content removed:

- Pale **ledger-green** stock — the accounting paper's actual material, deliberately not a cream default.
- **Oxblood vertical column rules**; desaturated green horizontal rules.
- A **double rule** under every figure that closes.
- Tracked small-caps column heads; tabular lining figures throughout.

**Colour strategy: Restrained** — ledger-green ground, black ink, one oxblood signal. Forced by the
physical scene (a bright kitchen table; a bright grocery aisle), so the design is authored **light-first**.

### The pace answer — a third ruled column

The pick's honest risk was that a ledger has no native way to express *ahead or behind on day 19*.
The resolution is ledger-native rather than a bolted-on progress bar:

For the **current month only**, the ledger carries three money columns:

```
LIMIT  ·  EXPECTED TO DATE  ·  ACTUAL
```

`EXPECTED TO DATE` is the limit pro-rated by elapsed days. Ahead-or-behind becomes a comparison between
two adjacent money columns — the single most ledger-native operation there is. Past months drop the
column entirely and the spread closes with `CARRIED FORWARD`.

### Raises carried from the challengers this direction beat

Each is a transferred discipline, never borrowed clothes. One world owns the page.

- **from the variable type specimen** — scale-contrast courage. Ledgers are traditionally uniform; here
  the household total sets at display scale and column heads drop to tracked small caps. No uniform grey.
- **from the phosphor terminal** — exceptions **print themselves as entries in the ledger**. Unbudgeted
  spend is a ruled `NO BUDGET SET` section; the uncategorized queue is a memorandum line. Never floating
  badges, chips, or toasts.
- **from the iridescent cloud edge** — colour **quarantined to rules and marginal marks**. The money
  column stays achromatic at full contrast. No red-tinted rows — which is also the anti-fintech move.
- **from the cloud quarry** — on phones, sections separate by **real void**, not by rule alone.
- **from the departure board** (competitive) — one type size for all tabular data; rank carried by
  weight, case, and rule.

## Scope and boundaries

**Build:** the status surface at `/`, replacing the `ShellPage` stub. Establishes the token layer and
component vocabulary that tickets 07–11 inherit, replacing Pico.css.

**Also changes:** `/budgets` becomes **management** (setting, editing, deleting limits). Reading and
editing are separated, per the confirmed discovery answer.

**Untouched:** all API contracts, the `BudgetWithActual` shape, auth, routing beyond `/`, and every
backend behavior.

**Anti-goals:** donut charts, delta arrows, rounded card grids, gamified encouragement, coloured status
pills, and any tinted-row status encoding.

## States and ranges

Real ranges from `src/Api/Data/DemoHousehold.cs`: 6 categories, limits $120.00–$600.00, ~12–15
transactions per month, 3 months of history. Design for **1–20 categories**.

Material states:

- **First run** — no categories, no budgets, no transactions. Three distinct empties, not one.
- Budgets set, zero spend (start of month).
- Typical mid-month, mixed under/over.
- **Post-import** — a large uncategorized queue; the queue's loudest moment.
- Unbudgeted spend present.
- **Refund-negative category** — `actual` can go below zero. Ticket 06 established the data stays
  honest and only the progress indicator clamps. The seeder contains one (a cancelled concert refund).
- Past-month view (no `EXPECTED TO DATE` column).
- Loading; error; HTTP 409 write conflict (SQLite is single-writer).
- Long category names.

## Interaction and layout

**Hierarchy:** elapsed-days band → household total against limit → the two exception counts → category
entries → unbudgeted section → Need/Want proportion.

- Month navigation is a **ledger convention** (previous/next spread), not a date picker.
- Every category entry links through to its filtered transactions.
- **Phone:** the three money columns collapse to figure-plus-relation; sections gain void; no horizontal
  scroll above ~400px. Mobile carries budget status only — reporting and data entry are desk work.
- Feedback stays print-native: a marginal mark, a struck rule. Never a coloured pill.

## Constraints and open decisions

**Binding:** React 19 + TypeScript (Vite); no CSS framework after this pass. `tabular-nums` throughout.
Readable type and strong contrast are a stated user preference, not a checkbox — this constrains exactly
where financial UIs usually compress hardest. Playwright e2e covers this surface, and existing specs
assert on exact cell text, so **selectors will need revisiting**.

**Execution contract: code-led.** No image generation is available in this environment, so there is no
comp and no toggle; ambition is carried by the direction contract and audited at the finish review.

**Resolved before the build (2026-08-27):**

1. **Dark mode** — declined. Light-only; the use scenes are bright.
2. **Nav label** for `/` — "This month".
3. **The app's name** — **MoneyRight**.

**Built.** The surface ships at `/`, `/budgets` is management-only, Pico.css is gone, and the
visual system is recorded in DESIGN.md.

## Round record

- Grounded candidates derived: 7, spanning ruled stationery, domestic enamel object, transactional
  print, graphic standard, industrial hardware, kraft packaging, and technical chart.
- Assigned by roll: index 7 (The Approach Plate). **Selected: the pick card (The Ruled Cash Book).**
- Challengers judged before any borrowing: 2 competitive (The Departure Board, The Lexicon Spread),
  4 declined (variable type specimen, phosphor terminal, iridescent cloud edge, cloud quarry).
  Every declined challenger donated one discipline, listed above as a named raise.
- Standing exit (the category standard) was offered and not taken.
