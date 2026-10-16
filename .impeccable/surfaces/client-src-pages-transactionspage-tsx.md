---
version: 1
slug: "client-src-pages-transactionspage-tsx"
primary_target: "client/src/pages/TransactionsPage.tsx"
related_targets: []
---

# Surface brief — Teaching a rule from a correction (`/transactions`)

Covers **ticket 15** of the
[Import Assignment Rules spec](https://github.com/jsteinshouer/get-my-money-right/blob/main/docs/specs/import-assignment-rules.md).
Produced by `shape` and confirmed by the user on 2026-09-25. First brief for this surface: the
transactions page shipped in ticket #16 before briefs existed, and this one covers **only the
addition**, not the page.

Established visual world:
**The Ruled Cash Book** ([DESIGN.md](https://github.com/jsteinshouer/get-my-money-right/blob/main/DESIGN.md)).
No new visual direction. Companion brief for the import flow:
[client-src-pages-ignorerulespage-tsx.md](client-src-pages-ignorerulespage-tsx.md) — the slip
idiom below must match it exactly.

## 1. Job and audience

**Visitor mode: Operate.** The monthly desk session, mid-ritual, working the review queue — a batch
of transactions the import left with no category. Also the ad hoc visit weeks later, correcting one
row that was filed wrong.

Their state of mind here is **repetition fatigue**, not suspicion. They have just assigned Groceries
to the fourth Kroger row in a row. The app has watched them do it four times and learned nothing.
That is the feeling this ticket answers.

## 2. Outcome and proof

The correction they were making anyway becomes the last time they make it — and the backlog it
would have caught is cleared in the same breath, with the number stated before anything moves.

Product truth: **rules describe incoming rows, not the ledger** ([ADR-0002](https://github.com/jsteinshouer/get-my-money-right/blob/main/docs/adr/0002-assignment-rules-apply-at-import-only.md)).
A rule saved here changes nothing already stored unless the household says so, in that moment, to
that count. This surface is the one exception in the whole product, and it earns it by being
explicit.

## 3. Selected direction — the slip already knows how to ask

The page's `CorrectionSlip` — the entry stays on its line, the correction is written on a slip
beneath it — gains **the same `APPLIES TO` section the import slip carries**, in the same wording
and the same order. The household learns one form, in two places, and the import preview is where
they meet it first.

```
Aug 16   Trader Joe's #221      —        Want      −63.40
┌────────────────────────────────────────────────────┐
│ ACCOUNT  DATE  DESCRIPTION  CATEGORY  N/W  AMOUNT  │
│  … the existing correction fields, unchanged …     │
│                                                    │
│ APPLIES TO                                         │
│  (•) this transaction only                         │
│  ( ) every row where the description               │
│      [ contains ▾ ] [ TRADER JOE'S           ]     │
│      for [ All accounts ▾ ]                        │
│                          [ Save ] [ Cancel ]       │
└────────────────────────────────────────────────────┘
```

Two differences from the import slip, both forced by where we are standing:

- **The default is inverted.** In the preview, teaching a rule is the point, so *every row like
  this* is preselected. Here the household came to fix one row; *this transaction only* is
  preselected and the rule is the deliberate reach. A page that turns every correction into a
  standing rule would be a page nobody dares correct anything on.
- **There is no skip branch.** An ignore rule strikes rows out of an import; it has nothing to say
  about a transaction that is already in the ledger. Offering it here would imply it deletes
  something.

### The focal moment — the offer, with its number

Choosing the rule branch and saving does two things and says both. The rule is saved, and the
household is told plainly what that means — *future imports*, in those words, because this is the
one place where a button could be mistaken for acting on what is on screen:

```
Aug 16   Trader Joe's #221    Groceries   Need     −63.40
┌────────────────────────────────────────────────────┐
│ Saved. Rows where the description contains         │
│ TRADER JOE'S will be filed as Groceries · Need     │
│ on future imports.                                 │
│                                                    │
│ 17 transactions already imported match this rule.  │
│ 12 of them would change; 5 you have already filed  │
│ are left alone.                                    │
│                  [ Fix those 12 ] [ Leave them ]   │
└────────────────────────────────────────────────────┘
```

This is a `.note` beneath the corrected entry — not a toast, not a modal, not a banner at the top of
the page. It stays until answered or dismissed, because a household member who looked away
mid-session must find it where they left it.

**The count is what would change, not what matches.** Those are different numbers, and the
difference is the ADR-0002 rule made visible: a transaction someone has already filed keeps its
Need/Want, so it matches without changing. Printing only "17 match" and then changing 12 would be
the app misreporting its own work — a direct violation of PRODUCT principle 2, *trust is built from
counts, not assurances*.

**On accept**, the rows change and the note re-prints as the result: *"12 transactions filed as
Groceries · Need."* The review queue count in the masthead-adjacent status shrinks on next read; no
animation is authored for rows changing off-screen. **On leave**, the note prints a one-line
confirmation that the rule still stands for future imports, then goes.

## 4. Scope and boundaries

**Build:** the `APPLIES TO` section in the existing correction slip; the rule-creation call; the
count-then-confirm offer and its apply operation; the result line.

**Untouched:** the transactions table, its filters, its inline Need/Want reclassify control, the
correction slip's existing fields and validation, tag creation, delete, and every other page. The
inline Need/Want select in the row does **not** grow a rule offer — a one-click reclassify must stay
one click.

**Anti-goals:** modals; toasts; a banner at the top of the page; an undo affordance (the offer is
the undo — it is asked before anything moves); bulk rule creation from a multi-select; suggesting a
rule the household did not ask for, however obvious the repetition; a rules manager on this page
(`/import/rules` owns that); applying a rule to stored transactions from anywhere but this offer.

## 5. States and ranges

- **No matching stored transactions** — the offer does not print. The confirmation states the rule
  is saved for future imports and stops. An offer of zero is noise.
- **Matches exist but none would change** (all already filed) — the offer does not print; the
  confirmation says so in one line: *"17 match, all already filed. Nothing to change."*
- **Large match** (hundreds, after a first bulk import) — the number prints plainly; no warning
  styling. A big number here is the feature working. The apply is one request, not a per-row loop.
- **The rule duplicates an existing one** — refused at the field in the slip, as on the rules page.
- **Applying while the other household member is writing** — 409 handling as elsewhere; SQLite is
  single-writer and the desk session is sometimes two people at one screen.
- **Correction saved with *this transaction only*** — unchanged from today's behavior, no note, no
  extra round trip.
- **A tag is part of the rule** — the count still describes transactions changed; tags union onto
  rows that already have others, which is additive and needs no separate warning.
- **Narrow** — the slip already stacks; the note wraps. Not held to the 400px bar (PRODUCT
  principle 1: this is desk work).

## 6. Constraints and open decisions

**Binding:** React 19 + TS; vocabulary from `client/src/styles/ledger.css` (`.note`, `.confirm`, the
tracked-caps register); Minimal API REPR, one file per operation; EF Core + SQLite; light-only.

**Fixed, not the builder's to choose:**

- The apply fills an **absent category**, **unions tags**, and sets Need/Want **only where the
  transaction has no category**. A filed transaction's Need/Want is never touched. ([ADR-0002](https://github.com/jsteinshouer/get-my-money-right/blob/main/docs/adr/0002-assignment-rules-apply-at-import-only.md))
- Count first, change second. The count and the change must come from the same predicate, so the
  number the household accepted is the number that moved.
- This offer is the **only** path by which a rule touches stored transactions. Not on the rules
  page, not on save of a rule written in the import preview.

**A builder must not invent:** automatic reclassification, a scheduled or background re-apply, a
"review suggested rules" surface, an undo stack, or a preview list of the affected transactions —
the count is the preview, and a list of 300 rows on this page would bury the ledger the household
is working.

**Tests:** `WebApplicationFactory` integration tests asserting the reported count equals what
changes, that an absent category is filled, that tags union, that Need/Want is corrected only on
uncategorized transactions, and that a filed transaction's Need/Want survives. Playwright: correct a
transaction, create the rule from it, accept the offer, and assert the review queue shrinks by
exactly the stated count.

**Confirmed with the user (2026-09-25):** the slip carries the same `APPLIES TO` section as the
import preview; the retroactive apply is offered here and nowhere else.

**Assumed, correct freely:** *this transaction only* as the default branch; the `.note` placement
beneath the entry; the two-number wording of the count; `Fix those 12` / `Leave them` as the button
labels; no skip branch on this surface.
