# Issue tracker: GitHub Issues

Issues for this repo live in **GitHub Issues** on `jsteinshouer/get-my-money-right`, driven from the command line with the `gh` CLI.

Specs (you may know a spec as a PRD) live in the repo as markdown under `docs/specs/<feature-slug>.md`, because they're long-lived reference documents that implementation issues link back to.

## Conventions

- One issue per implementation ticket, titled `NN — Title` (e.g. `08 — CSV Column Mapping`), where `NN` is the ticket's position in its spec's build order
- Each issue body opens with a `**Ticket NN of M**` line linking to its spec under `docs/specs/`, then `**What to build:**`, then a `**Blocked by:**` line, then a `**Design brief:**` line on UI tickets, then an `## Acceptance criteria` checklist
- One GitHub **milestone** per spec (e.g. `Budgeting App v1`) groups that spec's issues
- Triage state is a **label** on the issue (see `triage-labels.md`)
- **Blocking** is expressed by referencing the blocking issues by number in the `**Blocked by:**` line (e.g. `**Blocked by:** #8, #9`). An issue is unblocked when every issue it lists is closed
- **Design briefs** are the UI equivalent of the spec: a ticket that touches the interface carries a `**Design brief:**` line pointing at its surface brief under `.impeccable/surfaces/<slug>.md` (e.g. `**Design brief:** .impeccable/surfaces/client-src-pages-shellpage-tsx.md`). Briefs are produced by `/impeccable shape` before implementation starts, so a UI ticket without one isn't ready to build. One brief covers a whole surface or flow — tickets that share a flow each link the same brief. Backend-only tickets omit the line. Add it to an existing issue with `gh issue comment <n>` rather than rewriting the body
- Comments and conversation history are GitHub issue comments

## Commands

```bash
# List open work, and just the AFK-ready subset
gh issue list
gh issue list --label ready-for-agent

# Fetch a ticket (body + comments)
gh issue view 8 --comments

# Publish a new ticket to the tracker
gh issue create --title "13 — Some Feature" --body-file <path> \
  --label ready-for-agent --milestone "Budgeting App v1"

# Triage: swap the state label
gh issue edit 8 --remove-label needs-triage --add-label ready-for-agent

# Comment
gh issue comment 8 --body "..."

# Everything in a milestone, open and closed.
# `gh issue list --milestone` returns nothing on gh 2.97 even when the milestone is set —
# it goes through GitHub's search index. Use the REST endpoint, which is authoritative:
gh api 'repos/jsteinshouer/get-my-money-right/issues?milestone=1&state=all&per_page=100' \
  --jq '.[] | "#\(.number)  \(.state)  \(.title)"'
```

## When a skill says "publish to the issue tracker"

Write the body to a temp file and run `gh issue create --title "NN — Title" --body-file <path>` with the appropriate triage label and the spec's milestone. For a *spec* rather than an issue, write the file under `docs/specs/` and commit it.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number>`. The user will normally pass the issue number or URL directly.

## Marking a ticket done

The five canonical triage labels only cover triage state *before* work starts — there is no canonical "done" label. Use this convention instead:

1. Check off each acceptance-criterion checkbox in the issue body as it's completed (`gh issue edit <n> --body-file <updated>`, or tick them in the web UI).
2. Once every checkbox is checked and its tests pass, close the issue — ideally via `Closes #<n>` in the PR description, so the issue closes when the work merges.
3. Leave a closing comment (`gh issue comment`) — what was implemented, any deviations from the ticket as written, and the commit/PR it landed in. A PR that says `Closes #<n>` gives you the link for free.

## Archive

Tickets 01–07 of the budgeting-app spec shipped before the tracker moved to GitHub Issues. They were backfilled as **closed** issues #13–#19 (ticket 01 → #13 … ticket 07 → #19 — note the issue numbers don't match the ticket numbers, since #1–#12 were already taken), each carrying its original completion notes as a comment plus a link to the PR or commit it landed in. Tickets 01 and 02 predate the PR workflow and link to commits; ticket 03's commit reached `main` inside PR #2, which is the CI pull request.

The source markdown files are also kept at `docs/archive/budgeting-app-issues/` as the provenance of those notes. Don't add new files there — new work goes straight to GitHub Issues.

## Wayfinding operations

Used by `/wayfinder`. These are **transient exploration state**, not shipped work, so they deliberately stay as local markdown under `.scratch/` rather than becoming GitHub issues. The **map** is a file with one **child** file per ticket.

- **Map**: `.scratch/<effort>/map.md` — the Notes / Decisions-so-far / Fog body.
- **Child ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, numbered from `01`, with the question in the body. A `Type:` line records the ticket type (`research`/`prototype`/`grilling`/`task`); a `Status:` line records `claimed`/`resolved`.
- **Blocking**: a `Blocked by: NN, NN` line near the top. A ticket is unblocked when every file it lists is `resolved`.
- **Frontier**: scan `.scratch/<effort>/issues/` for files that are open, unblocked, and unclaimed; first by number wins.
- **Claim**: set `Status: claimed` and save before any work.
- **Resolve**: append the answer under an `## Answer` heading, set `Status: resolved`, then append a context pointer (gist + link) to the map's Decisions-so-far in `map.md`.

If a wayfinding ticket turns out to be real implementation work, promote it to a GitHub issue using the conventions above.
