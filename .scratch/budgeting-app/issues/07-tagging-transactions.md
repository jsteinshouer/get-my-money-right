# 07 — Tagging Transactions

**What to build:** A household member can create free-form tags and apply multiple of them to any transaction, then filter the transaction list by tag.

**Blocked by:** 04 — Manual Transaction Entry

**Status:** done

- [x] Tag entity (Name, unique) + `TransactionTag` many-to-many join persisted via EF Core/SQLite
- [x] Create, delete, fetch-all, assign-to-transaction, remove-from-transaction operations exist, each with a `WebApplicationFactory` integration test
- [x] Transaction search/filter (from ticket 04) supports filtering by tag
- [x] React: tag multi-select on the transaction edit form, a simple tag management page, tag filter on the transaction list
- [x] Playwright e2e test: create a tag, apply it to a transaction, filter the transaction list by that tag and confirm it appears

## Comments

Landed in `1beb815` on `feature/07-tagging-transactions`.

**What was implemented**

- `Tag` (unique `Name`) and a `TransactionTag` join with a composite key, migration `20260831021530_AddTags`.
- `POST`/`DELETE`/`GET /api/tags`, plus `PUT`/`DELETE /api/transactions/{transactionId}/tags/{tagId}` for assignment. Each has its own `WebApplicationFactory` test class under `tests/Api.Tests/Features/Tags/`.
- `GET /api/transactions` gained a `tagId` filter, and every transaction in the response now carries its `tagIds`.
- Client: a `/tags` management page, a Tags column and tag filter on the transaction list, and a checkbox multi-select in the transaction edit row.
- `client/e2e/tags.spec.ts` walks the whole flow: create a tag, apply it via the edit form, filter by it, and confirm the untagged transaction drops out.

**Decisions worth knowing**

- Both join foreign keys **cascade**, unlike `Category`, which the ledger depends on and so restricts. Deleting a tag detaches it from its transactions instead of being refused with a 409 — a tag is a label, not a classification. `Delete_WithAssignedTransaction_SucceedsAndLeavesTheTransaction` pins this.
- Assignment is **idempotent**: a repeated `PUT` returns 204 rather than conflicting, since the caller is asking for a state, not an insert.
- Assignment endpoints hang off the transaction (`/api/transactions/{id}/tags/{tagId}`), not off the tag, so the edit form sends only what actually changed rather than a whole tag set.
- Filtering takes a single `tagId`, matching the shape of the existing `accountId`/`categoryId`/`needWant` filters, rather than introducing multi-tag AND/OR semantics the ticket didn't ask for.
- The `Add a transaction` form has no tag picker — the ticket specifies the *edit* form. Tags are applied by editing after adding.

**Found in review, fixed in `de-review` follow-up**

- `DemoDataSeeder.DeleteEverythingAsync` never deleted tags, and `Tag.CreatedByUserId` restricts against `AspNetUsers` — so `seed-demo-data` would have thrown once any tag existed. Tags are now deleted first, pinned by `Reset_DeletesTagsThatWereAlreadyThere`.
- `.tag-mark`/`.tag-option` invented new type sizes; DESIGN.md is explicit that "rank is made from weight, case, width and rule — never from more type sizes". They now use the documented 0.705rem tracked caps and plain body type respectively.
- The transaction edit form saved the transaction and its tag changes in one `try`, so a failed tag call left the row open diffing against a baseline the server had moved past. The two are now separated: a failed transaction save keeps the edits on screen, a failed tag save closes and reloads so what actually stuck is visible.

**Known gaps, deliberately not addressed here**

- Tag names are unique by SQLite's default collation with no trimming, so `Vacation` and `vacation` can coexist. `Category` has the same gap — worth one ticket covering both rather than diverging.
- Filtering takes one tag at a time. Multi-tag AND/OR is a separate ask.
- `POST /api/tags` returns a `Location` of `/api/tags/{id}`, which serves `DELETE` but has no `GET`. Same shape as `Categories.Create`, which is backed by a fetch-one this ticket didn't ask for.

## Comments — design round (2026-09-01)

`/impeccable critique` scored the tag-editing interaction **19/40 (Poor)**, with measured evidence: at 27 tags the checkbox picker made an edit row **863px** tall (7.3× a normal row) with 33 tab stops before Save, and opening it at 390px pushed **257px** of horizontal overflow onto the page with the Actions column off-screen. The deeper finding was that the control answered "which tags does this row have?" when the household's question is "how much did the vacation cost?"

**What changed**

- **Bulk tagging.** Row selection plus a selection bar; `POST /api/tags/{tagId}/transactions` assigns to a whole selection in one request and reports `assignedCount` / `alreadyTaggedCount`, so the batch says what it changed.
- **A closing figure.** The filtered list now closes on a total under a double rule — the tag-filtered list finally answers the question the tagging was for.
- **The tag line replaces the checkbox list.** Type-ahead over existing tags, create-on-Enter, applied tags as removable marks, and the 5 most-used offered as one-click marks. Cost is O(1) in tag count: the same edit row measures **240px** at 27 tags.
- **Tags can be created where they are needed**, so an investigation no longer leaves the page (and loses its filters) to invent "Vacation 2026".
- **Case-insensitive, trimmed names for `Tag` *and* `Category`** (`NOCASE` collation + trim on write, migration `NormaliseTagAndCategoryNames`, which normalises existing rows first). Prerequisite for typing tags; the same gap in `Category` is closed in the same pass. It also fixes the sort, which used to put every capitalised name ahead of every lowercase one.
- **Counted, confirmable tag delete.** `GET /api/tags` carries `transactionCount`; deleting asks "removed from N transactions?" and reports the count afterwards. Principle 2 — nothing disappears silently.
- **The table no longer dictates the page's scroll.** Wrapped in its own scroll box, with the Actions column sticky to the right so Save and Delete are reachable at any width, and Account/Need-Want/selection dropped below 46rem. Measured: **no page overflow at 390px or 1440px**, in both list and edit states.

**Two defects found by the work itself**

- Pressing Enter to create a tag and clicking Save before the round-trip finished **silently dropped the tag**. Save is now disabled while a tag is being created.
- The suggestion slip was clipped by the table's scroll box on the last row; it now opens upward when there is no room below.

**Still open**

- Multi-tag filtering (AND/OR) remains out; the filter takes one tag.
- Filters are still not written back to the URL, so a tag-filtered view is not linkable. Much less pressing now that tags are created in place.

## Comments — edit interaction (2026-09-03)

A second `/impeccable critique`, scoped to editing a transaction, scored **17/40 (Poor)** — lower than the 19/40 of the first round, because it targeted the part left structurally untouched. Measured: the inline edit row had **6 distinct control tops across a 72px spread** with three different control heights, and the Amount input was **61px wide for 78px of content**, rendering `-25` for `-25.50`.

**The P0 that mattered most:** the edit row was not inside a `<form>`, so `required` was inert; the save guard checked only account/category/Need-Want; `Number('')` is `0`; and the server has no Amount rule. **Clearing Amount silently wrote 0.00 to the ledger** — and the closing total added in the previous round would then report a confidently wrong figure.

**What replaced the inline edit row**

- **Category and Need/Want are now edited in the ledger itself**, committing on change with the struck-rule busy state. PRODUCT.md says categorisation is a batch worked as a queue where imported rows already carry date, amount, description and account — so the queue is two fields per entry, with no mode to enter or leave.
- **Full correction moved to a correction slip**: a `colSpan` ruled panel opening beneath the entry, which stays on its line marked with an oxblood left rule. It is a real `<form>`, so Enter commits, and it uses the same labelled grid and the same ledger field order as the Add form — which previously described the same entity in a second layout and a third field order.
- Validation runs in one place with the project's own voice, printed **beside the field**, not at the top of a 6000px page.
- **Escape** cancels the correction from anywhere on the slip; the tag combobox keeps Escape only while its suggestions are open.
- Opening a correction while another is **dirty** now asks before discarding. It used to vanish without a word.
- **Deleting an entry states what it is destroying** — description, amount and date — matching the tag delete added last round. The safeguards were previously inverted.
- **Rows are never tinted.** DESIGN.md refuses it by name; selection and correction are marked with a rule instead.

**Three defects the work surfaced**

- The tag suggestion list was **permanently open** — it opened whenever options existed, regardless of focus — and covered whatever sat beneath it. It now opens only when the writer types or arrows down.
- Re-focusing the tag input after a pick sprang the list open again, over the Save button.
- `.tag-combobox input:focus-visible { outline: none }` from the previous round had removed the focus ring from the tag input alone.

**The sticky closing column is gone.** Introduced two rounds ago, it caused three separate defects: it permanently hid ~38px of tag content on desktop and ~55px on mobile where it could not be scrolled to, it silently captured the correction slip's full-width cell, and its compensating gutter forced the table to scroll at all times. Instead the ledger now runs to the full viewport width above 78rem, since a ledger spread is data rather than prose — **no horizontal scrolling on desktop in either state**, and nothing hidden anywhere.

**Still open**

- Multi-tag filtering (AND/OR).
- Filters are still not written to the URL, so a filtered view is not linkable.
- Below 46rem the ledger drops Account, Need/Want and selection to control width; the correction slip carries every field on a phone, but in-row Need/Want reclassification is desk-only.
