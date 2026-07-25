## Agent skills

### Issue tracker

Issues and specs are tracked as local markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default canonical labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.

### Reporting completed work

When reporting a completed ticket back to Jason:

- Use `/i-have-adhd:i-have-adhd` output shaping for the report: lead with what now works, restate ticket progress (e.g., "Ticket 04 of 12 done"), keep it concrete and scannable.
- Always tell him exactly how to validate the work is correct himself — concrete steps (commands to run, URL to open, what to click, what result to expect) — not just "tests pass."
- Confirm the ticket's acceptance criteria are covered by real tests before marking it done, not just implemented. A ticket isn't done if its behavior isn't proven by a test at the seam defined in the spec (`WebApplicationFactory` integration tests for the API, Playwright e2e for client flows) — thin or missing test coverage is a reason to keep the ticket open, not a detail to skip.
