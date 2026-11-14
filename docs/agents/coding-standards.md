# Coding Standards

The rules `/code-review` checks this repo against on its Standards axis, alongside its generic
code-smell baseline. Where a rule here and the baseline disagree, this file wins.

## Keep files a manageable size

Files grow a reasonable amount per ticket, so no single ticket looks like the problem. The import
page reached 1,345 lines over three tickets that way, and was split after the fact (PR #37). These
rules exist so the split happens before the next addition instead.

### 1. A size budget, as a signal

A file past **~400 lines**, or a component holding state for **more than one concern**, is due a
split. This is a prompt to look, not a hard limit: a 420-line file with one clear job can stay.

Split along the seams the code already has, rather than by line count:

- **Steps or sections become components.** A page with stations, tabs or regions gets one component
  per station (`pages/import/UploadStep.tsx`, `MapStep.tsx`, `PreviewStation.tsx`). The page keeps
  only the state that crosses them.
- **Each stateful concern becomes one hook.** State with its own rules — loading, failure, retry,
  what is stale — lives in one hook that owns those rules together
  (`pages/import/usePreviewReading.ts`), not spread across a component's `useState` calls.
- **Pure logic moves out of components.** Anything that maps data to data goes in a plain `.ts`
  module beside its components (`pages/import/mapping.ts`), where it can be read and tested alone.
- **A component taking ~10 or more props** usually means its state lives in the wrong place: move
  the state down into it.

Feature folders sit under `pages/<feature>/`; components shared by several pages stay in
`components/`. On the API, one file per operation (REPR) already keeps files small — the budget
applies there too, and a growing handler extracts a static helper class in the same feature folder
(as `Import.RuleMatching.cs` and `Import.AssignmentResolution.cs` do).

**Exempt:** tests, EF migrations and their snapshots, fixtures, and other generated files.

### 2. Split before adding

If a ticket would push a file past the budget, split the file **first**, as its own commit (or its
own PR when the split is large), then build the ticket on the new seams. This is the
"prefactoring goes first" rule from `workflow.md`, applied when the size comes from growth rather
than from the spec.

### 3. Never mix a large refactor into a feature PR

A feature PR carries the feature. A refactor of more than a few files goes in its own PR, so the
reviewer can see the real change and the move separately. A refactor PR states that it changes no
behaviour — or names the behaviour it does change — and leans on the existing tests at the seams in
`workflow.md` (`WebApplicationFactory` for the API, Playwright e2e for client flows) passing
unchanged.

## Known over budget

Files already past the budget. Split one as the prefactor for the next ticket that touches it, and
remove it from this list when it is done.

- `client/src/pages/TransactionsPage.tsx` (677 lines)

Past the line count but judged fine, because each has one clear job:

- `src/Api/Features/Import/Import.Csv.cs` (428 lines) — CSV splitting and shape detection only.
