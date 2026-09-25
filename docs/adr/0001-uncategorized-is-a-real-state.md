---
status: accepted
---

# Uncategorized is a real state, and an unclassified import defaults to Want

An import can only ever classify the merchants the household has taught it, so most first imports
arrive with a long tail nobody has judged yet. We make *uncategorized* a real state a transaction
can be in — no category at all, rather than a reserved "Uncategorized" category row — so the
review queue is something the ledger can be asked for rather than something the client infers
from a category id it fails to recognise. Need/Want stays required, because a transaction whose
discretionary-ness is unknown is exactly the transaction the app exists to surface, and an
unmatched row defaults to **Want**.

## Considered Options

A reserved `Uncategorized` category was cheaper — no schema change — but it makes a null into
something the household can budget against and rename, and "category" would stop meaning one
thing. Forcing every row to be classified before an import commits was rejected as reinstating
the data entry the app exists to remove.

Defaulting to **Need** was rejected deliberately. Both defaults are guesses, but they fail in
opposite directions: a wrong Need quietly shrinks apparent discretionary spending, which is the
one number the household built this app to watch, while a wrong Want overstates it and nags until
someone corrects it. We would rather be wrong in the direction that gets noticed.

## Consequences

A defaulted Want is a guess, not a judgment, and only the category's absence records that nobody
has looked yet — so a row keeps its guessed Want the moment it is categorized. There is
deliberately no separate "reviewed" flag: assigning a category *is* the review, because you
cannot pick one without reading the row.
