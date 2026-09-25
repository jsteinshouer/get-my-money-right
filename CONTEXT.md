# Get My Money Right

A self-hosted budgeting app for one two-person household. Its language is the household's own:
what money was spent on, whether it had to be, and which of the two of them recorded it.

## Language

**Household member**:
One of the two people who share this app. Each signs in separately, but all data is shared —
there is no private account and no permission between them.
_Avoid_: User, owner, tenant

## The ledger

**Account**:
A place money sits or is owed from — a checking account, a savings account, a credit card.
_Avoid_: Bank, card, source

**Transaction**:
One movement of money on one account: a date, a signed amount, a description, at most one
category, a required Need/Want, and any number of tags.
_Avoid_: Entry, record, line item, purchase

**Category**:
The household's own name for a kind of spending — Groceries, Dining Out, Utilities. Flat, with
no hierarchy, and entirely of their making; there is no starter set.
_Avoid_: Bucket, envelope, classification

**Need / Want**:
Whether a transaction had to happen or was discretionary. Judged per transaction, never derived
from its category, and never absent. Referred to together as a transaction's *classification*,
which means this and nothing else.
_Avoid_: Essential/non-essential, discretionary flag, priority

**Tag**:
A free-form label that cuts across categories — "Vacation 2026", "Tax Deductible". A transaction
may carry many. A tag is a label, not something the ledger depends on.
_Avoid_: Label, keyword, group

**Budget**:
A dollar limit the household sets for one category in one month, to compare actual spending
against. Not an envelope and not zero-based: exceeding it is information, not a block.
_Avoid_: Limit, target, allocation, envelope

**Uncategorized**:
A transaction with no category yet. A real state a transaction can be in — arriving from an
import is the usual way in — not a stand-in category.
_Avoid_: Unassigned, misc, other, "Uncategorized" as a category name

**Review queue**:
The uncategorized transactions, taken together. The household's standing to-do list after an
import, and the only thing that marks a transaction as not yet judged by a person.
_Avoid_: Inbox, pending, unreviewed list

## Import

**Import**:
Taking one CSV a household member exported from a bank or card website and turning its rows into
transactions on one account.
_Avoid_: Sync, upload, ingest, feed

**Column mapping**:
How one account's export is read — which column carries the date, the description and the amount
(or the debit/credit pair), in what date format, with what separator. Described once per account
and remembered, because a bank's format is the bank's, not the file's.
_Avoid_: Schema, format, parser config

**Import rule**:
A standing statement about how to read incoming rows, written by a household member and applying
to one account or to every account. Two kinds: ignore rules and assignment rules. A rule speaks
about rows arriving, never about transactions already stored.
_Avoid_: Processing rule, filter, automation, macro

**Ignore rule**:
An import rule that strikes a matching row out of the import entirely — an autopay confirmation,
a card payment, a transfer between the household's own accounts. This is also the only way a
transfer is described; the app has no transfer of its own.
_Avoid_: Exclusion, blocklist, skip rule

**Assignment rule**:
An import rule that gives a matching row a category, a Need/Want, tags, or any combination of
the three. It says what a row *is*, where an ignore rule says a row does not count.
_Avoid_: Classification rule, categorization rule, processing rule, auto-tagger

**Match text**:
The words an import rule holds up against a row's description — contained in it, starting it, or
being the whole of it, ignoring case. Plain text a person can read back; never a pattern.
_Avoid_: Pattern, regex, expression, query
