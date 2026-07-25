# 06 — Budget vs. Actual View

**What to build:** A household member can see actual spending per category for a given month compared against that category's budgeted limit — the first end-to-end demo of the core "are we on track" loop.

**Blocked by:** 04 (Manual Transaction Entry), 05 (Set Category Budgets)

**Status:** ready-for-agent

- [ ] `Budgets.FetchForMonth`-style operation computes actual spend per category from real transactions for the requested month and returns it alongside the budgeted amount
- [ ] `WebApplicationFactory` integration test: seed transactions + a budget for a category/month, assert the computed actual matches expected spend
- [ ] React Budgets page is extended to show actual-vs-limit per category for the selected month (e.g., a progress indicator or simple comparison column)
- [ ] Playwright e2e test: add transactions against a categorized budget, confirm the actual-vs-limit view reflects them
