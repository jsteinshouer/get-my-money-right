# 11 — Category Spend Trend Report

**What to build:** A household member can view a chart of a chosen category's spending over the trailing 12 months and switch between categories.

**Blocked by:** 04 — Manual Transaction Entry

**Status:** ready-for-agent

- [ ] Category-trend operation groups transactions by category and month for the trailing 12 months
- [ ] `WebApplicationFactory` integration test: seed transactions across several months/categories, assert the trend data groups and sums correctly
- [ ] React Reports page: chart + backing table for a selected category's trailing-12-month trend, with a category switcher
- [ ] Playwright e2e test: view the trend report for a category with seeded transactions and confirm the chart/table reflects them
