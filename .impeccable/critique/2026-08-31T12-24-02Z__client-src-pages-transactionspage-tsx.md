---
target: tag editing on transactions
total_score: 19
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 4
timestamp: 2026-08-31T12-24-02Z
slug: client-src-pages-transactionspage-tsx
---
Method: dual-agent (A: design review · B: detector + browser evidence)

Scope: the **tag-editing interaction** on the transactions page, not the whole page.

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 2 | Struck-rule busy state is right; nothing ever reports how many transactions a tag touches. |
| 2 | Match System / Real World | 3 | Vocabulary is exact; the control is a web form, not a book annotation. |
| 3 | User Control and Freedom | 2 | Cancel works; no undo on tag delete, and creating a tag costs your filters. |
| 4 | Consistency and Standards | 2 | Two tag controls on one page (select to filter, checkboxes to edit); the Add form has none. |
| 5 | Error Prevention | 1 | `/tags` Delete cascades across every transaction with no confirm and no count. |
| 6 | Recognition Rather Than Recall | 2 | Fine at n=3; at n=27 measured, alphabetical scan, no search, applied tags not surfaced. |
| 7 | Flexibility and Efficiency | 1 | No bulk tagging, no keyboard path, single-tag filter. The primary scene is unsupported. |
| 8 | Aesthetic and Minimalist Design | 1 | Measured: edit row 118px → 863px at 27 tags. 7.3x the normal row. |
| 9 | Error Recovery | 3 | Partial-failure copy is honest; doesn't name which tags. |
| 10 | Help and Documentation | 2 | The `/tags` help line is excellent — and nowhere near the point of use. |
| **Total** | | **19/40** | **Poor — major overhaul of this interaction** |

## Design Specificity Verdict

**Split, and the wrong half is authored.** The read state is genuinely MoneyRight: `.tag-mark` is tracked caps with a hairline oxblood underline, a marginal annotation pencilled under a ledger entry, correctly refusing the coloured pill DESIGN.md bans. The edit state is a generic CRUD checkbox list — a bare fieldset of native checkboxes whose only MoneyRight property is the inherited `accent-color`, and `.tag-option` exists solely to undo the design system's label type. Any Rails scaffold ships this control.

**Deterministic scan:** CLI detector returned `[]`, exit 0 — zero findings across all four files. The static scan is silent here; every real problem is behavioural and only appears under load.

**Browser overlay:** injection succeeded on `/transactions` with an edit row open; console reported 5 anti-patterns. Four are false positives (`all-caps-body` x4 is the deliberate tracked-caps voice; `em-dash-overuse` is 66 instances of the `—` empty-tag placeholder across 57 rows; `layout-transition` resolves to a UA stylesheet, not project CSS). One is real: `undersized-ui-text` at 9.99px on the Amount hint — pre-existing, not tag-related.

## Overall Impression

The tag *mark* is one of the best details in the app. The tag *picker* is the weakest control in it, and the gap only becomes visible at a tag count the tests never reach. Biggest opportunity: the control is answering "which tags does this row have?" when the household's actual question is "how much did the vacation cost?"

## What's Working

1. **`.tag-mark` as annotation, not badge** — reuses the system's own rule vocabulary instead of importing a pill. Stays inside colour quarantine.
2. **Separated save paths in `EditRow`** — a failed transaction save keeps edits on screen; a failed tag save closes and reloads.
3. **`.tag-none` renders an em-dash** — the book states "nothing here" rather than leaving silence.

## Priority Issues

**[P0] The investigation scene is unserved.** Tagging is one row per Edit→check→Save cycle, and the tag-filtered list shows no total. PRODUCT.md's "How much did the vacation actually cost?" means tagging ~30 rows, then getting a number. Neither exists. *Fix:* row selection plus "Tag selected…" above the table, and a `tfoot` total on the filtered list, reusing the existing idempotent PUT.

**[P1] You cannot create a tag where you need one.** "Vacation 2026" doesn't exist until the investigation invents it; the picker renders "No tags yet." as a dead end and creating requires leaving for `/tags`, which discards all filters (they are local state, never written to the URL). *Fix:* type-ahead tag line with create-on-Enter; meanwhile sync filters via `setSearchParams`.

**[P1] The control collapses under its own data — measured.** At 27 tags the edit row goes 118px → 863px, 27 checkboxes on 27 lines. `flex-wrap` never engages because each option is `white-space: nowrap` in a 192px cell. No search, no max-height, no hoisting of applied tags. 33 tab stops between Need/Want and Save. *Fix:* the tag line makes this O(1) in tag count.

**[P1] Tag delete is silent, unconfirmed and cascading.** One click detaches the tag from every transaction carrying it. Directly violates PRODUCT.md Principle 2 ("nothing disappears silently"). *Fix:* return a usage count from `GET /api/tags`, show it, confirm with "Remove 'Vacation 2026' from 23 transactions?"

**[P1] No responsive handling for an 8-column table.** Measured at 390px: the table already overflows before editing (932px doc scrollWidth); opening the edit row pushes it to 1189px — 257px of new horizontal overflow, tag labels clipped at the viewport edge, the whole Actions column off-screen. At 1440px the edit row makes the table 8px wider than its own container. There is no `overflow-x` container and no media query touching `table` anywhere. *Fix:* wrap the table in `overflow-x: auto`; below 46rem collapse to date/description/amount plus tag marks.

## Persona Red Flags

**Alex (power user):** No bulk tagging; no keyboard route from row to tag; single-tag filter, so "Vacation AND Tax Deductible" is impossible. Does this once, returns to a spreadsheet.

**Sam (accessibility):** The visually-hidden legend is right, but at 27 tags a screen reader announces "Tags, group, 27 checkboxes" with no search and no jump. 33 tab stops before Save. Focus rings verified correct (2px oxblood, offset 2, on `--paper-raised`).

**Casey (mobile):** Measured 257px of horizontal overflow at 390px with the edit row open; Actions column off-screen. Checkbox targets are 1.05rem (~17px), far under the 44pt floor.

**The Investigator (the household's own ad hoc scene):** Wants a vacation total. Gets a filtered list with no sum, after a 30-row manual tagging chore. Nothing on the page closes the question.

**The Joint Reviewer (both at one screen):** An open edit row is 863px of checkboxes; applied tags are indistinguishable from unapplied ones at conversational distance.

## Minor Observations

- Tags sort case-sensitively by name, so lowercase "kids" lands after every capitalized tag, and "Vacation 2026" sorts last — the most-used tag in the scene is furthest to reach.
- `TagSelect` hardcodes "All tags" instead of taking `includeAllOption` like its three siblings.
- "No tags yet." renders a bare `small` in a table cell; it should link to `/tags`.
- "Failed to add the tag." swallows the duplicate-name case, the likeliest failure.
- The `/tags` help copy uses lowercase examples ("vacation", "kids") while domain examples are title case — with no case folding, that copy actively seeds duplicates.

## Questions to Consider

1. If the household only tags during an investigation, why does the tag control live in the edit row at all, rather than as an action on a filtered selection?
2. The rule is the gauge on the status screen. What is the tag's equivalent structural device here — or is a tag just a word floating on ruled paper?
3. If deleting a tag detaches it from 23 transactions because "a tag is a label, not a classification", why does applying one require opening the row's full classification form?
4. Should `/tags` exist at all, or is a tag management page an admission that tags can't be created where they're needed?
5. What is the answer to "how much did the vacation cost?" supposed to look like — and which pixel on this page is it?
