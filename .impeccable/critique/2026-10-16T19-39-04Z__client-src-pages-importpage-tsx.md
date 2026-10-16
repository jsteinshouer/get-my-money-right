---
target: client/src/pages/ImportPage.tsx
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 3
target_identity: "file:/home/jsteinshouer/projects/budget/client/src/pages/ImportPage.tsx"
target_fingerprint: "sha256:fa6ae71186f644f5e4dd088341951d822f277cc0a6306884e6c2d4c6bbf58fdd"
target_path: /home/jsteinshouer/projects/budget/client/src/pages/ImportPage.tsx
timestamp: 2026-10-16T19-39-04Z
slug: client-src-pages-importpage-tsx
closed: true
---
Method: dual-agent (A: design review · B: detector + browser), both in isolated sub-agents.

# Critique — `/import` (ImportPage.tsx), focus on ticket 10's close

## Design Health Score
| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 3 | After import the station band still marks "Preview & confirm" current; reading head says "will be read" |
| 2 | Match System / Real World | 3 | "For review" labels rows about to be written; Want default never stated |
| 3 | User Control and Freedom | 2 | No undo of an import; over-broad rule fixed elsewhere |
| 4 | Consistency and Standards | 3 | Result box narrower (680px) than the tally it replaces |
| 5 | Error Prevention | 3 | Import disabled on unreadable rows; button names its count |
| 6 | Recognition Rather Than Recall | 3 | Rule reason inline; mapping remembered |
| 7 | Flexibility and Efficiency | 2 | Must scroll every duplicate in an overlapping re-export |
| 8 | Aesthetic and Minimalist Design | 3 | N identical "Ignore rows like this" buttons; result prints zeros |
| 9 | Error Recovery | 3 | Errors name the fix; one path can surface raw error.message |
| 10 | Help and Documentation | 2 | Duplicate criteria and Want default unexplained |
| **Total** | | **27/40** | **Acceptable, near Good** |

## Design Specificity Verdict
Authored for this product (strike + reason, unstruck "Already imported", double rule, count-naming button). Weak spot: the .exceptions tally is a bordered card whose figures (x≈1170) miss the money column (x≈989).
Detector: source files clean (0). Browser: all-caps-body on .reading-head (agrees with LLM — sentence + filename in caps, wraps at 408px); em-dash-overuse (mostly the "— not used —" option placeholder, low value); monotonous-spacing (false positive: --gap-1 scale); layout-transition (false positive: sanctioned gauge-fill, not on this page). Headless run — no user-visible overlay.

## Priority Issues
- [P1] Want default invisible — add one line under the tally: "Each lands with no category, as a Want, until someone files it." (/impeccable clarify)
- [P1] Ending weaker than the work — full-width .import-result; station 3 done + past-tense head; review-queue link as primary action. (/impeccable polish)
- [P1] Focus lost to <body> after Read the file / Preview all rows / Add rule / Import — move focus to new station head / struck row / result; aria-current="step"; name the row in each rule button. (/impeccable harden)
- [P2] Station 3 rows break at ≤46rem — mobile `.preview-line > * { grid-column: 1 }` (0,1,0) loses to desktop per-child rules (0,2,0); predates ticket 10. (/impeccable adapt)
- [P2] Station 2 remembered-mapping path: "Save mapping" primary though "Preview all rows" is the next step. (/impeccable clarify)

## Persona Red Flags
- Alex: scrolls past every duplicate; counts only at the foot; no undo.
- Sam: focus drops 4×; identical button names; divs not a list/table; live tally re-announces all counts; "✓" glyph read aloud.
- Jordan: "For review" doesn't say rows will be written; "Already imported" criteria unexplained; Save vs Preview choice unexplained.

## Minor Observations
- Reading head capped at 408px wraps on desktop.
- --signal-soft mapper column rules barely visible.
- Slip heading in oxblood spends signal on a label.
- "Imported 1" above "4 transactions are waiting" needs "on this account".
- Result line prints zero counts.

## Questions to Consider
- Should the tally sit at the top / stay pinned, with the double rule at the foot?
- Could 90%-duplicate re-exports fold duplicates into one ruled line?
- What does the household do when an import lands on the wrong account?
