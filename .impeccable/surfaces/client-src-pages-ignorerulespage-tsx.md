---
version: 1
slug: "client-src-pages-ignorerulespage-tsx"
primary_target: "client/src/pages/IgnoreRulesPage.tsx"
related_targets: ["client/src/pages/ImportPage.tsx"]
---

# Surface brief — Ignore rules & the import preview (`/import` station 3, `/import/rules`)

Ticket #9 of 12 (https://github.com/jsteinshouer/get-my-money-right/issues/9), where this brief is
also published. Produced by `shape` and confirmed by the user on 2026-09-24.

Extends the ticket #8 brief for `/import`
([client-src-pages-importpage-tsx.md](client-src-pages-importpage-tsx.md)), which built stations 1
and 2 and left station 3 present but inert. This ticket fills that frame rather than replacing it.

Established visual world:
**The Ruled Cash Book** ([DESIGN.md](https://github.com/jsteinshouer/get-my-money-right/blob/main/DESIGN.md)).
No new visual direction. Product truth lives in
[PRODUCT.md](https://github.com/jsteinshouer/get-my-money-right/blob/main/PRODUCT.md).

## 1. Job and audience

**Visitor mode: Operate.** Desk session only.

A household member one station further into the import they started in #8. The mapping is settled;
they are now looking at what the file will actually put in their transaction list — and seeing, for
the first time, the recurring noise they never want: autopay confirmations, the transfer to savings,
the card payment that is the same money counted twice.

Their state of mind is **suspicion, not curiosity**. They are checking the app before trusting it.
Product principle 2 — *trust is built from counts, not assurances* — is the whole job of this screen.

## 2. Outcome and proof

They end knowing **exactly which rows are going in, which are not, and why** — and able to write the
rule that drops a noise row without leaving the page or remembering its wording.

Product truth no generic importer carries: **there is no Transfer entity.** The inter-account
transfer is dropped by the same text rule as any other noise, so the rule mechanism has to be good
enough to carry a modeling decision on its back.

## 3. Selected direction — the reading, grown to the whole file

Station 3 is not a new screen. **It is `.reading` — the reads-as block #8 already prints under the
double rule — carrying every row instead of one.** The rehearsal on the Map step becomes the
performance.

```
UPLOAD ──── MAP COLUMNS ──── PREVIEW & CONFIRM
  ✓             ✓                  ●

Aug 14   Kroger #442                      −84.19
Aug 15   Shell Oil 5578                   −41.02
A̶u̶g̶ ̶1̶5̶ ̶ ̶ ̶A̶u̶t̶o̶p̶a̶y̶ ̶T̶h̶a̶n̶k̶ ̶Y̶o̶u̶ ̶ ̶ ̶ ̶ ̶ ̶ ̶ ̶ ̶+̶3̶1̶2̶.̶0̶0̶
         Skipped · contains AUTOPAY
Aug 16   Trader Joe's #221                −63.40
═══════════════════════════════════════════════
41 rows will import · 3 skipped by a rule
```

Three devices, all of them already in the system:

- **The strike is a drawn rule, not `text-decoration`.** One continuous oxblood hairline across the
  full entry — date, description and amount together — the pen stroke a cash book makes through a
  line that does not count. `line-through` would break into three disconnected segments across the
  grid and read as a typographic effect rather than a mark on the page. The row is **never tinted**;
  the figure drops to `--ink-3`. The stroke is decorative and `aria-hidden`.
- **The reason is visible text under the entry**, in the tracked-caps register:
  `Skipped · contains AUTOPAY`. It names the rule that caught the row, so an over-broad rule is
  diagnosable at a glance, and screen readers get the state from real text rather than ARIA.
- **The tally is `.exceptions`**, printed as entries under the double rule — never a badge or toast.
  `aria-live="polite"`, so saving a rule announces the new counts. In #10 the duplicate count joins
  this same line; the frame does not change.

### The focal moment — the rule is born from the row

Every **unstruck** row carries a persistent trailing control, `Ignore rows like this`. It opens a
slip *beneath that row* — the `CorrectionSlip` idiom already established on Transactions: a
correction is written on a slip under its entry, as a real form, never a modal.

```
Aug 15   Autopay Thank You                +312.00
┌───────────────────────────────────────────────┐
│ Skip rows where the description                │
│   [ contains ▾ ]  [ AUTOPAY THANK YOU       ]  │
│   for [ Sapphire Card ▾ ]                      │
│                      [ Add rule ] [ Cancel ]   │
└───────────────────────────────────────────────┘
```

Match text is pre-filled from **the normalized description the row is showing** — matching against
anything else would strike a row for text the household never saw. Scope offers the import's account
or *All accounts*.

On save, the preview re-reads and **the row strikes itself in place**: the pen stroke draws across
the row, ~260ms on the same exponential ease as `spread-turn`, and the tally above it re-counts. That
is the single authored moment of this ticket and the entire payoff — the rule you just wrote acting
on the row that provoked it, without a navigation. It collapses to an instant state change under
`prefers-reduced-motion`.

No override. A struck row is skipped; if that is wrong, the rule is wrong.

### `/import/rules` — the ledger of rules

A ruled table, reached by a `.memo` link on the Import page and from the slip's confirmation. Not a
masthead section: the masthead stays at seven.

| MATCH | SCOPE | |
|---|---|---|
| contains **AUTOPAY** | All accounts | Delete |
| contains **TRANSFER TO SAVINGS** | Checking | Delete |

Global rules first, then by account name. Its own add form sits above the table (the e2e path creates
a rule before ever uploading a file). Delete uses the existing inline `.confirm` panel, not a browser
dialog.

## 4. Scope and boundaries

**Build — API:** `ImportIgnoreRule` entity + migration; create, fetch-all, delete under
`Features/Import/`; a new full-file preview operation carrying per-row skip reasons
(`Import.ReadPreview` stays the Map step's 5-row rehearsal — different response, different operation,
per REPR).

**Build — client:** station 3 live and read-only; the from-row slip; `/import/rules`.

**Untouched:** stations 1 and 2 and every #8 API contract, status, transactions, budgets, categories,
tags, accounts, auth.

**Still nothing is written to `Transactions`.** Station 3 ends with a memo saying so: *"Nothing is
saved yet. Importing arrives with the next ticket."*

**Deferred to #10, and the frame must not change to admit them:** duplicate marks on the same rows,
the Import action, the three result counts.

**Anti-goals:** modals, toasts, "skipped" pills or badges, tinted or red rows, a trash icon with no
label, per-row un-skip, regex, a rule that also assigns a category, any auto-generated starter rules.

## 5. States and ranges

Ranges: **0–12 rules** (realistically 2–6), match text 3–40 characters, files of **20–2000 rows**,
all printed — no virtualization, no pagination.

- **No rules yet** — nothing is struck; the tally reads `44 rows will import`. The rules page prints
  a ruled empty naming where rules usually come from: *"No ignore rules yet. Add one from a row in an
  import preview."*
- **Every row caught** — `0 rows will import · 47 skipped by a rule`, with a `.note[data-signal]`
  beneath: the rule is almost certainly too broad, and it names which one.
- **Duplicate rule** (same text, type and scope) — refused, named at the field via `.field-error`.
- **Empty or whitespace-only match text** — refused at the field.
- **Row that both matches a rule and fails to parse** — the pipeline is map → ignore → dedupe, so the
  strike wins and the parse error is not printed. The row is leaving either way; two reasons would
  read as two problems.
- **Rule deleted while a preview is open** — the preview re-reads on return to station 3; rows
  un-strike.
- **Saving** — the struck rule already in the vocabulary. Never a spinner.
- **409 / error** — existing conflict handling.
- **Narrow** — the preview inherits `.table-scroll`; the page never scrolls sideways. Not held to the
  400px bar (PRODUCT principle 1: import is desk work).

## 6. Constraints and open decisions

**Binding:** React 19 + TS, new vocabulary extends `client/src/styles/ledger.css`; Minimal API REPR,
one file per operation under `Features/Import/`; EF Core + SQLite; light-only; fonts self-hosted.

**Matching semantics — fixed, not the builder's to choose:** case-insensitive, against the normalized
description, whitespace trimmed and collapsed the same way #10's dedupe will normalize it.
`MatchType` is `Contains | StartsWith | Equals`.

**One deviation to flag:** the entity carries `IsActive` per the spec, and the preview filters on it —
but the acceptance criteria name only create, fetch-all and delete. So rules are created active and
**delete is the off switch**; there is no update endpoint and no active/inactive toggle in this
ticket. A builder must not invent one.

**A builder must not invent:** regex or wildcard matching, rules that rewrite rather than skip,
auto-suggested rules, category assignment at import, a fourth station, or any insert into
`Transactions`.

**Tests:** `WebApplicationFactory` integration tests per operation plus a preview test driving a
fixture CSV containing a matching row and asserting the row comes back marked skipped with its
reason. Playwright `client/e2e/import-ignore-rules.spec.ts`: create a rule at `/import/rules` →
upload the fixture → map → confirm the matching row renders struck and the tally counts it — plus the
from-row path, since that is the primary creation affordance and the e2e is the only thing that
proves the strike-in-place works.

**Confirmed with the user (2026-09-24):**

1. **Preview scope** — 09 lights up station 3 as a full-file read-only preview; #10 adds dedupe
   marks, the commit action and the three counts.
2. **Rule home** — born from the preview row, managed at `/import/rules`. No new masthead section.
3. **Override** — none. The rule is the rule; an over-broad rule gets edited, not bypassed per row.

**Assumed, correct freely:** `/import/rules` as the route; global-first sort order; the trailing-cell
placement of `Ignore rows like this`; 260ms for the strike.
