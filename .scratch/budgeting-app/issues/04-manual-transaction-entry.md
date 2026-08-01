# 04 — Manual Transaction Entry

**What to build:** A household member can manually record a transaction against an account and category, with a required Need/Want classification, and search/filter the resulting transaction list.

**Blocked by:** 02 (Manage Accounts), 03 (Manage Categories)

**Status:** done

- [x] Transaction entity (AccountId, CategoryId, Date, signed decimal Amount, Description, required `NeedWant` enum, CreatedByUserId) persisted via EF Core/SQLite
- [x] Create, update, delete operations exist, each with a `WebApplicationFactory` integration test
- [x] A search/filter operation supports filtering by account, category, date range, and Need/Want, with a test covering at least one combination of filters
- [x] Attempting to save a transaction without a Need/Want value is rejected by validation
- [x] React Transactions page: table of transactions with filters, add/edit form including the required Need/Want field
- [x] Playwright e2e test: manually add a transaction, confirm it appears in the filtered list

## Comments

Implemented the Transactions vertical slice (Create/Update/Delete/FetchAll) following the same pattern as the Accounts and Categories features. The Transaction entity, migration, and DbContext FK config already existed from ticket 03's work.

Key decision: `NeedWant` is `NeedWant?` (nullable) in the Create/Update `Command` DTOs, not the non-nullable enum used on the entity. A non-nullable enum defaults to `Need` (0) when the JSON property is omitted, which would silently accept a missing value — nullable + `NotNull().IsInEnum()` in the FluentValidation validator is what actually rejects a missing selection, and is what the "no Need/Want" acceptance criterion needed.

`AccountId`/`CategoryId` existence isn't checked explicitly in the validator; an unknown id relies on the existing SQLite FK constraint throwing `DbUpdateException`, mapped to 409 by the pre-existing `DbUpdateExceptionHandler`. Covered by `Create_WithUnknownAccount_ReturnsConflict`.

No `FetchOne` slice was added (Accounts/Categories both have one) — nothing in the UI or tests needs a single-transaction fetch; the edit form works off data already in the loaded/filtered list, and Update tests assert on the PUT response directly.

Landed on branch `feature/04-manual-transaction-entry`.
