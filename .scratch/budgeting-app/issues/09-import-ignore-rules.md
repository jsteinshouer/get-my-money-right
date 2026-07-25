# 09 — Import Ignore Rules

**What to build:** A household member can define text-match rules (e.g., "contains AUTOPAY") so matching CSV rows are visibly excluded during the import preview, before anything is saved. This is also how inter-account transfers get filtered out — no special Transfer modeling.

**Blocked by:** 08 — CSV Column Mapping

**Status:** ready-for-agent

- [ ] `ImportIgnoreRule` entity (AccountId nullable for global rules, MatchText, MatchType, IsActive) persisted via EF Core/SQLite
- [ ] Create, fetch-all, delete operations exist, each with a `WebApplicationFactory` integration test
- [ ] The import preview (from ticket 08) applies active ignore rules and visibly marks matching rows as "will be skipped," verified by a test with a sample CSV containing a matching row
- [ ] React: ignore-rule management UI, preview step shows skipped rows struck out
- [ ] Playwright e2e test: create an ignore rule, upload a CSV containing a matching row, confirm the preview shows it struck out
