# 10 — CSV Import Confirm & Dedupe

**What to build:** Confirming an import applies the saved column mapping and ignore rules, normalizes debit/credit or signed-amount conventions to one consistent sign, skips rows that duplicate an already-imported transaction (same account/date/amount/description), inserts the rest as uncategorized transactions, and reports exact counts back to the user.

**Blocked by:** 04 (Manual Transaction Entry), 08 (CSV Column Mapping), 09 (Import Ignore Rules)

**Status:** ready-for-agent

- [ ] Confirm operation applies mapping → ignore rules → duplicate check `(AccountId, Date, Amount, Description)` (description whitespace-normalized before comparing) → sign normalization → bulk insert, and returns `{Imported, SkippedIgnored, SkippedDuplicate}`
- [ ] Imported transactions default to no category assigned (reviewable "uncategorized" state)
- [ ] `WebApplicationFactory` integration tests using realistic CSV fixtures: single signed-amount column, separate debit/credit columns, a fixture with an ignore-rule match, a fixture with an intentional duplicate — each asserting exact imported/ignored/duplicate counts
- [ ] React import wizard: Preview → Confirm step, result summary banner showing the three counts
- [ ] Playwright e2e test: full upload → map → preview → confirm flow with a real sample CSV, asserting the result banner's counts and that transactions land correctly
