# 07 — Tagging Transactions

**What to build:** A household member can create free-form tags and apply multiple of them to any transaction, then filter the transaction list by tag.

**Blocked by:** 04 — Manual Transaction Entry

**Status:** ready-for-agent

- [ ] Tag entity (Name, unique) + `TransactionTag` many-to-many join persisted via EF Core/SQLite
- [ ] Create, delete, fetch-all, assign-to-transaction, remove-from-transaction operations exist, each with a `WebApplicationFactory` integration test
- [ ] Transaction search/filter (from ticket 04) supports filtering by tag
- [ ] React: tag multi-select on the transaction edit form, a simple tag management page, tag filter on the transaction list
- [ ] Playwright e2e test: create a tag, apply it to a transaction, filter the transaction list by that tag and confirm it appears
