# Feature workflow

Two chains run side by side. The **engineering chain** (mattpocock skills) decides what to build
and builds it; the **design chain** (impeccable) decides what it looks like and how it behaves.
They meet at two artifacts — the **spec** (`docs/specs/<slug>.md`) and the **surface brief**
(`.impeccable/surfaces/<slug>.md`) — and nowhere else. Everything downstream reads those two files
instead of re-deriving the decisions in them.

```txt
  /grill-me ──────────────┐
                          ├── SAME session ──► /to-spec ──► docs/specs/<slug>.md
  codebase exploration ───┘                        │
                                                   ▼
                       /impeccable shape ──► .impeccable/surfaces/<slug>.md    (UI features only)
                                                   │
                                                   ▼
                          /mattpocock-skills:to-tickets ──► GitHub Issues, ready-for-agent
                                                   │
                          ┌────────────────────────┴─────────── one fresh session per ticket
                          ▼
              /mattpocock-skills:implement ──► /code-review ──┬─► /impeccable critique   (UI)
                                                              └─► PR, "Closes #n"
```

## Step 0 — one-time setup

Already done for this repo; listed so it's clear what the chain assumes exists.

- `PRODUCT.md` — durable product truth, written by `/impeccable init`. Every impeccable command loads it.
- `DESIGN.md` — the visual system (The Ruled Cash Book, established 2026-08-27). Written once, replaced only by a redesign.
- Tracker + triage vocabulary — see `issue-tracker.md` and `triage-labels.md`.

Not yet present: `CONTEXT.md` and `docs/adr/`, which `domain.md` describes. `/grill-with-docs`
creates them as a side effect of grilling (see step 1).

## Step 1 — Sharpen the idea (`/grill-me`)

A relentless interview: one question at a time, each with a recommended answer. Facts get looked up
from the codebase; **decisions are put to you and wait for your answer**. It doesn't act until you
confirm you've reached shared understanding.

- Use `/grill-with-docs` instead when the feature introduces new domain vocabulary or an
  architectural decision worth recording — it's the same grilling plus `/domain-modeling`, and it
  writes the glossary and ADRs as it goes.
- **Keep it product and technical.** Don't resolve visual or UX questions here. Those belong to
  `shape`, which will ask them again with better framing — answering them twice is how a design
  decision ends up recorded in two places that disagree.

## Step 2 — Write the PRD (`/to-spec`)

**Must run in the same session as the grilling.** `/to-spec` deliberately does not interview — it
synthesizes what's already in the conversation. Run it in a fresh window and you get a thin spec
built from nothing.

Two things to watch:

- **It proposes the test seams and asks you to confirm them.** This is the highest-leverage moment
  in the whole chain: those seams are what `implement` writes tests against, and what a ticket is
  judged "done" at. Repo standard is `WebApplicationFactory` integration tests for the API and
  Playwright e2e for client flows. Fewest seams, highest possible — one is ideal.
- Output goes to `docs/specs/<feature-slug>.md` and is committed (see `issue-tracker.md`, "When a
  skill says publish to the issue tracker").

## Step 3 — Design the surfaces (`/impeccable shape`) — UI features only

Fresh session; pass the spec path. Produces a confirmed brief at
`.impeccable/surfaces/<slug>.md` and **no code** — shape never writes code.

**Why it sits here, between the spec and the tickets:** `to-tickets` cuts vertical slices through
every layer, UI included. Slicing a UI nobody has designed yet produces tickets whose UI half is
guesswork. The brief names the screens, the interaction topology, and the real states — first-run,
empty, loading, error, permissions, overflow — and those become acceptance criteria in step 4.

- Tell it the product questions are settled and point it at the spec, so its discovery interview
  covers surface, audience, and states rather than re-litigating scope.
- **One brief per surface or flow, not per ticket.** Tickets #8–#10 are one CSV import flow and
  share one brief.
- Inside the established visual world this is refinement. A genuinely new surface routes through
  impeccable's new-work process for its concept; a *replacement* world would also replace `DESIGN.md`.
- Skip this step entirely for backend-only features (Docker packaging, migrations, CI).

## Step 4 — Break it down (`/mattpocock-skills:to-tickets`)

Fresh session; pass the spec path and the brief path.

- Produces **tracer bullets**: narrow but complete vertical slices, each demoable on its own and
  each sized to fit one fresh context window. Prefactoring goes first; wide mechanical refactors are
  the exception and get sequenced expand → migrate batches → contract.
- It presents the breakdown and quizzes you on granularity and blocking edges. Iterate there — it's
  far cheaper than re-cutting published issues.
- Each UI ticket must carry the `**Design brief:**` line (see `issue-tracker.md`). Because the brief
  already exists by this point, it goes in at creation; the `gh issue comment` route is only for
  retrofitting older issues.
- Check that the brief's state list survived into the acceptance criteria. An import ticket with no
  empty state and no malformed-CSV state is under-specified regardless of how good the spec is.

## Step 5 — Build (`/mattpocock-skills:implement`)

**One fresh session per ticket** — tickets are sized for exactly that. Pass the issue number.

`implement` is the hands, not the head: it executes the settled plan rather than reopening it. It
drives `/tdd` at the **pre-agreed seams only** (it must not invent new ones), typechecks often, runs
single test files as it goes, and runs the full suite once at the end.

- **Don't invoke impeccable during the build.** The hook in `.claude/settings.local.json` already
  runs design checks after every Edit/Write and a deep pass on Stop.
- Commands: `dotnet test` for the API; in `client/`, `npm run build`, `npm run lint`,
  `npm run test:e2e`.

## Step 6 — Review

- `/code-review` — `implement` runs this itself. Reviews on two axes: does it follow the repo's
  standards, and does it match what the ticket asked for.
- UI tickets add the design review: `/impeccable critique <target>` for UX and heuristic scoring,
  `/impeccable audit <target>` for accessibility, performance, and responsive behavior. Critiques
  land in `.impeccable/critique/`.
- Situational: `harden` (errors, i18n, edge cases), `onboard` (first-run and empty states),
  `polish` (final pass before shipping).

## Step 7 — Close out

Per `issue-tracker.md` and the reporting rules in `AGENTS.md`: tick the acceptance criteria, open a
PR whose description says `Closes #<n>`, leave a closing comment recording what shipped and any
deviations, and report back with concrete steps to validate the work by hand.

## Session boundaries at a glance

| Step | Session | Why |
|---|---|---|
| `/grill-me` → `/to-spec` | **One shared session** | `to-spec` synthesizes conversation context and never interviews |
| `/impeccable shape` | Fresh, per surface | Its own interview; pass the spec path |
| `/to-tickets` | Fresh | Reads the spec and brief from disk |
| `/implement` | Fresh, **per ticket** | Tickets are sized for one clean context window |
| `/impeccable critique` | Same session as the build, or fresh | Reads the shipped code either way |

## When to skip steps

- **Bug fix or small change:** skip steps 1–4. Go straight to `/mattpocock-skills:diagnosing-bugs`
  or `/mattpocock-skills:tdd`. The hook and `/code-review` still apply.
- **Pure visual change to shipped UI:** skip the engineering chain entirely — `/impeccable <command>
  <target>` directly on the file.
- **Backend-only feature:** skip step 3 and the design half of step 6.
- **Unsure whether a state model or a UI idea actually works:** drop `/mattpocock-skills:prototype`
  in between steps 1 and 2. A prototype snippet that pins a decision (state machine, schema, type
  shape) can be inlined into the spec.

## Anti-patterns

- Running `/to-spec` in a fresh session — produces a spec with nothing behind it.
- Running `shape`'s interview inside an `implement` session — mixes two contexts and burns the build's.
- Shaping per ticket instead of per surface — fragments a flow the user experiences as continuous.
- Settling visual questions during `/grill-me` — they get asked again, and the answers diverge.
- Hand-invoking impeccable mid-build — the hook already covers it.
- Letting `implement` pick its own seams — they were agreed in step 2 for a reason.
