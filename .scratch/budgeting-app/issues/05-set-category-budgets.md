# 05 — Set Category Budgets

**What to build:** A household member can set a monthly dollar spending limit per category.

**Blocked by:** 03 — Manage Categories

**Status:** ready-for-agent

- [ ] Budget entity (CategoryId, Year, Month, Amount) persisted via EF Core/SQLite with a unique index on (CategoryId, Year, Month)
- [ ] Create, update, delete, fetch-for-month operations exist, each with a `WebApplicationFactory` integration test
- [ ] Attempting to create a second budget for the same category+month is rejected, verified by a test
- [ ] React Budgets page: set a category's limit for a given month, list current month's budgets
- [ ] Playwright e2e test: set a budget for a category/month, confirm it appears after reload
