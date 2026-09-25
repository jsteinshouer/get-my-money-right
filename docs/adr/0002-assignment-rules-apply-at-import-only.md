---
status: accepted
---

# Assignment rules apply at import only, with one bounded retroactive apply

An assignment rule states how to read *incoming* rows. It does not run over the ledger, so a rule
written today can never silently rewrite a classification a household member set deliberately
last month — the failure mode that makes rule engines untrustworthy in a shared household ledger.
Rules take effect in the import preview, where their work is shown per row, cites the rule that
decided it, and can be overridden as a one-off before anything is committed.

The one exception is the moment of learning. Correcting a transaction and asking the app to
"always classify like this" creates the rule and then *offers* to apply it to matching
transactions already stored, with the count shown first. That apply fills an absent category and
unions tags, and corrects Need/Want **only where the category is still absent** — a row still in
the review queue carries a guess, while a categorized row carries someone's judgment and is left
alone. See [ADR-0001](./0001-uncategorized-is-a-real-state.md).

## Considered Options

Reclassifying automatically whenever a rule is saved gives the best feedback loop and keeps the
ledger consistent with the rules, but a single over-broad rule would overwrite months of
deliberate corrections with no undo. Pure import-time-only, with no exception at all, was the
first decision we reached; we reopened it because the most common moment of learning is
correcting a row weeks after its import, and a rule created there that visibly does nothing reads
as a broken button.

## Consequences

"Apply to existing" needs its own count-then-confirm step and is the only write path where a rule
touches stored transactions. Everywhere else — the rules page, the import preview — a rule
affects future imports only, and the UI says so in those words.
