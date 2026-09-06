---
target: editing a transaction
total_score: 17
max_score: 40
na_heuristics: 
p0_count: 2
p1_count: 4
timestamp: 2026-09-03T12-30-57Z
slug: client-src-pages-transactionspage-tsx
---
Method: dual-agent (A: design review · B: detector + browser evidence)

Scope: editing a transaction — the `EditRow` in `client/src/pages/TransactionsPage.tsx`.

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Busy state good; no dirty indicator; errors print at page top, far above the row. |
| 2 | Match System / Real World | 2 | Reading rows are ledger; edit mode is a web form. The tinted `is-editing` row contradicts DESIGN.md. |
| 3 | User Control and Freedom | 1 | Escape does nothing; no click-away; a second Edit click silently discards unsaved work. |
| 4 | Consistency and Standards | 1 | Two layouts, three field orders, two input idioms for one entity. |
| 5 | Error Prevention | 1 | Not a `<form>`, so `required` is inert; clearing Amount saves 0.00; Delete has no confirm. |
| 6 | Recognition Rather Than Recall | 2 | Column heads label the inputs, but `thead` is not sticky — deep in the queue the fields are unlabelled. |
| 7 | Flexibility and Efficiency | 2 | No Enter-to-save, no Escape, no tab-to-next-row; 14 tab stops from Date to Save. |
| 8 | Aesthetic and Minimalist | 2 | Seven controls in a reading grid; Amount stops being a money column. |
| 9 | Error Recovery | 2 | Save-failure separation is genuinely good; messages never name the offending field. |
| 10 | Help and Documentation | 2 | The "Negative for money out" hint exists on Add, absent on Edit — same field. |
| **Total** | | **17/40** | **Poor — the interaction needs restructuring, not polish** |

## Design Specificity Verdict

**Category-interchangeable, and the only surface in the app that is.** The reading table is authored — oxblood column rules, hairline entry rules, the closing double rule, tags as underlined marginal marks. Click Edit and the ruled cash book vanishes: seven boxed inputs in a tinted row with a generic Save/Cancel pair. That is the default CRUD datagrid every admin template ships. Two direct violations of the committed world: `tr.is-editing` and `tr[data-selected]` are **tinted rows**, which DESIGN.md refuses by name ("The row is never tinted").

**Deterministic scan:** detector returned `[]`, exit 0 — zero findings. Every defect below is runtime geometry the static scan cannot see.

## Overall Impression

You are right that it is wonky, and the measurements say exactly why: **six different vertical tops across a 72px spread, three different control heights, and an Amount box too small to show its own value.** But the structural problem is bigger — the household's actual desk job is changing two fields, and the row opens a form for eight.

## What's Working

1. **The failure separation in `handleSave`** — a failed transaction save keeps edits on screen; a failed tag save closes and reloads so what stuck is visible.
2. **`TagLine`** — in-world, O(1) in tag count, create-on-Enter. The one control that survived this critique intact.
3. **The sticky closing column** keeps Save reachable — though see P1 below, it now costs something.

## Priority Issues

**[P0] Clearing Amount silently writes 0.00.** The edit row is not inside a `<form>`, so `required` is inert; the guard checks only account/category/Need-Want; `Number('')` is `0`; and the server has no Amount rule. The one column the product exists to total can be zeroed without a word — and the closing figure I added last round will then report a confidently wrong total. *Fix:* validate before save, reject empty/NaN, print the message beside the field.

**[P0] You cannot see the amount you are editing.** Measured: the Amount input is **61px wide for 78px of content** — it renders `-25` for a value of `-25.50`. Description is 130px for 222px. *Fix:* falls out of the restructure below.

**[P1] The edit surface is the wrong shape.** PRODUCT.md says categorisation is a batch worked as a queue, where imported rows already carry date, amount, description and account — what is missing is **Category and Need/Want**. Two tiers: (a) make those two directly editable in the reading row, committing on change with the struck-rule busy state, so the queue is two taps per row and the ledger never reflows; (b) move full correction to a **correction slip** — a `colSpan={9}` ruled panel opening beneath the entry, the entry staying in place marked with an oxblood left rule. Rejected: a modal (DESIGN.md refuses it, and the queue must stay visible), and a route per transaction (filters are not in the URL, so it would destroy the investigation's filter state).

**[P1] A second Edit click destroys unsaved work.** Observed: typed a new description, clicked Edit on another row, the edit vanished with no warning and no alert. A filter change does the same.

**[P1] Two regressions from my own last round.** (a) The sticky Actions column **permanently hides 37.9px of the Tags cell on desktop and 54.8px on mobile** — the table is already at max scroll, so that content is unreachable. (b) `.tag-combobox input:focus-visible { outline: none }` removed the focus ring from the tag input; every other control kept its 2px oxblood ring.

**[P1] Delete is one unguarded click on real ledger data**, while deleting a *tag* — a label — got a counted confirmation last round. The safeguards are inverted.

**[P2] Alignment and rhythm.** `td { vertical-align: baseline }` plus unequal control heights gives 6 distinct tops and ~70px of dead space under the field line. The Amount input computes `text-align: start` inside a `td.money` that computes `right`, so figures jump left on Edit and right on Cancel. Opening Edit widens the table by 157px, so sideways scrolling starts *because you clicked Edit*.

## Persona Red Flags

**Alex (power user):** No Enter-to-save (no form), no Escape, no tab-to-next-row. **14 tab stops from Date to Save** — the date input alone eats 4, and the tag suggestion `<ul>` is itself a tab stop because it scrolls.

**Sam (accessibility):** Inputs carry `aria-label` only; the visible label is a non-sticky `thead` cell with no programmatic association. `role="alert"` fires at the top of a 6086px document while focus sits in a row far below. The tag input has no focus ring.

**The household, joint review at one screen:** one of them clicks Edit to reclassify a purchase as Want; the row swells, the entries below jump, and the line they were both reading moves. The screen stops supporting the conversation.

**The household, phone check-in:** below 46rem, Account and Need/Want are `display:none`. Their values are preserved on save, so nothing corrupts — but you are editing a form with two invisible fields and nothing says so.

## Minor Observations

- The Add form and the edit row describe one entity in **two layouts and three field orders**, and sit **5003px apart** on the page — they can never be compared.
- The Add form has no tag field at all, so a cash purchase cannot be tagged as it is entered.
- Escape's meaning depends on focus: inside the tag combobox it clears the combobox; elsewhere nothing. Worse than absent.
- Save is disabled while a tag is being created, but the reason is never stated.
- `.col-select` goes empty in edit mode — the ruled column continues over nothing.

## Questions to Consider

1. If the desk session only ever changes Category and Need/Want, why does correcting a row open a form for all eight fields?
2. What did a cash book do when an entry was wrong? It didn't rewrite the line — it struck it and wrote a correction. Where is that here?
3. Which layout is the transaction's real shape — the row or the Add form? One of them should go.
4. Delete needs no confirmation but deleting a tag does. What does that ordering say about what the app thinks is valuable?
5. If Escape doesn't cancel, is Cancel an exit, or just a button?
