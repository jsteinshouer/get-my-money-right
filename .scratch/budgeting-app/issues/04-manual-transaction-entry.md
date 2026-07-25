# 04 — Manual Transaction Entry

**What to build:** A household member can manually record a transaction against an account and category, with a required Need/Want classification, and search/filter the resulting transaction list.

**Blocked by:** 02 (Manage Accounts), 03 (Manage Categories)

**Status:** ready-for-agent

- [ ] Transaction entity (AccountId, CategoryId, Date, signed decimal Amount, Description, required `NeedWant` enum, CreatedByUserId) persisted via EF Core/SQLite
- [ ] Create, update, delete operations exist, each with a `WebApplicationFactory` integration test
- [ ] A search/filter operation supports filtering by account, category, date range, and Need/Want, with a test covering at least one combination of filters
- [ ] Attempting to save a transaction without a Need/Want value is rejected by validation
- [ ] React Transactions page: table of transactions with filters, add/edit form including the required Need/Want field
- [ ] Playwright e2e test: manually add a transaction, confirm it appears in the filtered list
