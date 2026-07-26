# 02 — Manage Accounts

**What to build:** A household member can create, view, edit, and deactivate bank/credit card accounts (checking, savings, credit card) that transactions will later be organized under.

**Blocked by:** 01 — can start once login exists

**Status:** done

- [x] Account entity (Name, Type: Checking/Savings/CreditCard, IsActive, CreatedByUserId) persisted via EF Core/SQLite
- [x] Create, update, deactivate, fetch-all, fetch-one operations exist, each with a `WebApplicationFactory` integration test
- [x] Deactivating an account hides it from the active list without deleting it or its (future) transaction history
- [x] React Accounts page: list accounts, add a new one, edit an existing one, deactivate one
- [x] Playwright e2e test: add an account through the UI, confirm it persists after a page reload

## Comments

**What was built:**
- `Api.Features.Accounts` (REPR, mirrors the Identity area from ticket 01): `Account` entity + `AccountType` enum (`Checking`/`Savings`/`CreditCard`) in `Accounts.cs`; `Create`/`Update`/`Deactivate`/`FetchAll`/`FetchOne` operations, each its own file with Request/Handler/Response/Mapper (+ `Validator` on writes). All routes require authorization at the group level. `FetchOne`/`Update`/`Deactivate` use the Operation Result pattern (nullable/bool handler returns → 404), not exceptions, consistent with ticket 01's Login/Logout and the skill's stated baseline ("not found" is an expected failure, not exceptional) — this is a deliberate deviation from the REPR reference doc's own worked FetchOne example, which throws a `{Area}NotFoundException`; the reference's general Operation Result guidance and ticket 01's already-reviewed precedent took priority.
- `BudgetDbContext` now exposes `DbSet<Accounts.Account> Accounts`, with a `Restrict`-on-delete FK from `Account.CreatedByUserId` to `AspNetUsers`. New `AddAccounts` migration.
- Registered a global `JsonStringEnumConverter` (`Program.cs`) so `AccountType` serializes as `"Checking"` etc., not a numeric index — the gotcha called out in `spec.md`'s Further Notes, and the first ticket to actually need it.
- `FetchAll` takes an `includeInactive` query flag (default `false`); the Accounts management page passes `true` (it needs to show deactivated accounts so they're not just invisible), while the default (active-only) is what a future transaction-entry account picker will use.
- No reactivate/delete operation — the ticket only asks for deactivate, and the spec explicitly says deactivation should hide without deleting; adding a delete or reactivate path wasn't asked for.
- 29 backend tests total across the two tickets (17 new for Accounts: 10 gray-box `WebApplicationFactory` tests covering create/update/deactivate/fetch-one/fetch-all — including the "deactivated accounts excluded from the default list but visible with `includeInactive=true`" behavior, 401 on an unauthenticated request, 404 on unknown ids, 400 on an empty name — plus 7 standalone FluentValidation `Validator` unit tests for `Create`/`Update`, matching the project's documented testing decision that validators get plain no-HTTP unit tests).
- Client: `AppLayout` (new) factors the persistent nav out of `ShellPage` so it's shared across authenticated routes; added a nested `/accounts` route. `AccountsPage` lists accounts in a table, has an "Add an account" form, inline per-row Edit (Save/Cancel) turning a row into editable Name/Type fields, and a Deactivate button (hidden once an account is already inactive, since there's no reactivate). A shared `AccountTypeSelect` component avoids duplicating the type `<select>` between the add form and the inline edit row.
- `client/e2e/accounts.spec.ts`: logs in, adds an account with a timestamp-suffixed name (avoids collisions with data left over from prior runs against the persistent `e2e.db`), asserts it appears with the right type/status, reloads the page, and asserts it's still there.

**How to verify:**
1. `dotnet test tests/Api.Tests` — 29/29 pass.
2. `cd src/Api && dotnet run`, `cd client && npm run dev`, log in, click "Accounts" in the nav — add an account, edit its name/type inline, deactivate it and watch its status change to "Inactive" and its Deactivate button disappear. Note the Accounts *management* page intentionally still lists inactive accounts (it fetches with `includeInactive=true`, since hiding them here would mean no way to ever see them again) — the "excluded from the default list" behavior is at the API layer (`GET /api/accounts` defaults to active-only), for a future transaction-entry account picker to rely on.
3. `cd client && npm run test:e2e` — 5/5 pass (includes the new accounts spec).

Landed on `main` in the commit(s) following this one.
