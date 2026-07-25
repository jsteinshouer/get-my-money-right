Status: ready-for-agent

# Personal Budgeting App

## Problem Statement

The user's need a way to see where their household money goes each month without manually re-entering every bank and credit card transaction. Spreadsheets and generic budgeting apps either require tedious manual entry, don't distinguish essential spending (Needs) from discretionary spending (Wants), or don't let them freely tag transactions for their own ad hoc questions (e.g., "how much did the vacation actually cost?"). They also don't want their financial data living on a third-party server — it needs to run on their own home network, under their own control, while still letting each of them log in separately so it's clear who entered what.

## Solution

A self-hosted household budgeting web app for exactly two users. The core workflow is: export a CSV from the bank/credit card website, import it (the app remembers how to parse each account's export format and automatically skips known-noise rows and duplicates), categorize and tag the resulting transactions, and see spending vs. a monthly budget per category — plus month-over-month trend charts per category. Every transaction is marked Need or Want so the household can review discretionary spending specifically. The whole thing runs as a single Docker container on their home network with a SQLite database, requiring no ongoing hosting cost or internet exposure.

## User Stories

1. As a household member, I want to log in with my own account, so that transactions and budgets I enter are attributed to me.
2. As a household member, I want my spouse's login to be separate from mine, so we can each see who added or edited a given transaction.
3. As a household member, I want all financial data to be shared between our two accounts (not siloed), so we see one unified household financial picture.
4. As a household member, I want to create a bank or credit card account record (checking, savings, or credit card), so I can organize transactions by where they came from.
5. As a household member, I want to edit or deactivate an account, so stale or closed accounts stop cluttering the active list without deleting their transaction history.
6. As a household member, I want to create my own spending categories (e.g., Groceries, Dining Out, Utilities), so the categories match how we actually think about our spending.
7. As a household member, I want to rename or delete a category I'm not using, so my category list stays relevant.
8. As a household member, I want to be prevented from deleting a category that still has transactions attached to it, so I don't accidentally orphan historical data.
9. As a household member, I want to set a dollar budget limit for a category for a given month, so I have a target to compare actual spending against.
10. As a household member, I want to see actual spending vs. my budgeted limit for each category in the current month, so I know whether we're on track.
11. As a household member, I want to be warned if I try to set two budgets for the same category and month, so I don't create conflicting limits.
12. As a household member, I want to manually add a transaction (date, amount, description, account, category), so I can record cash purchases or anything not covered by a CSV import.
13. As a household member, I want to edit or delete a transaction, so I can correct mistakes.
14. As a household member, I want to search and filter transactions by account, category, date range, and Need/Want, so I can find specific spending quickly.
15. As a household member, I want every transaction to require a Need or Want classification, so discretionary spending is always identifiable, never silently uncategorized.
16. As a household member, I want to change a transaction's Need/Want classification after the fact, so I can correct my own judgment calls (e.g., a "want" that turned out to be a "need").
17. As a household member, I want to create free-form tags (e.g., "Vacation 2026", "Tax Deductible"), so I can track spending that cuts across normal categories.
18. As a household member, I want to apply multiple tags to a single transaction, so one purchase can be tracked from several angles at once.
19. As a household member, I want to remove a tag from a transaction, so I can correct tagging mistakes.
20. As a household member, I want to filter the transaction list by tag, so I can answer ad hoc questions like "how much did we spend on the vacation."
21. As a household member, I want to delete a tag I no longer need, so my tag list stays manageable.
22. As a household member, I want to upload a CSV file exported from my bank or credit card's website, so I don't have to manually type in every transaction.
23. As a household member, I want to see a preview of the CSV's columns before importing, so I can tell the app which column is the date, description, and amount.
24. As a household member, I want to map either a single signed Amount column or separate Debit/Credit columns, so the import works regardless of how my particular bank formats its export.
25. As a household member, I want my column mapping to be remembered per account, so I don't have to redo the mapping every time I import from the same account.
26. As a household member, I want to define text-match rules that cause matching transaction rows to be skipped on import (e.g., "contains AUTOPAY"), so recurring noise like payment confirmations and inter-account transfers never clutter my transaction list.
27. As a household member, I want the app to automatically detect and skip transactions that were already imported (same account, date, amount, and description), so re-exporting an overlapping date range from my bank doesn't create duplicate entries.
28. As a household member, I want to see a summary count after import (how many were imported, how many skipped as duplicates, how many skipped by an ignore rule), so I can trust the import did what I expected.
29. As a household member, I want imported transactions to default to an "uncategorized" state I can review, so I know which ones still need a category assigned.
30. As a household member, I want to view a chart of a category's spending over the trailing 12 months, so I can spot trends (e.g., "are we spending more on Dining Out than we used to?").
31. As a household member, I want to switch which category the trend report is showing, so I can investigate different categories one at a time.
32. As a household member, I want the whole app to run in a single Docker container on our home server, so setup and maintenance are simple.
33. As a household member, I want our data to persist across container restarts, so an update or reboot never loses our financial history.
34. As a household member, I want the app reachable from any device on our home network (phone, laptop), so either of us can check the budget or add a transaction from wherever we are at home.

## Implementation Decisions

- **Backend architecture**: ASP.NET Core using the REPR pattern (Request-Endpoint-Response) via Minimal APIs — one file per operation, organized by feature area (`Accounts`, `Categories`, `Budgets`, `Transactions`, `Tags`, `Import`, `Reports`, `Identity`). FluentValidation for request validation, Riok.Mapperly for DTO↔entity mapping (entities never cross the wire directly), centralized exception→`ProblemDetails` translation. Chosen over Modular Monolith/Microservices because the domain is one cohesive, tightly-relational set of entities (transactions constantly join across accounts/categories/tags/budgets) rather than independently-deployable business capabilities — module boundaries and an event bus would be pure ceremony for a 2-user app with no scaling requirement.
- **Persistence**: single shared `BudgetDbContext : IdentityDbContext<ApplicationUser>` (SQLite via EF Core) — deliberately one context, not per-area, given how relational the domain is.
- **Auth**: ASP.NET Core Identity, cookie-based, two known household user accounts. Cookie policy must allow plain HTTP (LAN-only deployment, no TLS) — `CookieSecurePolicy.None`/`SameAsRequest`, no HTTPS redirection/HSTS.
- **Core entities**: `Account` (Name, Type: Checking/Savings/CreditCard, IsActive), `Category` (Name, unique), `Budget` (CategoryId, Year, Month, Amount; unique on CategoryId+Year+Month), `Transaction` (AccountId, CategoryId, Date, signed decimal Amount, Description, required `NeedWant` enum, CreatedByUserId, many-to-many Tags), `Tag` (Name, unique), `CsvImportMapping` (one per Account: date/description/amount column names or debit/credit column pair, date format string, delimiter, header-row flag), `ImportIgnoreRule` (AccountId nullable for global rules, MatchText, MatchType, IsActive). Money fields are `decimal(18,2)`; transaction dates are `DateOnly`.
- **No first-class Transfer entity**: inter-account transfers are filtered out via the same ignore-rule mechanism as any other noise row (e.g., "contains TRANSFER TO SAVINGS") rather than being specially modeled.
- **No recurring-transaction modeling** in this scope — recurring bills are just regular transactions that repeat naturally through normal categorization.
- **Category model is flat** (no hierarchy/subcategories) and fully user-managed (no predefined starter taxonomy).
- **Budgeting methodology**: simple category spending limits per month (Budget = category + month + amount), not zero-based/envelope budgeting.
- **Need/Want is per-transaction**, not derived from category — a required enum field on every transaction, independent of the separate free-form Tag system.
- **CSV import pipeline order**: parse → apply saved column mapping (normalizing debit/credit or signed-amount conventions to one consistent sign) → apply ignore rules (skip matches) → duplicate check against `(AccountId, Date, Amount, Description)` (skip matches, trimming/normalizing description whitespace first) → insert remaining rows as Transactions with no category assigned → return `{Imported, SkippedIgnored, SkippedDuplicate}` counts.
- **Reporting scope for this spec**: category spend trend (month-over-month, trailing 12 months) only. Net worth, account balance history, exportable reports, and recurring-bill tracking are explicitly out of scope (see below).
- **Frontend**: React + TypeScript (Vite), styled with Pico.css (classless CSS framework — minimal custom styling, relies on semantic HTML).
- **Deployment**: single Docker image — multi-stage build compiles the React client and copies its output into the ASP.NET Core app's static file root; the API serves both the SPA and the JSON API from one process/port. SQLite file persisted via a mounted volume. No docker-compose, no reverse proxy, no HTTPS for v1 (home-network only).

## Testing Decisions

- A good test here exercises observable behavior through the API/UI, not internal implementation details (e.g., "importing this CSV results in these transactions and these counts," not "the mapper class was called with these arguments").
- **Backend seam**: one seam, the HTTP API, tested via `WebApplicationFactory<Program>` gray-box integration tests, one per operation, mirroring the `Features/{Area}` structure. Tests run against a real SQLite connection (not EF Core's InMemory provider), since unique constraints — Budget's `(CategoryId, Year, Month)` and the transaction duplicate-detection index — need to actually be enforced by the database for the tests to be meaningful.
- **Below that seam**: FluentValidation validators get plain unit tests (no HTTP, no database) since they're pure input-shape checks.
- **CSV import test fixtures**: maintain several realistic sample bank/credit-card CSV exports (single signed-amount column, separate debit/credit columns, one containing an ignore-rule match, one containing an intentional duplicate) and drive full upload→map→confirm integration tests asserting exact imported/ignored/duplicate counts.
- **Client**: Playwright end-to-end tests covering the critical user-facing flows — login, the full CSV import wizard (upload → map columns → preview → confirm → see result counts), setting a budget and seeing actual-vs-limit reflect a transaction, and tagging a transaction then filtering by that tag. These run against the real running app (API + client together), not a mocked backend, since the import wizard's statefulness (server-side cached preview between steps) is exactly the kind of thing that's only meaningfully verified end-to-end.
- No prior art in this repo yet — this spec establishes the testing pattern for everything that follows (greenfield project).

## Out of Scope

- Net worth tracking, account balance history/snapshots, exportable reports (PDF/CSV summaries), and recurring-transaction/bill tracking — all explicitly deferred past v1.
- Per-account or per-user access control/data siloing — all household data is shared; there is no concept of private accounts or permissions between the two users.
- Envelope/zero-based budgeting, category hierarchies, and predefined category taxonomies.
- A first-class Transfer concept — handled entirely via ignore rules.
- Multi-currency support.
- Internet exposure, HTTPS/TLS, reverse proxy hardening, and any auth hardening beyond a basic two-account cookie login — this is a home-network-only deployment for v1.
- Modular Monolith or Microservices architecture — explicitly rejected for this scope in favor of a single REPR-pattern API with one shared DbContext.
- Multi-container deployment (e.g., docker-compose with a separate frontend container) — explicitly a single container.

## Further Notes

- This spec follows an extensive grilling session (product/domain decisions) plus a consult of the `aspnet-core-architectures` skill (backend architecture selection: REPR over Vertical Slice+MediatR or Clean/Layered Architecture) and a Plan-agent design pass (concrete entity/folder structure, phased build order). See `/home/jsteinshouer/.claude/plans/i-would-like-to-compiled-starfish.md` for the full build-order phasing (Scaffolding+Auth → Accounts/Categories → Budgets → Transactions+Need/Want → Tags → CSV Import → Reporting → Docker packaging) and a list of implementation gotchas worth remembering during build:
  - SQLite is single-writer — enable WAL mode + busy-timeout; map `DbUpdateException`/`DbUpdateConcurrencyException` to HTTP 409.
  - Bank CSV date formats and delimiters vary — parse with an explicit per-mapping `DateFormat` and `InvariantCulture`, watch for a UTF-8 BOM on real exports.
  - Debit/credit sign normalization must be consistent or budget-vs-actual and trend math silently corrupts.
  - Register a global `JsonStringEnumConverter` so `NeedWant` and `Account.Type` serialize as readable strings, not numeric indices.
  - Server-side CSV preview state (between upload and confirm) is transient — an in-memory cache keyed by a short-lived token is sufficient; no need for persistent storage of it.
- This is a two-person household app; scale, concurrency, and multi-tenancy concerns were deliberately kept out of scope as non-problems for this deployment size.
