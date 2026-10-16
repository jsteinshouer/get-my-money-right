import { budgetsApi, type BudgetWithActual } from '../api/budgets'
import { categoriesApi, type Category } from '../api/categories'
import { transactionsApi, type Transaction } from '../api/transactions'

/**
 * Everything the ledger spread reads for one month, derived in one place.
 *
 * Only budgeted categories come back from `/budgets`, so unbudgeted spend and
 * the Need/Want split are computed here from the month's transactions. The
 * review queue is the exception: which transactions have no category is the
 * server's to say, so it is asked for rather than inferred from ids this page
 * happens not to recognise.
 */

export type MonthState = 'past' | 'current' | 'future'

export interface BudgetedEntry {
  categoryId: number
  name: string
  limit: number
  actual: number
  /** The limit pro-rated by elapsed days. Null outside the current month. */
  expectedToDate: number | null
  isOver: boolean
}

export interface UnbudgetedEntry {
  categoryId: number
  name: string
  actual: number
}

export interface MonthSummary {
  year: number
  month: number
  state: MonthState
  /** Days counted so far, and the month's length. Pace comes from these. */
  dayOfMonth: number
  daysInMonth: number
  budgeted: BudgetedEntry[]
  unbudgeted: UnbudgetedEntry[]
  uncategorizedCount: number
  totalLimit: number
  totalActual: number
  /** Actual spend across every category, budgeted or not. */
  totalSpend: number
  needTotal: number
  wantTotal: number
  categoryCount: number
  transactionCount: number
}

export function daysInMonth(year: number, month: number): number {
  return new Date(year, month, 0).getDate()
}

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

export function monthBounds(year: number, month: number): { dateFrom: string; dateTo: string } {
  return {
    dateFrom: `${year}-${pad(month)}-01`,
    dateTo: `${year}-${pad(month)}-${pad(daysInMonth(year, month))}`,
  }
}

export function monthStateFor(year: number, month: number, today = new Date()): MonthState {
  const currentYear = today.getFullYear()
  const currentMonth = today.getMonth() + 1
  if (year === currentYear && month === currentMonth) return 'current'
  return year < currentYear || (year === currentYear && month < currentMonth) ? 'past' : 'future'
}

/** Money out is negative, so a category's spend is the negated net; refunds reduce it. */
function spendOf(transactions: Transaction[]): number {
  return -transactions.reduce((sum, t) => sum + t.amount, 0)
}

export async function fetchMonthSummary(
  year: number,
  month: number,
  today = new Date(),
): Promise<MonthSummary> {
  const { dateFrom, dateTo } = monthBounds(year, month)

  const [budgets, categories, transactions, uncategorized] = await Promise.all([
    budgetsApi.fetchForMonth(year, month),
    categoriesApi.fetchAll(),
    transactionsApi.fetchAll({ dateFrom, dateTo }),
    transactionsApi.fetchAll({ dateFrom, dateTo, uncategorized: true }),
  ])

  return buildMonthSummary({
    year,
    month,
    budgets,
    categories,
    transactions,
    uncategorizedCount: uncategorized.length,
    today,
  })
}

export function buildMonthSummary({
  year,
  month,
  budgets,
  categories,
  transactions,
  uncategorizedCount,
  today = new Date(),
}: {
  year: number
  month: number
  budgets: BudgetWithActual[]
  categories: Category[]
  transactions: Transaction[]
  uncategorizedCount: number
  today?: Date
}): MonthSummary {
  const state = monthStateFor(year, month, today)
  const length = daysInMonth(year, month)
  const dayOfMonth = state === 'current' ? today.getDate() : state === 'past' ? length : 0

  const nameOf = new Map(categories.map((c) => [c.id, c.name]))
  const budgetedIds = new Set(budgets.map((b) => b.categoryId))

  const budgeted: BudgetedEntry[] = budgets
    .map((budget) => ({
      categoryId: budget.categoryId,
      name: nameOf.get(budget.categoryId) ?? 'Unknown category',
      limit: budget.amount,
      actual: budget.actual,
      expectedToDate: state === 'current' ? (budget.amount * dayOfMonth) / length : null,
      isOver: budget.actual > budget.amount,
    }))
    .sort((a, b) => a.name.localeCompare(b.name))

  // Spend in a category nobody set a limit for. Invisible in ticket 06; an entry here.
  const spendByCategory = new Map<number, Transaction[]>()
  for (const transaction of transactions) {
    if (transaction.categoryId === null) continue
    const bucket = spendByCategory.get(transaction.categoryId)
    if (bucket) bucket.push(transaction)
    else spendByCategory.set(transaction.categoryId, [transaction])
  }

  const unbudgeted: UnbudgetedEntry[] = [...spendByCategory.entries()]
    .filter(([categoryId]) => !budgetedIds.has(categoryId) && nameOf.has(categoryId))
    .map(([categoryId, rows]) => ({
      categoryId,
      name: nameOf.get(categoryId) ?? 'Unknown category',
      actual: spendOf(rows),
    }))
    .filter((entry) => entry.actual !== 0)
    .sort((a, b) => b.actual - a.actual)

  const outgoing = transactions.filter((t) => t.amount < 0)

  return {
    year,
    month,
    state,
    dayOfMonth,
    daysInMonth: length,
    budgeted,
    unbudgeted,
    uncategorizedCount,
    totalLimit: budgeted.reduce((sum, entry) => sum + entry.limit, 0),
    totalActual: budgeted.reduce((sum, entry) => sum + entry.actual, 0),
    totalSpend: spendOf(transactions),
    needTotal: spendOf(outgoing.filter((t) => t.needWant === 'Need')),
    wantTotal: spendOf(outgoing.filter((t) => t.needWant === 'Want')),
    categoryCount: categories.length,
    transactionCount: transactions.length,
  }
}
