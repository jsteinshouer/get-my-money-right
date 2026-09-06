# Design session — MoneyRight budget status home screen

**Date:** 2026-08-27 · **Branch:** `feature/moneyright-budget-status-home` · **PR:** [#6](https://github.com/jsteinshouer/get-my-money-right/pull/6) · **Commit:** `f8cccde`

A record of what was decided and why. Not a transcript — the reasoning behind
choices that the code alone doesn't explain.

---

## Outcome

The `/` stub became a ruled ledger spread showing budget status; Pico.css was
replaced by the project's own design system; the app got a name.

Three artifacts were written: `PRODUCT.md` (durable product truth), `DESIGN.md`
(the visual system), and `.impeccable/surfaces/client-src-pages-shellpage-tsx.md`
(the confirmed brief for this one route).

Ran in three phases: **init** → **shape** → **build**.

---

## Phase 1 — init: capturing product truth

The existing budgeting-app spec (then `.scratch/budgeting-app/spec.md`, now `docs/specs/budgeting-app.md`) answered most of it (34 user
stories, entity model, architecture, explicit out-of-scope). Four gaps it did
not answer, all of which changed later work:

| Question | Answer | Why it mattered |
|---|---|---|
| Is Pico.css binding or scaffolding? | **Scaffolding — replaceable** | The spec framed Pico as a deliberate architectural choice, which would have bound every future design pass to working *inside* it. Recording it as scaffolding unlocked the whole redesign. |
| What's the real usage scene? | **All four** — monthly desk session, phone check-ins, joint review, ad hoc investigation | The joint-review scene (two people, one screen, discussing Want spending) is a different design problem than a single operator at a dashboard, and appeared nowhere in the spec. |
| Is "Household Budget" the name? | **Placeholder, open to a name** | A wordmark sits in the header, so it had to be settled before pixels. |
| Accessibility / devices? | Readable type & strong contrast are a **genuine preference**; mobile matters for **checking status, not data entry** | Gave the device split a real boundary instead of "responsive everywhere", and constrained type size exactly where financial UIs usually compress hardest. |

**Derived principle that drove everything after:** *two devices, two jobs.* The
phone answers questions; the desk does the work.

---

## Phase 2 — shape: the brief

### Discovery

Four decisions, all confirmed by the user:

1. **Scope grows past ticket 06.** Surface unbudgeted spending, the
   uncategorised queue, *and* the Need/Want split. Ticket 06's own comments had
   flagged the first two as deliberate omissions — the page could read
   "all green" while real money left the house.
2. **This becomes the home screen** at `/`. `/budgets` becomes management.
3. **Pace-aware, current month only.** "$142 of $600" means something different
   on the 3rd than on the 28th.
4. **Reading and editing separate.** The glance and the desk session want
   different things.

### The direction round

Seed key `5b555ef5` (scope: direction, mode: **operate**).

Seven grounded candidates were derived from the audience's world — household
financial administration and self-hosting culture — spanning seven material
families, ordered by resonance:

1. Ruled household cash book · 2. Mid-century kitchen wall planner ·
3. Utility bill / bank statement · 4. Swiss/DIN administrative form design ·
5. Rack-mount home-lab front panel · 6. Seed packet / cash envelope ·
7. Nautical & aviation chart legibility

**The roll assigned #7** (The Approach Plate) — the lowest-ranked, which is what
the roll exists to do: refuse the model's ranking rut. **The user chose #1**
(The Ruled Cash Book), which had been offered as the pick card. A user-pinned
decision beats the roll.

The pick's stated honest risk was that *a ledger has no native way to say ahead
or behind on day 19* — precisely what the user had just asked for. Resolving
that inside the ledger's own grammar became the brief's central job.

### Challenger verdicts

Six catalog challengers were fused with the product and judged on audience
identification and product clarity **before** any borrowing:

| Challenger | Verdict | Reasoning |
|---|---|---|
| Departure Board (1971 timetable) | **competitive** | One-size-only typography is a superb answer to dense-tables-stay-legible. Lost on its 11° rake, which would wreck a money column — and stripping the rake strips its identity. |
| Lexicon Spread (19c dictionary) | **competitive** | "Rank entries by how much apparatus each is granted" beats any progress bar at *which categories need attention*. Lost on having no time axis, and a shoulder margin is expensive below 400px. |
| Variable type specimen | declined | Weight is not an ordinally readable encoding of money. |
| Phosphor terminal | declined | Green-on-black bloom fights the one stated accessibility preference head-on. |
| Iridescent cloud edge | declined | Pastel low-contrast banding is the opposite of what was asked for. |
| Cloud quarry | declined | Metaphor cost, no clarity return, for a question that must resolve in three seconds. |

**Every declined challenger donated one discipline** to the built direction:

- **Scale-contrast courage** *(type specimen)* — headline at display scale,
  labels dropped to tracked caps. Ledgers are traditionally uniform; this
  fights that.
- **States print themselves** *(phosphor terminal)* — exceptions are entries in
  the document, never floating badges or toasts.
- **Colour quarantine** *(cloud edge)* — colour lives in rules and marks; the
  money column stays achromatic. **This is the anti-fintech rule**: no tinted
  rows, and it came from a challenger that was rejected.
- **Real void between sections** *(cloud quarry)* — the narrow-screen rule.
- **One type size, rank by weight/case/rule** *(departure board)* — the tabular
  discipline.

---

## Phase 3 — build

### The central design move

**The rule beneath an entry *is* the gauge.** Not a progress bar added on top —
the hairline that a ledger already has fills with ink for what was spent, and
carries an oxblood tick marking where the month says you should be today.

Pace is expressed as a **third money column** (`LIMIT · EXPECTED TO DATE ·
ACTUAL`), so ahead-or-behind is a comparison between two adjacent columns, which
is the most ledger-native operation there is. Closed months drop the column.

### Decisions forced by evidence rather than taste

- **Light-only.** Written as a physical scene — a bright kitchen table and a
  bright grocery aisle — which forced the answer. Later confirmed by the user.
- **Ledger-green, not cream.** The accounting paper's actual material. Cream
  would have been the default rendition wearing the subject's clothes.
- **Self-hosted fonts.** The app runs on a LAN with **no internet**, so a
  Google Fonts link would silently fail. Archivo Variable is bundled via
  `@fontsource-variable/archivo` — one family carrying all rank through weight
  (100–900) and width (62–125%).
- **No backend changes.** All three new sections derive client-side in
  `lib/monthSummary.ts` from existing endpoints. For a two-user app with ~15
  transactions a month this is trivially cheap and keeps every API contract
  intact.
- **Palette validated against WCAG AA before it was written into code** — every
  text token clears 4.5:1; ratios are recorded in `DESIGN.md`.

### Defects found in the inspection round

Built fully, inspected once at desktop and mobile together, fixed in one batch,
confirmed once:

1. **~200px dead void** under the headline on desktop — the exceptions column
   was much taller than the text beside it. Fixed by densifying the exception
   rows.
2. **Mobile section heads collapsed** into two cramped columns ("NO BUDGET SET"
   wrapping to three lines against its descriptor). Fixed by stacking below 46rem.
3. **Remaining wasn't shown on desktop** — only on mobile. "How much is left in
   Dining Out?" is *the* household question, and the desktop view couldn't
   answer it. Added under each actual figure, which also let the redundant
   "OVER" flag fold into the relation line.

A fourth issue turned out not to be a defect: leftover `E2E …` categories were
test pollution in `e2e.db`, not a bug. Rebuilt the database to confirm.

---

## Tests

`dotnet test` **101/101** · `npm run test:e2e` **11/11**

- The ticket 06 spec was rewritten against the status page and now additionally
  asserts the gauge's over-state and that a closed month carries no pace column.
- **Nav clicks are scoped to the sections nav.** Playwright's role-name matching
  is substring-based, so `{ name: 'Transactions' }` was also matching the
  exceptions link ("All 18 **transactions** categorised"). Worth remembering.
- The old "Welcome, {name}" assertions were replaced — that heading no longer
  exists; the spread itself is the authenticated landing surface.

---

## Still open

- **The uncategorised counter reads 0 and will until ticket 10.**
  `Transaction.CategoryId` is non-nullable, so those rows can't exist yet. The
  state and its finished reading ("All 18 transactions categorised") are built
  and ready; changing the entity belongs to the CSV import ticket.
- **Nav label "This month"** was my choice, not specified. Easy to change in
  `AppLayout.tsx`.
- **`origin` still points at `jsteinshouer/budget`** and GitHub is redirecting to
  `get-my-money-right`. Consider `git remote set-url`.
- Tickets **07–11** now inherit the token layer in `client/src/styles/ledger.css`.

---

## Where things live

| Path | What |
|---|---|
| `PRODUCT.md` | Durable product truth — users, scenes, positioning, constraints |
| `DESIGN.md` | The visual system, palette with contrast ratios, component vocabulary |
| `.impeccable/surfaces/client-src-pages-shellpage-tsx.md` | The confirmed brief for this route |
| `client/index.html` | The direction contract, as an HTML comment that survives the production build |
| `client/src/pages/StatusPage.tsx` | The ledger spread |
| `client/src/lib/monthSummary.ts` | All month derivation in one place |
| `client/src/styles/ledger.css` | Ledger component vocabulary |
| `client/src/index.css` | Tokens, reset, base elements, themed browser surfaces |
