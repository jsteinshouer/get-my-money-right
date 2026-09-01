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
