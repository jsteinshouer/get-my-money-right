# MoneyRight

A self-hosted household budgeting app for exactly two people. Export CSVs from the bank and credit
card sites, import them, categorize what comes back, and see actual-vs-budget per category for the
month — all on your own home network, with the data in a SQLite file you own.

Every transaction carries a required **Need/Want** judgment independent of its category, so
discretionary spending is always identifiable. Free-form tags sit alongside categories, so a single
purchase can be tracked from several angles ("Vacation 2026", "Tax Deductible") without distorting
the category taxonomy.

## Status

**Working:** cookie login for two household accounts, account records, user-managed categories,
manual transaction entry with Need/Want, per-category monthly budgets, tags and tag assignment, and
the budget-status home screen (actual vs. limit, pace against elapsed days, unbudgeted spend, the
uncategorized queue, the Need/Want split).

**Not built yet** — tracked as open issues under the `Budgeting App v1` milestone:

| # | Ticket |
|---|---|
| [#8](https://github.com/jsteinshouer/get-my-money-right/issues/8) | CSV column mapping |
| [#9](https://github.com/jsteinshouer/get-my-money-right/issues/9) | Import ignore rules |
| [#10](https://github.com/jsteinshouer/get-my-money-right/issues/10) | CSV import confirm & dedupe |
| [#11](https://github.com/jsteinshouer/get-my-money-right/issues/11) | Category spend trend report |
| [#12](https://github.com/jsteinshouer/get-my-money-right/issues/12) | Docker packaging |

Until #12 lands there is no container image — run it from source as below.

## Stack

- **Backend** — ASP.NET Core 10 Minimal APIs in the REPR pattern (one file per operation under
  `src/Api/Features/{Area}/`), EF Core over SQLite, ASP.NET Core Identity with cookies,
  FluentValidation for request shape, Riok.Mapperly for DTO mapping.
- **Client** — React 19 + TypeScript on Vite, React Router, self-hosted Archivo Variable, and the
  project's own ledger design system (`client/src/styles/ledger.css`).
- **Deployment target** — one process serving both the SPA and the JSON API, over plain HTTP on a
  LAN. No HTTPS redirect, no HSTS, no reverse proxy, no internet exposure.

## Layout

```txt
src/Api/              ASP.NET Core API — Features/, Data/, Migrations/
tests/Api.Tests/      WebApplicationFactory integration tests + validator unit tests
client/               React SPA (src/, e2e/ Playwright specs)
docs/specs/           The authoritative product spec
docs/agents/          Working conventions for agents on this repo
```

## Running it

Requires the .NET 10 SDK and Node 24.

```bash
# API — http://localhost:5059
cd src/Api
dotnet run
```

```bash
# Client — http://localhost:5173, proxying /api to the API above
cd client
npm install
npm run dev
```

Migrations are applied automatically at startup, and the two household users from
`HouseholdUsers` in `src/Api/appsettings.json` are seeded on first run. The defaults are
`user1` / `user2` with password `ChangeMe123!` — change them before putting this on a real network.

### Demo data

To fill a development database with a realistic household — accounts, categories, budgets, and a
history of transactions spending against them:

```bash
cd src/Api
dotnet run -- seed-demo-data
```

This **deletes everything in the database**, users included, then rebuilds it and exits without
serving. It refuses to run outside the Development environment.

## Tests

```bash
dotnet test tests/Api.Tests/Api.Tests.csproj   # API integration + validator tests

cd client
npm run lint         # oxlint
npx tsc -b           # typecheck
npm run test:e2e     # Playwright, against a real API + Vite pair
```

The e2e suite starts its own API (on `e2e.db`) and dev server, so nothing needs to be running first.
It needs the Chromium binary once — `npx playwright install chromium`; see
[`client/README.md`](client/README.md) for the no-root fallback when system libraries are missing.

All three run in CI on every push and pull request to `main` (`.github/workflows/ci.yml`).

## Database

SQLite, at `src/Api/budget.db` by default (`ConnectionStrings:BudgetDb`). To add a migration:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Api
```

Startup runs `Database.MigrateAsync()`, so there is no separate update step.

Note that SQLite is single-writer: write conflicts surface as HTTP 409, and the UI is expected to
handle that state.

## Documentation

- [`PRODUCT.md`](PRODUCT.md) — who this is for, what it's for, and what is deliberately out of scope.
- [`DESIGN.md`](DESIGN.md) — the visual system: The Ruled Cash Book, light-only, one signal colour.
- [`docs/specs/budgeting-app.md`](docs/specs/budgeting-app.md) — the authoritative spec: 34 user
  stories, the entity model, architecture and testing decisions.
- [`AGENTS.md`](AGENTS.md) and [`docs/agents/`](docs/agents/) — conventions for agents working here,
  including the full feature workflow in [`docs/agents/workflow.md`](docs/agents/workflow.md).

Issues live in [GitHub Issues](https://github.com/jsteinshouer/get-my-money-right/issues).
