import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { budgetsApi, type BudgetInput, type BudgetWithActual } from '../api/budgets'
import { categoriesApi, type Category } from '../api/categories'
import { ApiError } from '../api/client'
import { CategorySelect } from '../components/CategorySelect'

function currentMonthValue() {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
}

function parseMonthValue(monthValue: string): { year: number; month: number } {
  const [year, month] = monthValue.split('-').map(Number)
  return { year, month }
}

/**
 * Budget *management*: what the limits are, and changing them. How spending is
 * running against them is the ledger spread's job, on This month.
 */
export function BudgetsPage() {
  const [monthValue, setMonthValue] = useState(currentMonthValue())
  const [budgets, setBudgets] = useState<BudgetWithActual[] | null>(null)
  const [categories, setCategories] = useState<Category[]>([])
  const [error, setError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)

  const [formCategoryId, setFormCategoryId] = useState<number | ''>('')
  const [formAmount, setFormAmount] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    void (async () => {
      try {
        setCategories(await categoriesApi.fetchAll())
      } catch {
        setError('Failed to load categories.')
      }
    })()
  }, [])

  // Switching months fires a fetch while the previous month's is still in flight; only the
  // most recently issued one may write to state, or a slow earlier response wins the race.
  const latestLoad = useRef(0)

  const load = useCallback(async () => {
    const loadId = ++latestLoad.current
    const { year, month } = parseMonthValue(monthValue)
    try {
      const loaded = await budgetsApi.fetchForMonth(year, month)
      if (loadId === latestLoad.current) {
        setBudgets(loaded)
      }
    } catch {
      if (loadId === latestLoad.current) {
        setError('Failed to load budgets.')
      }
    }
  }, [monthValue])

  useEffect(() => {
    void load()
  }, [load])

  function categoryName(categoryId: number) {
    return categories.find((c) => c.id === categoryId)?.name ?? 'Unknown category'
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (formCategoryId === '') {
      return
    }

    setError(null)
    setSaving(true)
    try {
      const { year, month } = parseMonthValue(monthValue)
      const input: BudgetInput = { categoryId: formCategoryId, year, month, amount: Number(formAmount) }
      const existing = budgets?.find((b) => b.categoryId === formCategoryId)
      if (existing) {
        await budgetsApi.update(existing.id, input)
      } else {
        await budgetsApi.create(input)
      }
      setFormCategoryId('')
      setFormAmount('')
      await load()
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('That category already has a budget for this month.')
      } else {
        setError('Failed to save the budget.')
      }
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: number) {
    setError(null)
    try {
      await budgetsApi.delete(id)
      await load()
    } catch {
      setError('Failed to delete the budget.')
    }
  }

  return (
    <>
      <h1>Budgets</h1>
      <p style={{ color: 'var(--ink-2)', marginTop: 'var(--gap-2)' }}>
        The monthly limit for each category. To see how spending is running against these, open{' '}
        <Link to="/">This month</Link>.
      </p>

      {error && <p role="alert">{error}</p>}

      <label htmlFor="budget-month" style={{ maxWidth: '14rem', marginTop: 'var(--gap-5)' }}>
        Month
        <input id="budget-month" type="month" value={monthValue} onChange={(e) => setMonthValue(e.target.value)} />
      </label>

      {budgets === null ? (
        <p aria-busy="true">Loading…</p>
      ) : budgets.length === 0 ? (
        <p>No budgets set for this month yet — add one below.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th scope="col">Category</th>
              <th scope="col" className="money">Monthly limit</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {budgets.map((budget) =>
              editingId === budget.id ? (
                <EditRow
                  key={budget.id}
                  budget={budget}
                  categoryLabel={categoryName(budget.categoryId)}
                  onDone={() => setEditingId(null)}
                  onSaved={load}
                  onError={setError}
                />
              ) : (
                <tr key={budget.id}>
                  <td>{categoryName(budget.categoryId)}</td>
                  <td className="money">{budget.amount.toFixed(2)}</td>
                  <td>
                    <button className="secondary" onClick={() => setEditingId(budget.id)}>
                      Edit
                    </button>{' '}
                    <button className="contrast" onClick={() => void handleDelete(budget.id)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      )}

      <article>
        <h2>Set a category budget</h2>
        <form onSubmit={handleSubmit}>
          <div className="grid">
            <label htmlFor="budget-category">
              Category
              <CategorySelect
                id="budget-category"
                categories={categories}
                value={formCategoryId}
                onChange={setFormCategoryId}
                required
              />
            </label>
            <label htmlFor="budget-amount">
              Monthly limit
              <input
                id="budget-amount"
                type="number"
                step="0.01"
                min="0.01"
                value={formAmount}
                onChange={(e) => setFormAmount(e.target.value)}
                required
              />
            </label>
          </div>
          <button type="submit" aria-busy={saving} disabled={saving}>
            Save budget
          </button>
        </form>
      </article>
    </>
  )
}

function EditRow({
  budget,
  categoryLabel,
  onDone,
  onSaved,
  onError,
}: {
  budget: BudgetWithActual
  categoryLabel: string
  onDone: () => void
  onSaved: () => Promise<void>
  onError: (message: string) => void
}) {
  const [amount, setAmount] = useState(String(budget.amount))
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      const input: BudgetInput = { categoryId: budget.categoryId, year: budget.year, month: budget.month, amount: Number(amount) }
      await budgetsApi.update(budget.id, input)
      onDone()
      await onSaved()
    } catch {
      onError('Failed to save the budget.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <tr>
      <td>{categoryLabel}</td>
      <td className="money">
        <input aria-label="Monthly limit" type="number" step="0.01" min="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
      </td>
      <td>
        <button aria-busy={saving} disabled={saving} onClick={() => void handleSave()}>
          Save
        </button>{' '}
        <button className="secondary" onClick={onDone}>
          Cancel
        </button>
      </td>
    </tr>
  )
}
