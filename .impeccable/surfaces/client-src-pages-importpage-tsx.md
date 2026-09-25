---
version: 1
slug: "client-src-pages-importpage-tsx"
primary_target: "client/src/pages/ImportPage.tsx"
related_targets: []
---

# Surface brief — CSV import: upload & column mapping (`/import`)

Ticket #8 of 12 (https://github.com/jsteinshouer/get-my-money-right/issues/8), where this brief is
also published. Produced by `shape` and confirmed by the user on 2026-09-07.

Established visual world:
**The Ruled Cash Book** ([DESIGN.md](https://github.com/jsteinshouer/get-my-money-right/blob/main/DESIGN.md)).
No new visual direction — this surface extends the existing ruling system into a flow it has not yet
had to carry. Product truth lives in
[PRODUCT.md](https://github.com/jsteinshouer/get-my-money-right/blob/main/PRODUCT.md).

## 1. Job and audience

**Visitor mode: Operate.** Desk session only.

One of two household members, laptop, at the start of the monthly ritual. They have just downloaded a
CSV from a bank or card website and have no idea what its columns are called — the bank chose those
names, not them. Two arrival states, and the second is the common one:

- **First import for this account.** The file is unknown. They must tell the app how to read it.
- **Every import after that.** The mapping is remembered; they are here to confirm nothing changed and
  move on. This path must feel like *nothing to do*.

Mobile is explicitly out: PRODUCT principle 1 puts import on the desk. The surface must not break on a
phone, but it is not held to the 400px bar.

## 2. Outcome and proof

The household member ends **certain the app is reading their file correctly** — before a single row is
written.

Proof is not a validation checkmark. It is the file's own first rows, re-printed as the app will store
them: parsed date, normalized description, sign-normalized amount. Product principle 2 — *trust is built
from counts, not assurances* — applies before the counts exist: the honest version at this stage is
showing the reading back.

Product truth this surface owns, which no generic importer carries: **the mapping is per account and
permanent**, so a bank that formats exports one way is described once and never again.

## 3. Selected direction — the document is the form

Roles are assigned **onto the sampled rows**, not in a form beside them.

```
┌──────────────┬──────────────┬───────────────────────┬──────────┐
│ [ DATE    ▾] │ [ —       ▾] │ [ DESCRIPTION      ▾] │ [AMOUNT▾]│
│ Posted Date  │ Ref No.      │ Payee                 │ Amount   │
├──────────────┼──────────────┼───────────────────────┼──────────┤
│ 08/14/2026   │ 4471         │ KROGER #442           │   -84.19 │
│ 08/15/2026   │ 4472         │ SHELL OIL 5578        │   -41.02 │
│ 08/15/2026   │ 4473         │ AUTOPAY THANK YOU     │   312.00 │
└──────────────┴──────────────┴───────────────────────┴──────────┘
════════════════════════════════════════════════════════════════
        reads as →  Aug 14, 2026 · Kroger #442 · −84.19
```

- The CSV's first **5 data rows** print as a ruled ledger table, verbatim, in the file's own column
  order. Nothing is hidden or reordered.
- Each **column head cell carries its role selector** (tracked caps select):
  `DATE · DESCRIPTION · AMOUNT · DEBIT · CREDIT · —`. The bank's own header text sits beneath it in
  `--ink-3`, smaller. You map by looking at the data.
- **The assignment mark is the oxblood vertical column rule.** DESIGN.md's signature device does the
  work: an assigned column is ruled down both sides in `--signal-soft`; an unassigned column carries no
  rule and its sample cells drop to `--ink-3`. Mapped/unmapped is carried by *ruling*, never by a tinted
  cell, a badge, or a green check. Colour stays quarantined.
- A **double rule closes the sample block**, and beneath it the first row prints once more in the app's
  own ledger formatting — `Aug 14, 2026 · Kroger #442 · −84.19`. This is the focal moment and the entire
  feedback loop of the screen. It is a live region.
- **Debit/Credit** is not a radio pair in a form. Assigning `DEBIT` to a column makes `CREDIT` the
  offered role for the next unassigned numeric column; the reads-as line shows the sign normalization
  happening (`Withdrawal 84.19` → `−84.19`). Assigning `AMOUNT` retires both.
- **Delimiter, date format and header-row flag are detected, not asked.** They appear as a `.memo` in
  plain words — "Read as comma-separated, with a header row, dates as month/day/year" — with an inline
  control to correct any of the three. Rationale: a household member does not know what `MM/dd/yyyy`
  means, and the reads-as line already tells them when the guess is wrong. The controls exist for that
  moment, not before it.

**The wizard shell** is a ruled station band, not a stepper: three tracked-caps stations across one
horizontal rule, closed by a double rule. Current station in `--ink` at weight 650 with an oxblood
underline; completed stations carry the correction mark already in the vocabulary; **`PREVIEW & CONFIRM`
sits in `--ink-3` and is plainly inert** — labelled as not yet built, so #9 and #10 fill the frame rather
than replace it. No circled numbers, no connector chevrons, no percentage.

```
UPLOAD  ——  MAP COLUMNS  ——  PREVIEW & CONFIRM
  ✓            ●              (not yet built)
```

## 4. Scope and boundaries

**Build — client:** `/import` route, a seventh masthead section labelled **Import**, placed after
*Transactions* (it is the monthly session's first move and feeds that page). Wizard shell + step 1
(Upload) + step 2 (Map columns). Step 3 rendered inert.

**Build — API:** `CsvImportMapping` entity and migration; upload-and-preview (returns a short-lived
token, the header row, and sample rows — inserts nothing); save-mapping; get-mapping. Preview state is
the transient in-memory cache the spec calls for, keyed by that token.

**Untouched:** the status screen, transactions, budgets, categories, tags, accounts, auth, and every
existing API contract. **Nothing is inserted into `Transactions` by this ticket.**

**Deferred by design, and the frame must not need changing to add them:** ignore-rule strike-through in
the preview (#9); dedupe, sign-normalized bulk insert, and the three result counts (#10).

> Ticket #9 fills station 3. Its brief is
> [client-src-pages-ignorerulespage-tsx.md](client-src-pages-ignorerulespage-tsx.md) — read it before
> touching the preview station or the reading block.

**Anti-goals:** pill or circle steppers, progress bars, two-list drag-to-connect mapping, modal dialogs,
toasts, tinted valid/invalid cells, green checkmarks, "Success!" copy, and any encouragement. Voice stays
plain: controls name their action, errors name the problem and the recovery.

## 5. States and ranges

Ranges to design against: **3–12 CSV columns**, header names 4–24 characters, files of ~20–2000 rows,
**5 sample rows shown**.

Material states:

- **No accounts yet** — a ruled empty naming the prerequisite, linking to Accounts.
- **Upload, nothing chosen** — account select + file field in a ruled well.
- **First import for this account** — every role unassigned; nothing is guessed. (Header-name guessing is
  deliberately declined: a wrong guess the user doesn't notice is worse than an honest blank.)
- **Remembered mapping** — every role pre-filled on arrival, memo reading "Remembered from your last
  import for *Sapphire Card*, Aug 14 2026." The screen's job is confirmation, and it must read that way.
- **Remembered mapping no longer fits this file** — a column the saved mapping names is absent from this
  header. That role clears, and the memo says so in `--signal`: names the missing column, states that it
  must be re-assigned. This state is implied by the acceptance criteria but not spelled out in it; it is
  the one that will actually happen when a bank changes its export.
- **Role reassigned** — assigning a role already held elsewhere moves it, and the vacated column visibly
  loses its oxblood rule. No silent duplicates.
- **Incomplete mapping** — save is refused with the requirement named at the field (`.field-error`), not
  at the top of the page.
- **Unreadable amount or date under the current format** — the reads-as line prints the failure in
  oxblood and points at the format memo. This is what the format controls exist for.
- **File rejected** — not a CSV, empty, header only, single column, oversized. Each named specifically.
  UTF-8 BOM is absorbed silently.
- **Saving** — the struck rule already in the vocabulary. Never a spinner.
- **Error / 409** — the existing conflict handling.
- **Many columns** — the map table inherits `.table-scroll`; the page itself never scrolls sideways.

## 6. Interaction and layout

Hierarchy: station band → account and file (step 1) → format memo → the sampled ledger with its role
heads → the reads-as line under the double rule → save.

- Step 2's primary action in this build is **Save mapping**; it closes with a memo stating the mapping is
  saved and that importing rows is not built yet. In #10 the same control becomes *Save and preview* — a
  label change, not a redesign.
- Moving back to step 1 (different file, same account) keeps the mapping.
- Each role select carries an accessible name naming its column ("Role for column *Posted Date*"). The
  reads-as line is `aria-live="polite"`.
- Below 46rem the sample table scrolls sideways inside its box; the upload step stacks. No new
  mobile-specific composition.
- Motion: the existing struck rule for work in progress; no new authored moment. Everything collapses
  under `prefers-reduced-motion`.

## 7. Constraints and open decisions

**Binding:** React 19 + TS (Vite), no CSS framework; new vocabulary extends
`client/src/styles/ledger.css` rather than living in the page. ASP.NET Core Minimal API, REPR — one file
per operation under `Features/Import/`. EF Core + SQLite; explicit `DateFormat` parsed with
`InvariantCulture`. Fonts stay self-hosted. Light-only.

**Tests:** `WebApplicationFactory` integration tests per operation including the pre-fill case; a new
Playwright spec (`client/e2e/import-mapping.spec.ts`) covering upload → map → save → re-upload → mapping
remembered, using the per-run DB reset convention. CSV fixtures needed: single signed-amount column, and
separate debit/credit columns.

**A builder must not invent:** guessed role assignments from header names, any auto-import, a Transfer
concept, category assignment during import, or a fourth wizard step.

**Confirmed with the user (2026-09-07):**

1. **Ship scope** — the full three-station wizard shell now, with `PREVIEW & CONFIRM` present but inert.
2. **Entry point** — a new `Import` nav section, with the account chosen inside step 1.
3. **Map step composition** — roles assigned onto the sampled rows, not a mapping form beside them.

**Assumed, correct freely:** nav placement after *Transactions*; 5 sample rows; detection-with-correction
for delimiter/date/header rather than three required fields.
