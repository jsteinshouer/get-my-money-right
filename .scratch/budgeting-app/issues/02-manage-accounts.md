# 02 — Manage Accounts

**What to build:** A household member can create, view, edit, and deactivate bank/credit card accounts (checking, savings, credit card) that transactions will later be organized under.

**Blocked by:** 01 — can start once login exists

**Status:** ready-for-agent

- [ ] Account entity (Name, Type: Checking/Savings/CreditCard, IsActive, CreatedByUserId) persisted via EF Core/SQLite
- [ ] Create, update, deactivate, fetch-all, fetch-one operations exist, each with a `WebApplicationFactory` integration test
- [ ] Deactivating an account hides it from the active list without deleting it or its (future) transaction history
- [ ] React Accounts page: list accounts, add a new one, edit an existing one, deactivate one
- [ ] Playwright e2e test: add an account through the UI, confirm it persists after a page reload
