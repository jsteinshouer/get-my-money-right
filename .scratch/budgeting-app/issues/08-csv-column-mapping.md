# 08 — CSV Column Mapping

**What to build:** A household member can upload a CSV export from their bank/credit card, preview its columns, and map them (Date/Description/Amount, or separate Debit/Credit columns) to the fields the app needs — with the mapping remembered per account for next time.

**Blocked by:** 02 — Manage Accounts

**Status:** ready-for-agent

- [ ] `CsvImportMapping` entity (one per Account: date/description/amount column names or debit/credit pair, date format string, delimiter, header-row flag) persisted via EF Core/SQLite
- [ ] Upload-and-preview operation parses the CSV header + sample rows and returns them for mapping, without yet inserting any transactions
- [ ] Save-mapping and get-mapping operations exist, each with a `WebApplicationFactory` integration test
- [ ] Re-uploading a CSV for an account with a saved mapping pre-fills the mapping UI from the saved values, verified by a test
- [ ] React import wizard: Upload step and Map Columns step (pre-filled when a mapping already exists for the account)
- [ ] Playwright e2e test: upload a sample CSV, map its columns, confirm the mapping is remembered on a second upload for the same account
