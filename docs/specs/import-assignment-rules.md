# Import Assignment Rules

Tracked in GitHub Issues under the **Budgeting App v1** milestone. This spec amends ticket 10 (issue #10, CSV Import Confirm & Dedupe) and adds tickets 13–16 (issues #26–#29).

Domain vocabulary for this spec is defined in [`CONTEXT.md`](../../CONTEXT.md). The two decisions it rests on are recorded as [ADR-0001](../adr/0001-uncategorized-is-a-real-state.md) and [ADR-0002](../adr/0002-assignment-rules-apply-at-import-only.md).

## Problem Statement

A household member exports a CSV from the bank, imports it, and lands with a pile of rows that the app knows nothing about. Ignore rules already strike out the noise — autopay confirmations, card payments, transfers — but every surviving row arrives with no category and no judgment about whether it had to be spent. The household then re-does, by hand, a decision they have already made dozens of times: KROGER is Groceries and a Need, every single month; the Amazon charge is Shopping and a Want; anything from the airline belongs to the vacation they are tracking.

That hand-classification is the tedium the app was built to remove. It is worst exactly when the app is most useful — the first import of a year's history — and it never gets easier, because nothing the household teaches the app on Tuesday is remembered on Wednesday. Ignore rules prove the mechanism works: a household member writes "contains AUTOPAY" once and never thinks about those rows again. Nothing equivalent exists for the rows they actually want to keep.

## Solution

**Assignment rules**: standing statements about how to read incoming rows, written by a household member, that give a matching row a category, a Need/Want, tags, or any combination. They are the second kind of import rule, sitting beside the ignore rule on one rules page and matching a row's description with the same plain, case-insensitive text the ignore rules use.

Rules do their work where the household can see it. The import preview prints each row's category, Need/Want and tags as the rules resolved them, each citing the rule that decided it — the same honesty a struck row already gets when it names the ignore rule that caught it. Any row can be corrected in place before the import is confirmed, as a one-off that leaves the rule alone. A row can also become a rule on the spot: the household learns that a merchant is Groceries at the moment they are staring at it, and "always classify rows like this" turns that into a rule that immediately reclassifies the rest of the file.

Rules cover the merchants the household has taught it and never the long tail, so the unmatched rows need somewhere honest to land. Uncategorized becomes a real state a transaction can be in, and the uncategorized transactions together form the **review queue** — the standing to-do list after an import. An unmatched row still gets a Need/Want, because the app exists to keep discretionary spending visible, and it defaults to **Want**: a wrong Want overstates discretionary spending and nags until someone fixes it, where a wrong Need quietly shrinks the one number the household is watching.

The third moment of learning is weeks later, correcting a row on the transactions page. Doing it there also offers to write the rule — and, because dozens of matching rows may already be sitting in the review queue, offers to apply it to them, showing the count first. That is the only path by which a rule touches a transaction already stored.

## User Stories

### Writing rules

1. As a household member, I want to write a rule that gives rows matching some text a category, so that merchants I recognize stop arriving uncategorized.
2. As a household member, I want a rule to be able to set Need/Want, so that recurring essentials are judged once rather than every month.
3. As a household member, I want a rule to be able to apply tags, so that spending I am tracking across categories (a vacation, tax-deductible purchases) collects itself.
4. As a household member, I want one rule to be able to set any combination of category, Need/Want and tags, so that "KROGER is Groceries and a Need" is one rule and one line to read, not three.
5. As a household member, I want to write a rule that only applies tags, so that I can track a vacation without also dictating the category of every airline charge.
6. As a household member, I want to be stopped from saving a rule that sets nothing at all, so that the rules page never fills with rows that do nothing.
7. As a household member, I want to choose whether a rule matches rows whose description contains, starts with, or exactly equals my text, so that a broad merchant name and a precise one are both expressible.
8. As a household member, I want matching to ignore letter case, so that KROGER, Kroger and kroger are one rule.
9. As a household member, I want my rule to match the description exactly as the preview shows it to me, so that a rule I wrote by reading a row behaves the way that row led me to expect.
10. As a household member, I want a rule to apply to one account or to all of my accounts, so that a card-specific merchant and a household-wide one are both expressible.
11. As a household member, I want to see my assignment rules and my ignore rules on one page, so that "what does the import do to my rows" is answered in one place.
12. As a household member, I want to read a saved rule back as a sentence, so that an over-broad rule is diagnosable at a glance.
13. As a household member, I want to delete a rule I no longer want, so that the rules stay a description of how we actually categorize.

### Rules doing their work

14. As a household member, I want rules applied to every row of a file I am importing, so that a year of history classifies itself in one pass.
15. As a household member, I want a row struck by an ignore rule to be left alone by assignment rules, so that a row I have said never counts does not also acquire a category.
16. As a household member, I want several rules to be able to contribute to one row, so that a rule about the category and a rule about a tag can both land.
17. As a household member, I want the tags from every matching rule to accumulate on the row, so that a purchase tracked from two angles keeps both.
18. As a household member, I want the first rule I wrote to win when two rules disagree about a row's category or Need/Want, so that the outcome is predictable rather than a race.
19. As a household member, I want to see what the rules decided for each row before I commit the import, so that a rule that is too broad is caught before three hundred rows are classified by it.
20. As a household member, I want each row to name the rule that classified it, so that I know which rule to fix.
21. As a household member, I want to correct a row's category, Need/Want or tags in the preview, so that a one-off exception does not force me to write or weaken a rule.
22. As a household member, I want a correction I make in the preview to leave my rules untouched, so that fixing one row never silently changes how every future import behaves.
23. As a household member, I want to turn a row I am looking at into a rule, so that I can teach the app at the moment I learn something.
24. As a household member, I want a rule created from a row to prefill its match text and assignments from that row, so that teaching the app is one confirmation and not a form.
25. As a household member, I want the rest of the file to reclassify immediately when I create a rule from a row, so that I see what my new rule did before committing anything.
26. As a household member, I want to see how many rows a pending import will classify and how many it will leave for review, so that I know what I am taking on before I confirm.

### The review queue

27. As a household member, I want a row no rule matched to import with no category at all, so that it is visibly unfinished rather than filed somewhere misleading.
28. As a household member, I want the uncategorized transactions gathered as a review queue, so that I have one list to work through after an import.
29. As a household member, I want a row no rule classified to arrive as a Want, so that spending nobody has judged inflates discretionary spending rather than hiding inside essentials.
30. As a household member, I want to filter and sort my transactions so the uncategorized ones are reachable, so that working the queue does not mean scrolling the whole ledger.
31. As a household member, I want a transaction to stop appearing in the review queue once I give it a category, so that the queue empties as I work it.

### Learning after the fact

32. As a household member, I want to turn a correction I make on the transactions page into a rule, so that the most common moment of learning is not wasted.
33. As a household member, I want to be told plainly that a rule created this way affects future imports, so that I am not left wondering whether the button did anything.
34. As a household member, I want to be offered the chance to apply that new rule to transactions I have already imported, so that the backlog it would have caught gets cleared at the same moment.
35. As a household member, I want to see how many existing transactions a rule would change before it changes them, so that I can back out of an over-broad rule.
36. As a household member, I want applying a rule to existing transactions to fill in missing categories and add tags, so that the review queue shrinks.
37. As a household member, I want applying a rule to correct Need/Want only on transactions still awaiting review, so that a judgment I made deliberately is never overwritten by a rule I wrote later.
38. As a household member, I want rules to otherwise never touch transactions already stored, so that my ledger only changes when I change it.

### Keeping rules honest

39. As a household member, I want to be stopped from deleting a category that my rules depend on, so that a rule does not quietly stop classifying.
40. As a household member, I want to be told how many rules use a category I tried to delete, so that I know what to fix first.
41. As a household member, I want deleting a tag to simply drop it from my rules, so that removing a label I no longer track is not a chore.
42. As a household member, I want a rule left with nothing to set after a tag deletion to be removed with it, so that the rules page never shows a rule that cannot do anything.

## Implementation Decisions

### Vocabulary

- **Import rule** is the umbrella term for both kinds. The two kinds are the **ignore rule** (already shipped) and the **assignment rule** (new). "Classification" continues to mean Need/Want and nothing else, so an assignment rule is deliberately not called a classification rule. "Processing rule" and "categorization rule" are not used. See `CONTEXT.md`.

### The assignment rule

- New entity in the `Import` feature area, stored in the shared context alongside `ImportIgnoreRule`, with the same scoping convention: a nullable account reference where null means every account.
- Carries match text and a match type (`Contains` / `StartsWith` / `Equals`) reusing the ignore rule's match-type vocabulary, plus an optional category reference, an optional Need/Want, and a set of tag references (many-to-many).
- **At least one of category, Need/Want or tags must be set** — enforced by a validator, since a rule that assigns nothing cannot be expressed in the UI as a sentence.
- Match text is required and trimmed. No regex and no wildcards, matching the deliberate dullness of ignore matching: a household member writes a rule by reading a row, and a rule that matches something other than what they read is a rule they cannot reason about.
- Carries an active flag defaulting to true and a created-by user reference, mirroring the ignore rule. There is no toggle UI for the flag in this scope.
- Operations mirror the shipped ignore-rule endpoints: create, fetch all, delete. **No edit** — a wrong rule is deleted and rewritten, as ignore rules already are.

### Matching and resolution

- The ignore rule's matcher is generalized into a **shared matcher** over match text plus match type, used by both kinds of rule, keeping one definition of what "contains" means. It also keeps the existing "say the rule back in words" behaviour so both kinds of rule can be printed as a sentence.
- Matching runs against the **mapped, normalized description** — the same string the preview prints and the duplicate check compares — not against raw CSV columns. Rules are therefore portable across accounts and survive a bank renaming its columns.
- **Resolution across several matching rules**: category and Need/Want are taken from the first matching rule by id that sets them; tags are unioned across every matching rule. Ordering by id matches the ignore rules' existing first-match-by-id convention, so "earliest rule wins" is one sentence for the whole feature.
- Resolution is a pure function over (the rules in force, one row's description) returning the resolved assignments plus the rules that contributed them, so a row can cite more than one rule.

### Import pipeline

The spec's pipeline order is amended to insert assignment rules after the duplicate check:

> parse → apply column mapping (sign normalization) → apply ignore rules (skip matches, terminal) → duplicate check → **apply assignment rules** → insert

- An ignored row is never assigned — it is not being imported, so classifying it is meaningless. A duplicate-skipped row is likewise not assigned.
- The whole-file preview response gains each row's resolved category, Need/Want and tag list, along with the rules that decided them, in the same shape the struck rows already carry their ignore-rule reason.
- The preview's counts are extended so the household can see how many rows will import already classified and how many will land in the review queue.
- Confirming an import carries the household member's per-row overrides from the preview. Overrides are one-off values attached to the pending import; they never write back to a rule.

### Transaction shape (amends ticket 10)

- **A transaction's category becomes optional.** Uncategorized is a real state, not a reserved category row — see ADR-0001. The existing restrict-on-delete relationship is retained for transactions that do have a category.
- The uncategorized count on the status page is computed from the absence of a category rather than inferred client-side from a category id missing from the loaded list, which is what it does today.
- **Need/Want remains required** on every transaction. An imported row that no rule classified defaults to **Want**.
- There is deliberately **no separate "reviewed" flag**. The absence of a category is what marks a transaction as not yet judged by a person; assigning a category *is* the review, because nobody picks one without reading the row.
- Manual transaction entry is unchanged: a household member entering a transaction by hand still chooses a category, and the API still requires an explicit Need/Want rather than defaulting it. The Want default belongs to import, not to the transactions API.

### Retroactive application

- Rules apply at import only, with exactly one exception: creating a rule from an existing transaction offers to apply it to transactions already stored. See ADR-0002.
- That operation is count-then-confirm: the household member is shown how many transactions would change before anything is written.
- **What it may change**: it fills an absent category, unions tags, and sets Need/Want **only where the transaction has no category** — i.e. the transaction is still in the review queue and carries a guessed Want rather than a person's judgment. A categorized transaction keeps its Need/Want untouched.
- The rules page itself offers no bulk apply in this scope.

### Referential integrity

- Deleting a **category** referenced by any assignment rule returns a conflict naming how many rules use it, matching the protection transactions already have. A rule pointing at a vanished category would silently stop classifying.
- Deleting a **tag** drops it from every rule that references it, matching the existing treatment of tags as labels the ledger does not depend on. A rule thereby left setting nothing is deleted along with it.

### Client

- The shipped ignore-rules page becomes the **Import rules** page with two sections, one per kind. Both sections read their rules back as sentences.
- The import preview station prints resolved assignments per row with inline editing, and offers "always classify rows like this" per row.
- The transactions page's existing correction affordance gains the rule-creation offer, followed by the apply-to-existing offer with its count.
- Design briefs for all of these surfaces are produced by `/impeccable shape` before ticket breakdown; the import flow shares one brief, as tickets 8–10 already do.

### Build order

1. **Ticket 10 (amended, issue #10)** — confirm and dedupe, owning the schema truth: optional category, Want default on import, counts. Ships first and unblocks everything else.
2. **Ticket 13** — the assignment rule itself: entity, validator, create/fetch/delete operations, the shared matcher, and the rules page section.
3. **Ticket 14** — rules applied at import: resolution across the file, preview display with rule citations, the classified / for-review counts, and confirm-time assignment.
4. **Ticket 15** — the preview row's correction slip: per-row override carried into confirm, "make a rule from this row" with live reclassify, and the skip branch.
5. **Ticket 16** — transactions-page authoring plus apply-to-existing with its count.

Each is a vertical slice demoable on its own and sized for one session.

## Testing Decisions

A good test here exercises what a household member can observe — the transactions an import produces, the counts it reports, what the preview says about a row — not how the resolver is structured internally. Assertions name rules and outcomes, not method calls.

**No new seams.** The three existing seams cover this feature:

- **The HTTP API**, via `WebApplicationFactory<Program>` gray-box integration tests against real SQLite, one per operation, mirroring the existing per-feature test layout. This is where rule creation/fetch/deletion, resolution across a whole file, confirm-time assignment, the category-delete conflict, the tag-delete cascade, and apply-to-existing are all proven.
- **Validator unit tests** (no HTTP, no database) for the create-rule command's input shape only: match text required, at least one assignment set. Mirrors the shipped ignore-rule validator tests.
- **Playwright end-to-end tests** against the real running app, mirroring the shipped import and ignore-rule specs.

**Resolution semantics are tested through the API, not against the resolver directly.** A direct unit seam on the resolver was considered and rejected: the shipped ignore matcher has no tests of its own either — it is proven through the endpoint that uses it — and adding a seam here would duplicate coverage while raising the codebase's seam count. Multi-rule cases are expressed as CSV fixtures whose rows several rules match.

Cases the integration tests must carry:

- A fixture where one rule assigns a category and Need/Want, asserting the imported transactions carry both.
- A fixture where two rules match one row — one setting a category, one setting a tag — asserting the category from the first and the union of tags.
- A fixture where two rules set the same field, asserting the earlier rule by id wins.
- A fixture where an ignore rule and an assignment rule match the same row, asserting the row is skipped and nothing is assigned.
- A fixture with rows no rule matches, asserting they import with no category and with Want.
- A rule scoped to one account, asserting it does not touch an import into another account.
- Creating a rule that sets nothing, asserting it is rejected.
- Deleting a category a rule uses, asserting a conflict and that the category and rule both survive.
- Deleting a tag a rule uses, asserting the tag leaves the rule and that a rule left empty is gone.
- Apply-to-existing: asserting the reported count matches what changes, that an absent category is filled, that tags are unioned, that Need/Want is corrected on uncategorized transactions only, and that a categorized transaction's Need/Want survives untouched.

End-to-end flows to cover:

- Writing an assignment rule on the import rules page and seeing it read back as a sentence, then deleting it.
- The full import flow with rules in force: upload → map → preview showing assigned rows citing their rules → override one row → confirm → the resulting transactions reflect both the rules and the override.
- Creating a rule from a preview row and watching the rest of the file reclassify.
- Correcting a transaction, creating a rule from it, and accepting the apply-to-existing offer, asserting the queue shrinks by the stated count.

## Out of Scope

- **Editing a rule.** Rules are created and deleted, as ignore rules already are.
- **Rule priority or manual reordering.** Earliest rule by id wins; a handful of rules in a two-person household does not need an ordering UI.
- **Regex, wildcards, or matching on anything but the description.** Raw CSV column matching was considered and rejected: rules would become per-bank-format and break when a bank renames a column.
- **Amount, date or account-type conditions** on a rule. Description-only for this scope.
- **Bulk apply from the rules page.** The only retroactive path is the offer made when a rule is created from a transaction.
- **Automatic reclassification when a rule is saved or an existing rule changes.** See ADR-0002.
- **A reviewed/unreviewed flag** distinct from the absence of a category.
- **Suggesting rules.** The app does not propose rules from observed patterns or from what the household categorizes by hand; every rule is written deliberately.
- **Rules that create, split, merge or transfer transactions**, or that set the account or amount. A rule describes what a row *is*, never what it is worth or where it belongs.
- **Toggling a rule inactive from the UI.** The flag exists on the entity; no surface exposes it in this scope.

## Further Notes

- This spec follows a grilling session run with `/domain-modeling`, which produced the repo's first `CONTEXT.md` and the first two ADRs. The two decisions worth re-reading before implementing are ADR-0001 (uncategorized as a real state, Want as the import default) and ADR-0002 (import-time only, with one bounded retroactive apply).
- The Want default is a deliberate inversion of the intuitive choice. Both defaults are guesses; they fail in opposite directions, and the household would rather be wrong in the direction that gets noticed.
- The feature deliberately borrows the ignore rule's every convention it can — nullable account scope, match types, first-match-by-id, rules read back as sentences, create/fetch/delete with no edit — so that "import rule" is one idea with two kinds rather than two subsystems that happen to sit on one page.
- Ticket 10 was written before any of this was decided and its acceptance criteria assume a category is always present. It needs amending, not just following, and it must ship before tickets 13–16.
