import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  fetchMonthSummary,
  monthBounds,
  type BudgetedEntry,
  type MonthSummary,
} from '../lib/monthSummary'

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

function money(value: number): string {
  return value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function percent(part: number, whole: number): string {
  if (whole <= 0) return '0%'
  return `${Math.min(Math.max((part / whole) * 100, 0), 100).toFixed(2)}%`
}

/** "155.35 left" under the limit, "25.60 over" past it — the ledger's own words. */
function relationOf(entry: BudgetedEntry): string {
  const remaining = entry.limit - entry.actual
  return remaining < 0 ? `${money(Math.abs(remaining))} over` : `${money(remaining)} left`
}

/** Against the pro-rated limit, not the whole limit: the current month's real question. */
function paceOf(entry: BudgetedEntry): string | null {
  if (entry.expectedToDate === null) return null
  if (entry.isOver) return null
  const difference = entry.expectedToDate - entry.actual
  if (Math.abs(difference) < 0.005) return 'on plan'
  return difference > 0 ? `${money(difference)} under pace` : `${money(-difference)} ahead of pace`
}

function transactionsHref(year: number, month: number, categoryId?: number): string {
  const { dateFrom, dateTo } = monthBounds(year, month)
  const params = new URLSearchParams({ dateFrom, dateTo })
  if (categoryId !== undefined) params.set('categoryId', String(categoryId))
  return `/transactions?${params.toString()}`
}

export function StatusPage() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const [summary, setSummary] = useState<MonthSummary | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Paging months fires a request while the previous one is still in flight;
  // only the most recently issued may write state, or a slow earlier response wins.
  const latestLoad = useRef(0)

  const load = useCallback(async () => {
    const loadId = ++latestLoad.current
    try {
      const loaded = await fetchMonthSummary(year, month)
      if (loadId === latestLoad.current) {
        setSummary(loaded)
        setError(null)
      }
    } catch {
      if (loadId === latestLoad.current) {
        setError('Could not load this month. Check that the API is running, then try again.')
      }
    }
  }, [year, month])

  useEffect(() => {
    void load()
  }, [load])

  function step(delta: number) {
    const next = new Date(year, month - 1 + delta, 1)
    setYear(next.getFullYear())
    setMonth(next.getMonth() + 1)
  }

  const previous = new Date(year, month - 2, 1)
  const next = new Date(year, month, 1)

  return (
    <div className="spread">
      <div className="spread-head">
        <div>
          <div className="spread-month">
            {MONTHS[month - 1]} {year}
          </div>
          {summary && (
            <div className="spread-elapsed">
              {summary.state === 'current'
                ? `Day ${summary.dayOfMonth} of ${summary.daysInMonth}`
                : summary.state === 'past'
                  ? 'Month closed'
                  : 'Not started'}
            </div>
          )}
        </div>
        <nav className="spread-nav" aria-label="Month">
          <button className="secondary" onClick={() => step(-1)}>
            {MONTHS[previous.getMonth()].slice(0, 3)} {previous.getFullYear()}
          </button>
          <button className="secondary" onClick={() => step(1)}>
            {MONTHS[next.getMonth()].slice(0, 3)} {next.getFullYear()}
          </button>
        </nav>
      </div>

      {error && <p role="alert">{error}</p>}

      {summary === null ? (
        <p aria-busy="true">Loading…</p>
      ) : (
        <MonthSpread summary={summary} onRetry={load} />
      )}
    </div>
  )
}

function MonthSpread({ summary, onRetry }: { summary: MonthSummary; onRetry: () => Promise<void> }) {
  const {
    year, month, state, budgeted, unbudgeted, uncategorizedCount,
    totalLimit, totalActual, totalSpend, needTotal, wantTotal,
    categoryCount, transactionCount,
  } = summary

  const outgoing = needTotal + wantTotal
  const overCount = budgeted.filter((entry) => entry.isOver).length

  if (categoryCount === 0) {
    return (
      <div className="spread-body" key={`${year}-${month}`}>
        <FirstRun />
      </div>
    )
  }

  return (
    <div className="spread-body" key={`${year}-${month}`}>
      <div className="headline">
        <div>
          <div className="headline-figure num">{money(totalSpend)}</div>
          <div className="headline-close" />
          <p className="headline-of">
            {totalLimit > 0 ? (
              <>
                spent this month, against <strong>{money(totalLimit)}</strong> budgeted
                {budgeted.length > 0 && <> across {budgeted.length} categories</>}.
              </>
            ) : (
              <>spent this month. No budgets are set, so there is nothing to measure it against yet.</>
            )}
          </p>
        </div>

        <div className="exceptions">
          <Link
            className="exception"
            to={transactionsHref(year, month)}
            data-flagged={uncategorizedCount > 0}
          >
            <span>
              <span className="exception-label">Awaiting a category</span>
              <span className="exception-note" style={{ display: 'block' }}>
                {uncategorizedCount > 0
                  ? 'Review these before trusting the figures'
                  : `All ${transactionCount} transactions categorised`}
              </span>
            </span>
            <span className="exception-count num">{uncategorizedCount}</span>
          </Link>

          <div className="exception" data-flagged={unbudgeted.length > 0}>
            <span>
              <span className="exception-label">Spent with no budget</span>
              <span className="exception-note" style={{ display: 'block' }}>
                {unbudgeted.length > 0
                  ? `${money(unbudgeted.reduce((sum, e) => sum + e.actual, 0))} outside any limit`
                  : 'Every category that spent has a limit'}
              </span>
            </span>
            <span className="exception-count num">{unbudgeted.length}</span>
          </div>

          <div className="exception" data-flagged={overCount > 0}>
            <span>
              <span className="exception-label">Over its limit</span>
              <span className="exception-note" style={{ display: 'block' }}>
                {overCount > 0 ? 'Named in the margin below' : 'Nothing has passed its limit'}
              </span>
            </span>
            <span className="exception-count num">{overCount}</span>
          </div>
        </div>
      </div>

      <section>
        <div className="section-head">
          <h2>Against budget</h2>
          {totalLimit > 0 && (
            <span className="spread-elapsed num">
              {money(totalActual)} of {money(totalLimit)}
            </span>
          )}
        </div>

        {budgeted.length === 0 ? (
          <div className="empty">
            <h3>No budgets set for this month</h3>
            <p>
              Set a monthly limit for a category and this page will start measuring spending
              against it — and, during the current month, against how far into the month you are.
            </p>
            <Link className="button" to="/budgets">
              Set a budget
            </Link>
          </div>
        ) : (
          <div className="entries">
            <div className="entry-columns colhead">
              <span>Category</span>
              <span>Limit</span>
              {state === 'current' && <span>Expected to date</span>}
              <span>Actual</span>
            </div>
            {budgeted.map((entry) => (
              <BudgetedRow key={entry.categoryId} entry={entry} year={year} month={month} state={state} />
            ))}
          </div>
        )}
      </section>

      {unbudgeted.length > 0 && (
        <section>
          <div className="section-head">
            <h2>No budget set</h2>
            <span className="spread-elapsed">Spending with nothing to measure it against</span>
          </div>
          <div className="entries is-unbudgeted">
            <div className="entry-columns colhead">
              <span>Category</span>
              <span>Actual</span>
            </div>
            {unbudgeted.map((entry) => (
              <Link
                className="entry"
                key={entry.categoryId}
                to={transactionsHref(year, month, entry.categoryId)}
              >
                <div className="entry-line">
                  <div className="entry-name">{entry.name}</div>
                  <div className="entry-figure is-actual num">
                    {money(entry.actual)}
                    <span className="entry-mark">No limit</span>
                  </div>
                </div>
                <div className="gauge" />
              </Link>
            ))}
          </div>
          <p className="memo">
            These categories spent money this month with no limit to compare against.{' '}
            <Link to="/budgets">Give them a budget</Link> and they move into the section above.
          </p>
        </section>
      )}

      <section>
        <div className="section-head">
          <h2>Need and want</h2>
          <span className="spread-elapsed">Every transaction carries one</span>
        </div>
        {outgoing <= 0 ? (
          <p className="memo">No money went out this month, so there is nothing to split yet.</p>
        ) : (
          <div className="needwant">
            <div
              className="needwant-bar"
              role="img"
              aria-label={`Need ${money(needTotal)}, want ${money(wantTotal)}, of ${money(outgoing)} spent`}
            >
              <div className="needwant-need" style={{ width: percent(needTotal, outgoing) }} />
              <div className="needwant-want" style={{ width: percent(wantTotal, outgoing) }} />
            </div>
            <div className="needwant-key">
              <div>
                Need <strong className="num">{money(needTotal)}</strong>{' '}
                <span className="num">({percent(needTotal, outgoing)})</span>
              </div>
              <div>
                Want <strong className="num">{money(wantTotal)}</strong>{' '}
                <span className="num">({percent(wantTotal, outgoing)})</span>
              </div>
            </div>
          </div>
        )}
      </section>

      {uncategorizedCount > 0 && (
        <p className="memo">
          <strong>{uncategorizedCount}</strong>{' '}
          {uncategorizedCount === 1 ? 'transaction is' : 'transactions are'} still waiting for a
          category, so the figures above are incomplete.{' '}
          <Link to={transactionsHref(year, month)}>Review them</Link>.
        </p>
      )}

      <p className="memo" hidden={transactionCount > 0}>
        Nothing has been recorded for this month yet.{' '}
        <Link to="/transactions">Add a transaction</Link>, or{' '}
        <button className="secondary" onClick={() => void onRetry()} style={{ verticalAlign: 'baseline' }}>
          Reload
        </button>
      </p>
    </div>
  )
}

function BudgetedRow({
  entry,
  year,
  month,
  state,
}: {
  entry: BudgetedEntry
  year: number
  month: number
  state: MonthSummary['state']
}) {
  const pace = paceOf(entry)
  const relation = relationOf(entry)

  return (
    <Link className="entry" to={transactionsHref(year, month, entry.categoryId)}>
      <div className="entry-line">
        <div className="entry-name">{entry.name}</div>
        <div className="entry-figure num">{money(entry.limit)}</div>
        {state === 'current' && (
          <div className="entry-figure num">
            {entry.expectedToDate === null ? '—' : money(entry.expectedToDate)}
          </div>
        )}
        <div className="entry-figure is-actual num">{money(entry.actual)}</div>
      </div>
      <div className="entry-relation" data-over={entry.isOver}>
        <span className="entry-remaining num">{relation}</span>
        {pace && <span className="entry-pace num"> · {pace}</span>}
      </div>
      <div
        className="gauge"
        data-over={entry.isOver}
        style={{ ['--spent' as string]: percent(entry.actual, entry.limit) }}
      >
        <span className="gauge-fill" />
        {entry.expectedToDate !== null && (
          <span
            className="gauge-tick"
            style={{ ['--expected' as string]: percent(entry.expectedToDate, entry.limit) }}
          />
        )}
      </div>
    </Link>
  )
}

function FirstRun() {
  return (
    <div className="empty">
      <h3>Nothing to show yet</h3>
      <p>
        MoneyRight measures what the household spent against what it planned to spend. It needs
        three things before this page says anything useful: an account to hold transactions,
        categories to sort them into, and a monthly limit per category.
      </p>
      <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
        <Link className="button" to="/accounts">
          Add an account
        </Link>
        <Link className="button secondary" to="/categories">
          Add categories
        </Link>
      </div>
    </div>
  )
}
