---
version: 1
slug: "client-src-pages-ignorerulespage-tsx"
primary_target: "client/src/pages/IgnoreRulesPage.tsx"
related_targets: ["client/src/pages/ImportPage.tsx"]
---

# Surface brief — The import reading (`/import` station 3, `/import/rules`)

Covers **ticket 10** (issue #10, amended) and **tickets 13–14** of the
[Import Assignment Rules spec](https://github.com/jsteinshouer/get-my-money-right/blob/main/docs/specs/import-assignment-rules.md).
Produced by `shape` and confirmed by the user on 2026-09-25.

**Rewrites this surface's ticket #9 brief** rather than sitting beside it: the surface is the same
one, grown. Everything #9 shipped that still holds is carried forward below — the strike, the
tally, the from-row slip, the matching semantics. Two of #9's anti-goals are **deliberately
reversed** by the spec, and a builder reading the closed #9 issue will find the old wording:

1. *"a rule that also assigns a category"* was an anti-goal. It is now the entire feature.
2. *"No override. A struck row is skipped; if that is wrong, the rule is wrong."* still holds for
   **skipping**. It does **not** hold for classification: a row's category, Need/Want and tags are
   correctable in place before the import commits.

Established visual world:
**The Ruled Cash Book** ([DESIGN.md](https://github.com/jsteinshouer/get-my-money-right/blob/main/DESIGN.md)).
No new visual direction. Domain vocabulary is in
[CONTEXT.md](https://github.com/jsteinshouer/get-my-money-right/blob/main/CONTEXT.md); the two
decisions this rests on are [ADR-0001](https://github.com/jsteinshouer/get-my-money-right/blob/main/docs/adr/0001-uncategorized-is-a-real-state.md)
and [ADR-0002](https://github.com/jsteinshouer/get-my-money-right/blob/main/docs/adr/0002-assignment-rules-apply-at-import-only.md).

## 1. Job and audience

**Visitor mode: Operate.** Desk session, laptop, the monthly ritual. Not held to the 400px bar.

The same household member as #9, at the same station, but the station's job has changed twice over.
In #9 they were checking what the app would *throw away*. Now they are reading what it will
**file** — and, for the first time, the app is showing that it has learned something. The first
import after writing a handful of rules is the moment the product either earns the monthly ritual
or doesn't.

State of mind is still **suspicion, not curiosity** — but a second feeling arrives with it:
*impatience*. A 400-row file that classifies itself is only trustworthy if the household can see,
without scrolling all of it, how much it got right and what is left over.

## 2. Outcome and proof

They end knowing four things and able to act on all of them without leaving the page:

- which rows are going in, **and how each one will be filed**
- which rows nobody has taught the app about yet, and therefore what the review queue will cost them
- which rows are skipped, and by which rule
- which rows are duplicates already in the ledger (ticket 10)

Product truth no generic importer carries: **Need/Want is a per-transaction judgment that may not
be absent** (PRODUCT principle 3). A row nobody has classified does not get to be blank — it prints
as a **Want** and says so before it lands, because a guess made in the open is honest and a guess
made silently is not. That single line of type is the product's whole position on discretionary
spending, stated on the screen where it is decided.

## 3. Selected direction — the reading, now classified

Station 3 is still `.reading` — the reads-as block, carrying every row. The composition does not
change. **What each row carries under it does.**

```
Aug 14   Kroger #442                       −84.19
         GROCERIES · NEED · contains KROGER

Aug 15   Delta Air Lines 0062             −412.80
         TRAVEL · WANT · +VACATION 2026 · 2 rules

A̶u̶g̶ ̶1̶5̶   A̶u̶t̶o̶p̶a̶y̶ ̶T̶h̶a̶n̶k̶ ̶Y̶o̶u̶              +̶3̶1̶2̶.̶0̶0̶
         SKIPPED · contains AUTOPAY

Aug 16   Trader Joe's #221                 −63.40
         NO CATEGORY · WANT · for review

Aug 17   Shell Oil 5578                    −41.02
         GROCERIES · NEED · your correction
═══════════════════════════════════════════════════
38 classified · 6 for review · 3 skipped · 2 duplicates
```

**One device, four states.** The second line is the same tracked-caps register #9 established for
`Skipped · contains AUTOPAY`. It is real text, not an ARIA label — screen readers and sighted
readers get the same sentence — and it reads in `--ink-3` except where it carries a correction
mark. Four forms, and no fifth:

| Row state | Second line |
|---|---|
| Classified by rules | `GROCERIES · NEED · contains KROGER` |
| Classified, several rules contributed | assignments, then `· 2 rules` |
| Nothing matched | `NO CATEGORY · WANT · for review` |
| Skipped | `SKIPPED · contains AUTOPAY` (unchanged from #9) |
| Corrected by hand | assignments, then `· your correction` in `--signal` |

Tags print as `+VACATION 2026` in the same register, after the Need/Want. **Colour stays
quarantined**: the line is achromatic in every form but the correction mark, which is the one place
the cash book has always used oxblood — the household's own pen on the page.

**Citation rule.** One contributing rule is named in full. Two or more collapse to `· 2 rules`,
because naming three rules in a row's margin turns a ledger into a log; the slip names them all
when opened. An over-broad rule stays diagnosable either way, which is the point #9 established.

**The money grid is untouched.** Date, description and amount keep the columns and alignment they
have had since #8. Adding category and Need/Want as columns was considered and declined: it
squeezes the description, moves the money column off its established position, and leaves the rule
citation nowhere to live.

### The focal moment — one slip, three outcomes

Every row carries **one** trailing control, `What is this?`, including struck rows (whose slip opens
on the skip branch, so an over-broad ignore rule is reachable from the row it wrongly caught). It
opens the `CorrectionSlip` idiom beneath that row: the entry stays on its line, the correction is
written on a slip under it, as a real form.

```
Aug 16   Trader Joe's #221                 −63.40
         NO CATEGORY · WANT · for review   [ What is this? ]
┌─────────────────────────────────────────────────┐
│ THIS ROW IS                                     │
│  [ Groceries ▾ ]  [ Need ▾ ]  [ + tag ]         │
│                                                 │
│ APPLIES TO                                      │
│  ( ) this row only                              │
│  (•) every row where the description            │
│      [ contains ▾ ] [ TRADER JOE'S        ]     │
│      for [ All accounts ▾ ]                     │
│  ( ) skip rows like this instead                │
│                            [ Save ] [ Cancel ]  │
└─────────────────────────────────────────────────┘
```

This is the structural thesis of the ticket: **at the row, there is one question — what is this? —
and the rule is just how far the answer reaches.** "Import rule, two kinds" stops being a data
model and becomes the thing the household actually touches. The ignore rule is no longer a separate
affordance; it is the third radio.

- *This row only* → a one-off override on the pending import. Writes no rule. The row's second line
  re-reads with `· your correction`.
- *Every row where…* → an assignment rule, prefilled from the row's **normalized description** —
  matching against anything else would classify rows by text the household never saw.
- *Skip rows like this instead* → exactly #9's ignore rule, unchanged in semantics and wording.
  Choosing it disables the assignment fields rather than hiding them, so the slip does not resize
  under the cursor.

**On save with a rule**, the file re-reads and the rows the rule touched acquire their second line
in place — the counts above re-count, `aria-live="polite"`. On the skip branch the pen stroke draws
as it already does. Reuse the strike's curve and duration exactly (~260ms, the `spread-turn`
exponential); **no new motion vocabulary**, and everything collapses to an instant state change
under `prefers-reduced-motion`.

**On save with *this row only***, nothing re-reads but that row. A one-off must feel cheaper than a
rule or nobody will reach for it.

### The close — ticket 10's confirm

#9 ended with a memo saying nothing was saved. That memo goes. The reading now closes under its
double rule with the counts as `.exceptions` entries, then the action:

```
═══════════════════════════════════════════════════
  38 classified                                 38
  for review                                     6
  skipped by a rule                              3
  already imported                               2
                                    [ Import 44 rows ]
```

The button names the number it will write, because a button that says `Import` and a tally that
says 44 are two facts the household has to reconcile themselves. After it runs, the same block
prints the result — `Imported 44 · skipped 3 by a rule · skipped 2 as duplicates` — plus a `.memo`
pointing at the work it created: *"6 transactions are waiting for a category."* linking to the
review queue. That memo is the handoff from import to the monthly ritual, and it is the only place
the two connect.

### `/import/rules` — two ruled sections

The page keeps its route and its shape, and grows a second section. Classify first, skip second:
the household writes ten of the first for every two of the second, and the page should open on the
thing they came for.

```
IMPORT RULES

RULES THAT CLASSIFY A ROW
─────────────────────────────────────────────────────
contains KROGER            Groceries · Need    All    Delete
contains DELTA             Travel · Want       All    Delete
contains DELTA             +Vacation 2026      All    Delete
starts with SQ *           Need                Amex   Delete

RULES THAT SKIP A ROW
─────────────────────────────────────────────────────
contains AUTOPAY                               All    Delete
contains TRANSFER TO SAVINGS              Checking    Delete
```

Each rule reads back as a sentence — `contains KROGER` in the match column, its assignments beside
it, its scope, its delete. A rule that sets only Need/Want or only a tag prints only that; the
column does not pad with dashes. Global rules first, then by account name, matching #9. Each section
has its own add form above it and its own ruled empty state. Delete stays the inline `.confirm`
panel, never a browser dialog.

## 4. Scope and boundaries

**Ticket 10 — build:** the confirm operation and its counts; the optional-category migration; the
Want default; the `.exceptions` close, the Import action, the result block and the review-queue
memo; duplicate marks on rows.

**Ticket 13 — build:** the assignment rule entity and its create/fetch/delete operations; the shared
matcher; the classify section on `/import/rules` with its add form, sentences and empty state.

**Ticket 14 — build:** resolution across the whole file; the second line in all its forms; the
one-slip-three-outcomes affordance; one-off overrides carried into confirm.

**Untouched:** stations 1 and 2 and every #8 API contract; the masthead (it stays at seven sections
— `/import/rules` is still reached by a `.memo`, not a nav item); budgets; accounts; auth; the
strike device and its reason wording; the money grid.

**Anti-goals:** modals, toasts, badges or pills for any of this; tinted or coloured rows; a
category column in the reading; per-row un-skip as a separate control (it is the slip's third
radio); regex or wildcard match text; auto-suggested rules; a rules preview that guesses what the
household might want; virtualization or pagination of the reading; any second authored motion
beyond the strike's curve; a confirmation dialog on Import.

## 5. States and ranges

**Ranges:** assignment rules **0–40**, realistically 5–25 after a few months, against ignore rules'
2–6. Files of 20–2000 rows, all printed. Match text 3–40 characters. Tags per rule 0–3. Categories
5–25.

- **No assignment rules yet** (the state on day one) — every row reads `NO CATEGORY · WANT · for
  review` and the tally reads `0 classified · 44 for review`. This is not an error and must not be
  dressed as one. The classify section's empty state names where rules come from: *"No rules yet.
  Write one from a row in an import preview."*
- **Rules exist but match nothing in this file** — silent. A rule that matched nothing is not worth
  a line of the household's attention on this screen.
- **Every row classified** — `44 classified · 0 for review`. The review memo after import does not
  print when the count is zero.
- **A rule and an ignore rule both match a row** — the strike wins and no assignments print; the
  row is leaving either way, and two outcomes would read as a contradiction.
- **A duplicate row that rules also classified** — duplicate mark wins in the same way.
- **A row that fails to parse** — unchanged from #9: the error prints, no assignments, no slip
  branch for classification (there is nothing to classify), but the rule branches stay reachable.
- **Rule saved that matches zero rows** — the save succeeds, the tally does not move, and a
  `.field-error`-register note in the slip's place says so plainly: *"Saved. Nothing in this file
  matches it."* Silence here reads as a bug.
- **Rule saved that matches everything** — the existing `.note[data-signal]` device from #9, adapted:
  the rule is almost certainly too broad, and it is named.
- **Duplicate rule** (same match text, type, scope and kind) — refused at the field.
- **A rule setting nothing** — Save is unavailable until one of category, Need/Want or a tag is set,
  with the reason in the tracked-caps register rather than on hover.
- **Override then rule** on the same row — the rule wins for fields it sets and the second line
  re-reads; the household's `· your correction` mark persists only for fields the rule left alone.
- **Saving** — the struck rule under the control, never a spinner.
- **409 / write conflict** — existing handling; SQLite is single-writer and both users may be at the
  desk.
- **Narrow** — `.table-scroll` as inherited; the second line wraps rather than truncating, since it
  is the row's meaning and an ellipsis would hide the rule's name.

## 6. Constraints and open decisions

**Binding:** React 19 + TS; new vocabulary extends `client/src/styles/ledger.css`; Minimal API REPR,
one file per operation under `Features/Import/`; EF Core + SQLite; light-only; fonts self-hosted.

**Fixed, not the builder's to choose** (from the spec and ADRs):

- Matching is **description-only**, case-insensitive, against the normalized description, with
  `MatchType` of `Contains | StartsWith | Equals`. No raw CSV columns.
- Resolution across several matching rules: category and Need/Want from the **first matching rule by
  id** that sets them; tags **union**. Same first-by-id convention #9 established.
- Pipeline order: map → ignore (terminal) → dedupe → assign → insert.
- An unmatched row imports with **no category** and **Want**.
- Rules are create/fetch/delete only. **No edit, no active toggle** — delete is the off switch, as
  with ignore rules. A builder must not invent an update endpoint.

**Cross-surface consequence to honor:** deleting a category that an assignment rule references
returns a conflict. The categories page must name how many rules use it, in the same plain register
its transaction conflict already uses. That surface has no brief; do not redesign it, just make the
message name the count.

**A builder must not invent:** amount or date conditions on a rule, rule priority or reordering,
bulk apply from the rules page, automatic reclassification when a rule is saved, a reviewed flag
separate from the absent category, suggested rules, a fourth station, or a masthead entry for
`/import/rules`.

**Tests:** `WebApplicationFactory` integration tests per operation, plus fixture CSVs covering a
multi-rule row, a first-by-id collision, an ignore-beats-assign row, and unmatched rows landing with
no category and Want. Playwright: extend `client/e2e/import-ignore-rules.spec.ts` for the slip's
three branches, and add the full classified-import flow — upload → map → preview showing assigned
rows citing their rules → override one row → import → the result counts and the review memo.

**Confirmed with the user (2026-09-25):**

1. **Row print** — a second line under the entry, not columns in the grid, and unmatched rows print
   their line too rather than staying bare.
2. **Row affordance** — one slip, three outcomes; the ignore rule becomes its third radio.
3. **Rules page** — two ruled sections on the one route, classify first.

**Assumed, correct freely:** the `What is this?` control wording; classify-before-skip ordering on
the rules page; the `· 2 rules` collapse at two or more citations; `· your correction` as the
override mark; the Import button naming its row count; the review-queue memo's wording.
