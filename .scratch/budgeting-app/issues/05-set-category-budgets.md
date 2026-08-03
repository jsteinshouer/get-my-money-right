# 05 — Set Category Budgets

**What to build:** A household member can set a monthly dollar spending limit per category.

**Blocked by:** 03 — Manage Categories

**Status:** done

- [x] Budget entity (CategoryId, Year, Month, Amount) persisted via EF Core/SQLite with a unique index on (CategoryId, Year, Month)
- [x] Create, update, delete, fetch-for-month operations exist, each with a `WebApplicationFactory` integration test
- [x] Attempting to create a second budget for the same category+month is rejected, verified by a test
- [x] React Budgets page: set a category's limit for a given month, list current month's budgets
- [x] Playwright e2e test: set a budget for a category/month, confirm it appears after reload

## Comments

**What was built:**
- `Api.Features.Budgets` (REPR, mirrors Categories/Transactions): `Budget` entity (`CategoryId`, `Year`, `Month`, `Amount`) in `Budgets.cs`; `Create`/`Update`/`Delete`/`FetchForMonth` operations, each its own file with Command/Response/Validator/Mapper/Handler.
- `BudgetDbContext` exposes `DbSet<Budgets.Budget> Budgets` with a unique index on `(CategoryId, Year, Month)` and a `Restrict`-on-delete FK from `Budget.CategoryId` to `Category`. New `AddBudgets` migration.
- Deviation from the ticket's sibling tickets (03/04): unlike `Category`/`Transaction`, `Budget` has no `CreatedByUserId`. The ticket's entity line — `(CategoryId, Year, Month, Amount)` — is the only one of the three that doesn't list it, and a household spending limit on a category reads as shared state rather than something owned by whoever entered it, so it was left off deliberately rather than by omission.
- The "second budget for the same category+month is rejected" behavior needs no application-level duplicate check — it relies entirely on the DB unique-index violation surfacing as `DbUpdateException`, already mapped to 409 by the pre-existing `DbUpdateExceptionHandler` from ticket 03. Same mechanism protects `Update` against retargeting a budget onto an already-used category+month.
- `Update`'s `Command` mirrors `Create`'s exactly (`CategoryId`, `Year`, `Month`, `Amount`, full replace) rather than an amount-only patch, matching the `Transactions.Update` convention of replacing the whole record.
- 22 new backend tests across `Budgets.Create/Update/Delete/FetchForMonth.Tests.cs` (gray-box `WebApplicationFactory`) plus `Create.Validator`/`Update.Validator` unit tests.
- Client: `BudgetsPage` — a month picker (`<input type="month">`, defaults to the current month), a table of that month's budgets (category name resolved from the loaded category list, inline Edit/Delete), and a "Set a category budget" form. The form upserts: if the selected category already has a budget for the selected month, submitting calls `Update` instead of `Create`, so setting a limit for an already-budgeted category doesn't require the user to know to hit "Edit" first. Added to `AppLayout` nav and routing.
- `client/e2e/budgets.spec.ts`: logs in, adds a category, sets a budget for it via the form, asserts it appears in the list, reloads, and asserts it's still there with the same amount.

**How to verify:**
1. `dotnet test tests/Api.Tests` — 90/90 pass.
2. `cd src/Api && dotnet run`, `cd client && npm run dev`, log in, click "Budgets" in the nav — pick a month, set a limit for a category, confirm it appears in the table, edit it inline, delete it. Switch months to confirm the list is scoped to the selected month.
3. `cd client && npm run test:e2e` — 8/8 pass (includes the new budgets spec).

Landed on branch `feature/05-set-category-budgets`.
