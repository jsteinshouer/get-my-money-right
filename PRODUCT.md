# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Exactly two people: a married couple sharing one household's finances, on their own home network. Both are the primary user; there is no admin/viewer split and no third audience.

They meet the app in four distinct situations, all confirmed as real:

- **Monthly desk session (laptop).** The anchor ritual. Export CSVs from the bank and credit card sites, import them, work through a batch of uncategorized transactions, set or adjust category budgets. Long, focused, dense-data work.
- **Quick phone check-in.** Standing in a store or sitting on the couch: "how much is left in Dining Out?" Also logging a cash purchase that no CSV will ever carry. Short and glanceable.
- **Joint review, one screen, together.** Both of them looking at the same display, talking through discretionary (Want) spending. The screen is supporting a conversation between two people, not serving one operator.
- **Ad hoc investigation.** "How much did the vacation actually cost?" Filtering and tagging to answer a question that just came up and may never be asked again.

**Confirmed device split:** mobile matters for *checking budget status* — it does not need to carry reporting or data entry. Import, categorization batches, and trend reports are desk work.

## Product Purpose

Show a two-person household where its money actually goes each month, without anyone re-typing bank transactions by hand, and without the household's financial history living on someone else's server.

Success is that the monthly session is short and the answers are trusted: imports land clean, transactions carry a category and a Need/Want judgment, and actual-vs-budget per category is true at a glance. The secondary success is longitudinal — spotting that a category has been drifting upward over the trailing year, before it becomes a surprise.

## Positioning

Three things held together that neighboring products separate:

1. **Self-hosted, LAN-only, no ongoing cost.** A single Docker container with a SQLite file on their own home server. No third party ever holds the data, and there is no internet exposure to harden.
2. **Need/Want is a required field on every transaction**, independent of category. Discretionary spending is always identifiable and never silently uncategorized — a distinction generic budgeting apps either omit or derive from the category, which loses the per-purchase judgment call.
3. **The import remembers each account's export format.** Per-account column mapping, per-account and global text-match ignore rules, and automatic duplicate detection — so re-exporting an overlapping date range is safe and recurring noise never reaches the transaction list.

Free-form tags sit alongside categories rather than inside them, so a single purchase can be tracked from several angles ("Vacation 2026", "Tax Deductible") without distorting the category taxonomy.

## Operating Context

- **The import loop is the load-bearing workflow:** download CSV from the bank or card website → upload → confirm/apply the saved column mapping → preview → confirm → read back the counts. Imported rows arrive with no category assigned and form a review queue.
- Bank exports vary in the wild: signed single Amount column vs. separate Debit/Credit columns, varying date formats and delimiters, possible UTF-8 BOM. The mapping is stored per account for this reason.
- Categorization is a **batch** activity, not a per-transaction one. The desk session works through a queue of uncategorized rows.
- Budgets are set per category per month — a target to compare against, not an envelope allocation.
- Both users see all data. Nothing is siloed. But every transaction records who created it, so "who entered this?" is always answerable.
- Deployment reality: home network, HTTP only, no TLS, no reverse proxy, reachable from any device in the house. Data must survive container restarts via a mounted volume.

## Capabilities and Constraints

**Built and working:** cookie login for two accounts, account records (Checking/Savings/CreditCard, with deactivate rather than delete), user-managed flat categories, manual transaction entry with required Need/Want, category budgets per month, and the budget-status home screen at `/` (actual vs. limit, pace against elapsed days, unbudgeted spend, the uncategorised queue, and the Need/Want split). `/budgets` is limit management only. A demo-data seeder exists for development.

**Specified but not yet built:** tags and tag filtering, the CSV import wizard (column mapping, ignore rules, dedupe, result counts), the trailing-12-month category spend trend report, and Docker packaging.

**Domain vocabulary** (use these terms, don't drift to synonyms): Account, Category, Budget, Transaction, Tag, `CsvImportMapping`, `ImportIgnoreRule`, Need/Want. Money is `decimal(18,2)`; transaction dates are `DateOnly`; amounts are signed and sign-normalized at import.

**Technical constraints that bind design:**

- Stack is React 19 + TypeScript (Vite) against an ASP.NET Core Minimal-API backend (REPR pattern, one file per operation) with EF Core over SQLite. The API and the built SPA ship from one process in one container.
- Cookie policy must allow plain HTTP; no HTTPS redirect or HSTS.
- SQLite is single-writer — write conflicts surface as HTTP 409 and the UI must handle that state.
- **Pico.css has been removed.** The original spec framed it as a deliberate classless-framework choice; the user confirmed it was only a way to get pixels on screen. It was replaced on 2026-08-27 by the project's own design system — see [DESIGN.md](DESIGN.md). Fonts must stay self-hosted: the app runs on a LAN with no internet, so a webfont CDN would fail.

**Deliberately out of scope** (do not design toward these): net worth tracking, account balance history, exportable reports, recurring-bill modeling, a first-class Transfer entity (handled by ignore rules), envelope/zero-based budgeting, category hierarchies, predefined starter categories, multi-currency, per-user permissions, and any internet-facing hardening.

## Brand Commitments

**The product is named MoneyRight** (confirmed 2026-08-27, replacing the "Household Budget" working title). The wordmark sets "Money" in ink and "Right" in the signal oxblood.

**Light-only.** Dark mode was considered and declined: the real use scenes are a bright kitchen table and a bright grocery aisle.

The visual system is recorded in [DESIGN.md](DESIGN.md) — the ruled cash book. Voice is plain and factual: controls name their action, errors name the problem and the recovery. No encouragement, no gamification, no cute microcopy.

## Evidence on Hand

- `.scratch/budgeting-app/spec.md` — the authoritative product spec: problem statement, 34 user stories, entity model, architecture and testing decisions, explicit out-of-scope list.
- `.scratch/budgeting-app/issues/01…12-*.md` — the twelve build tickets, in phase order. Tickets 01–06 are complete.
- `src/Api/Data/DemoDataSeeder.cs`, `DemoHousehold.cs` — a demo household with realistic seed data, usable for populated-state design work.
- `AGENTS.md`, `docs/agents/` — working conventions for this repo.
- `client/src/styles/ledger.css` — the ledger component vocabulary established with the status screen.

**No real financial data, screenshots, testimonials, users beyond the two, pricing, or launch plans exist.** This is a private household tool; future work must not invent any of these. The only brand asset is the typeset MoneyRight wordmark — there is no logo or bitmap asset.

## Product Principles

1. **Two devices, two jobs.** The phone answers questions; the desk does the work. Mobile must carry budget status well and is not obligated to carry import, batch categorization, or trend reporting.
2. **Trust is built from counts, not assurances.** Every import reports exactly what it imported, what it skipped as a duplicate, and what an ignore rule caught. Nothing disappears silently.
3. **No transaction escapes judgment.** Category may be pending review; Need/Want may not be absent. The uncategorized queue is a visible, finishable state — not a permanent shrug.
4. **One household picture, with authorship intact.** All data is shared by default; who entered a given row stays answerable.
5. **It has to keep running unattended.** Two users, one small container, no ongoing cost, no internet exposure, no maintenance ritual. Anything that adds operational burden is the wrong answer here.

## Accessibility & Inclusion

No formal standard required and no specific assistive-technology need. Two confirmed preferences that are real, not checkbox items:

- **Readable type and strong contrast are a genuine preference.** Dense transaction and budget tables must stay comfortably legible — this constrains type size and contrast choices in exactly the places where financial UIs usually compress hardest.
- **Small-screen behavior matters for budget status specifically.** Layouts below ~400px must hold up for checking where a category stands; reporting and data-entry surfaces are not held to that bar.
