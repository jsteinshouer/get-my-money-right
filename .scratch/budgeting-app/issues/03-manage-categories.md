# 03 — Manage Categories

**What to build:** A household member can create, rename, and delete their own flat list of spending categories, with a guard against deleting a category still in use.

**Blocked by:** 01 — can start once login exists

**Status:** ready-for-agent

- [ ] Category entity (Name, unique, CreatedByUserId) persisted via EF Core/SQLite
- [ ] Create, update, delete, fetch-all operations exist, each with a `WebApplicationFactory` integration test
- [ ] Deleting a category that still has transactions attached is rejected (Restrict FK behavior or equivalent guard), verified by a test
- [ ] React Categories page: list categories, add, rename, delete
- [ ] Playwright e2e test: add a category, confirm it appears in the list after reload
