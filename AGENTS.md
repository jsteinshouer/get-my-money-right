## Agent skills

### Issue tracker

Issues are tracked as **GitHub Issues** on `jsteinshouer/get-my-money-right`, managed with the `gh` CLI (`gh issue list`, `gh issue view <n>`, `gh issue create`). Specs live in the repo under `docs/specs/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical GitHub labels: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix` — one at a time per issue. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.

### Working a ticket

The full feature lifecycle — `/grill-me` → `/to-spec` → `/impeccable shape` → `/to-tickets` →
`/implement` → review — is documented in `docs/agents/workflow.md`. Read it before starting new
feature work. Per ticket, the short version:

- **Backend-only ticket:** `/mattpocock-skills:implement` on its own. It builds test-first at the seams the spec named, runs `/code-review`, and commits.
- **UI ticket:** the ticket carries a `**Design brief:**` line pointing at its surface brief under `.impeccable/surfaces/`, produced by `/impeccable shape` *before* implementation starts. Build against the brief, then `/impeccable critique <page>` for the design review alongside `/code-review`.

`implement` is the hands, not the head — it executes a settled plan rather than deciding what to
build. For UI work the surface brief is that plan, the way the spec is for API work.

- One fresh session per ticket; tickets are sized for exactly that.
- Don't invoke impeccable during the build. The hook in `.claude/settings.local.json` already runs design checks after every Edit/Write and a deep pass on Stop.

### Reporting completed work

When reporting a completed ticket back to Jason:

- Use `/i-have-adhd:i-have-adhd` output shaping for the report: lead with what now works, restate ticket progress (e.g., "Ticket 04 of 12 done"), keep it concrete and scannable. Link the GitHub issue.
- Always tell him exactly how to validate the work is correct himself — concrete steps (commands to run, URL to open, what to click, what result to expect) — not just "tests pass."
- Close the GitHub issue via `Closes #<n>` in the PR description rather than by hand, and leave a closing comment on it (what shipped, any deviations, the PR link).
- Confirm the ticket's acceptance criteria are covered by real tests before marking it done, not just implemented. A ticket isn't done if its behavior isn't proven by a test at the seam defined in the spec (`WebApplicationFactory` integration tests for the API, Playwright e2e for client flows) — thin or missing test coverage is a reason to keep the ticket open, not a detail to skip.
