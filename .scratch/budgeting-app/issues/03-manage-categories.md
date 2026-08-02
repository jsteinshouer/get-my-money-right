# 03 — Manage Categories

**What to build:** A household member can create, rename, and delete their own flat list of spending categories, with a guard against deleting a category still in use.

**Blocked by:** 01 — can start once login exists

**Status:** done

- [x] Category entity (Name, unique, CreatedByUserId) persisted via EF Core/SQLite
- [x] Create, update, delete, fetch-all operations exist, each with a `WebApplicationFactory` integration test
- [x] Deleting a category that still has transactions attached is rejected (Restrict FK behavior or equivalent guard), verified by a test
- [x] React Categories page: list categories, add, rename, delete
- [x] Playwright e2e test: add a category, confirm it appears in the list after reload

## Comments

**What was built:**
- `Api.Features.Categories` (REPR, mirrors the Accounts area from ticket 02): `Category` entity (`Name`, `CreatedByUserId`) in `Categories.cs`; `Create`/`Update`/`Delete`/`FetchAll`/`FetchOne` operations, each its own file with Request/Handler/Response/Mapper (+ `Validator` on writes). Unlike Accounts' soft `Deactivate`, `Delete` here is a real hard delete — the ticket asks for delete, not deactivate, and a guard (not a hide) is what keeps in-use categories safe.
- `BudgetDbContext` now exposes `DbSet<Categories.Category> Categories` with a unique index on `Name`, plus a `Restrict`-on-delete FK from `Category.CreatedByUserId` to `AspNetUsers`. New `AddCategoriesAndTransactions` migration.
- A minimal `Api.Features.Transactions.Transaction` entity/table (`AccountId`, `CategoryId`, `Date`, `Amount`, `Description`, `NeedWant`, `CreatedByUserId`) was added in the same migration — no CRUD endpoints, just the table with a `Restrict` FK from `Transaction.CategoryId` to `Category`, so the "category still in use" delete guard has something real to enforce against and test. Ticket 04 owns building the actual Transaction CRUD on top of this table.
- Incidental bug fix: deleting an in-use category (or any other `DbUpdateException`-triggering conflict) was supposed to return 409, but never actually did — `ForEvolve.ExceptionMapper`'s default ProblemDetails serializer reflects every public property of the caught exception into the response body, and `DbUpdateException.Entries` holds EF change-tracker entries that reference the `DbContext` back, causing a JSON serialization cycle that crashed the response instead of returning 409. Root-caused by decompiling the library and ASP.NET Core's shared framework with `ilspycmd`. Fixed with a dedicated `DbUpdateExceptionHandler : IExceptionHandler` registered via `AddExceptionHandler<T>()` ahead of `AddExceptionMapper()` — ASP.NET Core checks native DI-registered `IExceptionHandler`s first, so this intercepts `DbUpdateException` and writes a clean 409 `ProblemDetails` directly, without touching ForEvolve's behavior for every other exception/status-code type. This had been silently broken since ticket 01/02; nothing had exercised it until this ticket's own required guard test.
- 19 new backend tests across `Categories.Create/Update/Delete/FetchAll/FetchOne.Tests.cs` (gray-box `WebApplicationFactory`) plus standalone `Create.Validator`/`Update.Validator` unit tests — including the key guard test (`Delete_WithAttachedTransaction_ReturnsConflictAndDoesNotDelete`): creates a category, attaches a transaction to it directly via the DB context, asserts `DELETE /api/categories/{id}` returns 409 and the category is still fetchable afterward.
- Client: `CategoriesPage` (list/add/inline rename/delete), added to `AppLayout` nav and routing; `handleDelete` maps a 409 response to "This category still has transactions attached and cannot be deleted." rather than a generic error.
- `client/e2e/categories.spec.ts`: logs in, adds a category with a timestamp-suffixed name, asserts it appears in the list, reloads, and asserts it's still there.

**How to verify:**
1. `dotnet test tests/Api.Tests` — 48/48 pass.
2. `cd src/Api && dotnet run`, `cd client && npm run dev`, log in, click "Categories" in the nav — add, rename inline, and delete a category. Create a transaction against a category (via the DB directly, since ticket 04 hasn't landed CRUD yet) and confirm deleting that category now fails with the "still has transactions attached" message instead of crashing or silently succeeding.
3. `cd client && npm run test:e2e` — 6/6 pass (includes the new categories spec).

Landed on `main` in the commit(s) following this one.
