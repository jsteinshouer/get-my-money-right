# 06 — Budget vs. Actual View

**What to build:** A household member can see actual spending per category for a given month compared against that category's budgeted limit — the first end-to-end demo of the core "are we on track" loop.

**Blocked by:** 04 (Manual Transaction Entry), 05 (Set Category Budgets)

**Status:** done

- [x] `Budgets.FetchForMonth`-style operation computes actual spend per category from real transactions for the requested month and returns it alongside the budgeted amount
- [x] `WebApplicationFactory` integration test: seed transactions + a budget for a category/month, assert the computed actual matches expected spend
- [x] React Budgets page is extended to show actual-vs-limit per category for the selected month (e.g., a progress indicator or simple comparison column)
- [x] Playwright e2e test: add transactions against a categorized budget, confirm the actual-vs-limit view reflects them

## Comments

**What was built:**

- `Budgets.FetchForMonth` now returns `Actual` alongside `Amount` (the budgeted limit) for each budget in the month. Actual spend is the *negated net* of that category's transactions dated inside the month: transaction amounts are signed with negative = money out (established by ticket 04), so a `-120.50` spend reports as `120.50` actual, and a refund reduces it. A category that took in more than it spent reports a negative actual rather than being floored at zero — the data stays honest, and only the progress bar clamps.
- Scope note: the response still lists exactly the categories that have a budget for the month. Spending in a category with *no* budget is not surfaced here; that's a different question ("where did unbudgeted money go") and isn't in this ticket.
- The per-category sums are computed in memory after one filtered query rather than with a SQL `GROUP BY`. SQLite has no native decimal type, so aggregating money columns in the database loses precision — the same reason the spec insists on real SQLite in tests.
- Mapperly's generated `Map` ignores the computed `Actual` target (`[MapperIgnoreTarget]`); the handler fills it in via a `with` expression, so the entity→DTO mapping stays a pure copy and the computed field is visibly computed.
- Client: `BudgetsPage` gained **Actual**, **Remaining**, and **Progress** columns. Remaining reads `124.75 left` under the limit and flips to `50.25 over` past it; Progress is a Pico-styled `<progress>` with an aria-label naming the category and the two figures.
- **Bug found and fixed along the way** (not in the ticket): switching the month picker fired a fetch while the previous month's was still in flight, and whichever resolved last won — the page could show the wrong month's budgets indefinitely. The e2e test caught it. `load()` now tags each request and ignores any response that isn't the most recently issued one.
- Tests: 3 new `WebApplicationFactory` integration tests on `FetchForMonth` (spend confined to the right month *and* category, refunds reducing actual, zero when nothing was spent) — backend suite 93/93. New `client/e2e/budget-vs-actual.spec.ts` drives the whole loop through the UI: create account + category, set a 200.00 limit, then add three transactions and assert actual/remaining after each, including the over-budget reading — e2e suite 9/9.
- `client/e2e/budgets.spec.ts` (ticket 05's spec) needed its two assertions narrowed from "row contains the text 325.00" to "this cell is exactly 325.00", because that figure now legitimately appears in more than one cell of the row.

**How to verify:**

1. `dotnet test` — 93/93 pass.
2. `cd client && npm run test:e2e` — 9/9 pass.
3. By hand: `cd src/Api && dotnet run`, `cd client && npm run dev`, log in, then Categories → add "Groceries"; Accounts → add a checking account; Budgets → set Groceries to 400.00 for the current month (row should read Actual `0.00`, Remaining `400.00 left`, empty bar); Transactions → add a `-150.00` Groceries transaction dated this month; back on Budgets the row should read Actual `150.00`, Remaining `250.00 left`, bar ~38% full. Add another `-300.00` and it should read Actual `450.00`, Remaining `50.00 over`, bar full. Switching the month picker away and back should keep showing the right month's numbers.

Landed on branch `feature/06-budget-vs-actual-view`.
