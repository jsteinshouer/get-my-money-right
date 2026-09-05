import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { accountsApi, type Account } from '../api/accounts'
import { ApiError } from '../api/client'
import { categoriesApi, type Category } from '../api/categories'
import { tagsApi, type Tag } from '../api/tags'
import { transactionsApi, type NeedWant, type Transaction, type TransactionInput } from '../api/transactions'
import { AccountSelect } from '../components/AccountSelect'
import { CategorySelect } from '../components/CategorySelect'
import { CorrectionSlip, type Correction } from '../components/CorrectionSlip'
import { NeedWantSelect } from '../components/NeedWantSelect'
import { TagCombobox } from '../components/TagCombobox'
import { TagSelect } from '../components/TagSelect'

const COLUMN_COUNT = 9

const emptyForm = {
  accountId: '' as number | '',
  categoryId: '' as number | '',
  date: new Date().toISOString().slice(0, 10),
  amount: '',
  description: '',
  needWant: '' as NeedWant | '',
}

export function TransactionsPage() {
  const [transactions, setTransactions] = useState<Transaction[] | null>(null)
  const [accounts, setAccounts] = useState<Account[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [tags, setTags] = useState<Tag[]>([])
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [correctingId, setCorrectingId] = useState<number | null>(null)
  const [correctionDirty, setCorrectionDirty] = useState(false)
  const [savingCorrection, setSavingCorrection] = useState(false)
  const [pendingCorrectId, setPendingCorrectId] = useState<number | null>(null)
  const [deleting, setDeleting] = useState<Transaction | null>(null)
  const [busyRowId, setBusyRowId] = useState<number | null>(null)
  const [selectedIds, setSelectedIds] = useState<number[]>([])
  const [applying, setApplying] = useState(false)

  // The ledger spread links here with a category and month already chosen, so the
  // filters start from the URL rather than empty.
  const [searchParams] = useSearchParams()
  const numberParam = (key: string): number | '' => {
    const raw = Number(searchParams.get(key))
    return Number.isInteger(raw) && raw > 0 ? raw : ''
  }

  const [filterAccountId, setFilterAccountId] = useState<number | ''>(() => numberParam('accountId'))
  const [filterCategoryId, setFilterCategoryId] = useState<number | ''>(() => numberParam('categoryId'))
  const [filterDateFrom, setFilterDateFrom] = useState(() => searchParams.get('dateFrom') ?? '')
  const [filterDateTo, setFilterDateTo] = useState(() => searchParams.get('dateTo') ?? '')
  const [filterNeedWant, setFilterNeedWant] = useState<NeedWant | ''>(() => {
    const raw = searchParams.get('needWant')
    return raw === 'Need' || raw === 'Want' ? raw : ''
  })
  const [filterTagId, setFilterTagId] = useState<number | ''>(() => numberParam('tagId'))

  const [form, setForm] = useState(emptyForm)
  const [adding, setAdding] = useState(false)

  const loadTags = useCallback(async () => {
    setTags(await tagsApi.fetchAll())
  }, [])

  useEffect(() => {
    void (async () => {
      try {
        const [accountList, categoryList] = await Promise.all([accountsApi.fetchAll(true), categoriesApi.fetchAll()])
        setAccounts(accountList)
        setCategories(categoryList)
        await loadTags()
      } catch {
        setError('Failed to load accounts, categories or tags.')
      }
    })()
  }, [loadTags])

  const load = useCallback(async () => {
    try {
      setTransactions(
        await transactionsApi.fetchAll({
          accountId: filterAccountId || undefined,
          categoryId: filterCategoryId || undefined,
          dateFrom: filterDateFrom || undefined,
          dateTo: filterDateTo || undefined,
          needWant: filterNeedWant || undefined,
          tagId: filterTagId || undefined,
        }),
      )
    } catch {
      setError('Failed to load transactions.')
    }
  }, [filterAccountId, filterCategoryId, filterDateFrom, filterDateTo, filterNeedWant, filterTagId])

  useEffect(() => {
    void load()
  }, [load])

  // A selection only means anything against the list it was made on, so changing the filters drops it.
  useEffect(() => {
    setSelectedIds([])
  }, [filterAccountId, filterCategoryId, filterDateFrom, filterDateTo, filterNeedWant, filterTagId])

  async function createTag(name: string): Promise<Tag | null> {
    setError(null)
    try {
      const created = await tagsApi.create(name)
      await loadTags()
      return created
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? `A tag called “${name}” already exists.`
          : 'Failed to create the tag.',
      )
      return null
    }
  }

  /** The queue job: reclassify one field on an entry without entering a mode. */
  async function reclassify(transaction: Transaction, change: Partial<TransactionInput>) {
    setError(null)
    setBusyRowId(transaction.id)
    try {
      await transactionsApi.update(transaction.id, {
        accountId: transaction.accountId,
        categoryId: transaction.categoryId,
        date: transaction.date,
        amount: transaction.amount,
        description: transaction.description,
        needWant: transaction.needWant,
        ...change,
      })
      await load()
    } catch {
      setError('Failed to save that change.')
    } finally {
      setBusyRowId(null)
    }
  }

  function requestCorrection(transactionId: number) {
    if (correctingId !== null && correctingId !== transactionId && correctionDirty) {
      // Unsaved corrections used to vanish without a word when another entry was opened.
      setPendingCorrectId(transactionId)
      return
    }
    setCorrectingId(transactionId)
  }

  async function saveCorrection(transaction: Transaction, correction: Correction) {
    setError(null)
    setSavingCorrection(true)
    try {
      await transactionsApi.update(transaction.id, correction.input)
    } catch (err) {
      setSavingCorrection(false)
      setError(
        err instanceof ApiError && err.status === 409
          ? 'That account or category no longer exists.'
          : 'Failed to save the correction.',
      )
      return
    }

    // Tags live behind their own assign/remove endpoints, so only what actually changed is sent.
    // The entry itself is saved by this point, so a failure here still closes the slip and
    // reloads: the ledger then shows which tag changes stuck.
    try {
      await Promise.all([
        ...correction.tagIds
          .filter((id) => !transaction.tagIds.includes(id))
          .map((id) => tagsApi.assign(transaction.id, id)),
        ...transaction.tagIds
          .filter((id) => !correction.tagIds.includes(id))
          .map((id) => tagsApi.remove(transaction.id, id)),
      ])
    } catch {
      setError('The entry was corrected, but its tags could not all be updated.')
    } finally {
      setSavingCorrection(false)
      setCorrectingId(null)
      setCorrectionDirty(false)
      await Promise.all([load(), loadTags()])
    }
  }

  async function handleAdd(event: FormEvent) {
    event.preventDefault()
    if (form.accountId === '' || form.categoryId === '' || form.needWant === '') {
      return
    }

    setError(null)
    setAdding(true)
    try {
      await transactionsApi.create({
        accountId: form.accountId,
        categoryId: form.categoryId,
        date: form.date,
        amount: Number(form.amount),
        description: form.description,
        needWant: form.needWant,
      })
      setForm(emptyForm)
      await load()
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('That account or category no longer exists.')
      } else {
        setError('Failed to add the transaction.')
      }
    } finally {
      setAdding(false)
    }
  }

  async function confirmDelete(transaction: Transaction) {
    setError(null)
    setNotice(null)
    setDeleting(null)
    try {
      await transactionsApi.delete(transaction.id)
      setNotice(`Deleted “${transaction.description}” of ${transaction.amount.toFixed(2)} on ${transaction.date}.`)
      if (correctingId === transaction.id) setCorrectingId(null)
      await load()
    } catch {
      setError('Failed to delete the transaction.')
    }
  }

  async function applyTagToSelection(tag: Tag) {
    setError(null)
    setNotice(null)
    setApplying(true)
    try {
      const result = await tagsApi.assignToMany(tag.id, selectedIds)
      // Counted, never silent: the household is told what the batch changed and what it left.
      const already = result.alreadyTaggedCount > 0 ? ` ${result.alreadyTaggedCount} already carried it.` : ''
      setNotice(
        `Tagged ${result.assignedCount} ${result.assignedCount === 1 ? 'transaction' : 'transactions'} “${tag.name}”.${already}`,
      )
      setSelectedIds([])
      await Promise.all([load(), loadTags()])
    } catch {
      setError('Failed to tag the selected transactions.')
    } finally {
      setApplying(false)
    }
  }

  function accountName(accountId: number) {
    return accounts.find((a) => a.id === accountId)?.name ?? 'Unknown account'
  }

  function tagNames(tagIds: number[]) {
    return tagIds
      .map((tagId) => tags.find((t) => t.id === tagId)?.name)
      .filter((name): name is string => name !== undefined)
  }

  const total = useMemo(
    () => (transactions ?? []).reduce((sum, transaction) => sum + transaction.amount, 0),
    [transactions],
  )

  const allSelected = transactions !== null && transactions.length > 0 && selectedIds.length === transactions.length
  const pendingCorrectTarget = transactions?.find((t) => t.id === pendingCorrectId) ?? null

  return (
    <>
      <h1>Transactions</h1>

      {error && <p role="alert">{error}</p>}
      {notice && <p role="status" className="notice">{notice}</p>}

      {deleting !== null && (
        <div className="confirm" role="alertdialog" aria-labelledby="confirm-delete-transaction">
          <p id="confirm-delete-transaction">
            Delete “{deleting.description}” — {deleting.amount.toFixed(2)} on {deleting.date}? This removes the entry
            from the ledger for good.
          </p>
          <div className="confirm-actions">
            <button className="contrast" onClick={() => void confirmDelete(deleting)}>
              Delete entry
            </button>
            <button className="secondary" onClick={() => setDeleting(null)}>
              Keep it
            </button>
          </div>
        </div>
      )}

      {pendingCorrectTarget !== null && (
        <div className="confirm" role="alertdialog" aria-labelledby="confirm-discard-correction">
          <p id="confirm-discard-correction">
            You have an unsaved correction open. Discard it and correct “{pendingCorrectTarget.description}” instead?
          </p>
          <div className="confirm-actions">
            <button
              className="contrast"
              onClick={() => {
                setCorrectingId(pendingCorrectTarget.id)
                setCorrectionDirty(false)
                setPendingCorrectId(null)
              }}
            >
              Discard and move on
            </button>
            <button className="secondary" onClick={() => setPendingCorrectId(null)}>
              Keep correcting
            </button>
          </div>
        </div>
      )}

      <article>
        <h2>Filters</h2>
        <div className="grid">
          <label htmlFor="filter-account">
            Filter by account
            <AccountSelect
              id="filter-account"
              accounts={accounts}
              value={filterAccountId}
              onChange={setFilterAccountId}
              includeAllOption
            />
          </label>
          <label htmlFor="filter-category">
            Filter by category
            <CategorySelect
              id="filter-category"
              categories={categories}
              value={filterCategoryId}
              onChange={setFilterCategoryId}
              includeAllOption
            />
          </label>
          <label htmlFor="filter-need-want">
            Filter by Need/Want
            <NeedWantSelect id="filter-need-want" value={filterNeedWant} onChange={setFilterNeedWant} includeAllOption />
          </label>
        </div>
        <div className="grid">
          <label htmlFor="filter-tag">
            Filter by tag
            <TagSelect id="filter-tag" tags={tags} value={filterTagId} onChange={setFilterTagId} />
          </label>
          <label htmlFor="filter-date-from">
            From
            <input
              id="filter-date-from"
              type="date"
              value={filterDateFrom}
              onChange={(e) => setFilterDateFrom(e.target.value)}
            />
          </label>
          <label htmlFor="filter-date-to">
            To
            <input id="filter-date-to" type="date" value={filterDateTo} onChange={(e) => setFilterDateTo(e.target.value)} />
          </label>
        </div>
      </article>

      {selectedIds.length > 0 && (
        <div className="selection-bar" role="region" aria-label="Tag the selected transactions">
          <p className="selection-count">{selectedIds.length} selected</p>
          <TagCombobox
            label="Tag the selected transactions"
            placeholder="Write a tag to apply…"
            tags={tags}
            disabled={applying}
            onPick={(tag) => void applyTagToSelection(tag)}
            onCreate={createTag}
          />
          <button className="secondary" onClick={() => setSelectedIds([])}>
            Clear selection
          </button>
        </div>
      )}

      {transactions === null ? (
        <p aria-busy="true">Loading…</p>
      ) : transactions.length === 0 ? (
        <p>No transactions match the current filters.</p>
      ) : (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th scope="col" className="col-select">
                  <input
                    type="checkbox"
                    aria-label="Select all transactions"
                    checked={allSelected}
                    onChange={(e) => setSelectedIds(e.target.checked ? transactions.map((t) => t.id) : [])}
                  />
                </th>
                <th scope="col" className="col-date">Date</th>
                <th scope="col" className="col-account">Account</th>
                <th scope="col">Category</th>
                <th scope="col">Description</th>
                <th scope="col" className="money">Amount</th>
                <th scope="col" className="col-needwant">Need/Want</th>
                <th scope="col" className="col-tags">Tags</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((transaction) => (
                <Entry
                  key={transaction.id}
                  transaction={transaction}
                  accounts={accounts}
                  categories={categories}
                  tags={tags}
                  accountName={accountName}
                  tagNames={tagNames}
                  busy={busyRowId === transaction.id}
                  correcting={correctingId === transaction.id}
                  selected={selectedIds.includes(transaction.id)}
                  savingCorrection={savingCorrection}
                  onSelect={(checked) =>
                    setSelectedIds(
                      checked
                        ? [...selectedIds, transaction.id]
                        : selectedIds.filter((id) => id !== transaction.id),
                    )
                  }
                  onReclassify={reclassify}
                  onCorrect={() => requestCorrection(transaction.id)}
                  onCancelCorrection={() => {
                    setCorrectingId(null)
                    setCorrectionDirty(false)
                  }}
                  onDirtyChange={setCorrectionDirty}
                  onSaveCorrection={(correction) => void saveCorrection(transaction, correction)}
                  onCreateTag={createTag}
                  onDelete={() => setDeleting(transaction)}
                />
              ))}
            </tbody>
            <tfoot>
              <tr className="ledger-close">
                <td className="col-select" />
                <td colSpan={4}>
                  {transactions.length} {transactions.length === 1 ? 'entry' : 'entries'} shown
                </td>
                <td className="money">{total.toFixed(2)}</td>
                <td className="col-needwant" />
                <td />
                <td />
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      <article>
        <h2>Add a transaction</h2>
        <form onSubmit={handleAdd}>
          {/* Ledger order, the same order the correction slip uses. */}
          <div className="grid">
            <label htmlFor="new-transaction-date">
              Date
              <input
                id="new-transaction-date"
                type="date"
                value={form.date}
                onChange={(e) => setForm({ ...form, date: e.target.value })}
                required
              />
            </label>
            <label htmlFor="new-transaction-account">
              Account
              <AccountSelect
                id="new-transaction-account"
                accounts={accounts.filter((a) => a.isActive)}
                value={form.accountId}
                onChange={(accountId) => setForm({ ...form, accountId })}
                required
              />
            </label>
            <label htmlFor="new-transaction-category">
              Category
              <CategorySelect
                id="new-transaction-category"
                categories={categories}
                value={form.categoryId}
                onChange={(categoryId) => setForm({ ...form, categoryId })}
                required
              />
            </label>
          </div>
          <div className="grid">
            <label htmlFor="new-transaction-description">
              Description
              <input
                id="new-transaction-description"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                required
              />
            </label>
            <label htmlFor="new-transaction-amount">
              Amount
              <input
                id="new-transaction-amount"
                type="number"
                step="0.01"
                value={form.amount}
                onChange={(e) => setForm({ ...form, amount: e.target.value })}
                required
              />
              <small>Negative for money out, positive for money in.</small>
            </label>
            <label htmlFor="new-transaction-need-want">
              Need/Want
              <NeedWantSelect
                id="new-transaction-need-want"
                value={form.needWant}
                onChange={(needWant) => setForm({ ...form, needWant })}
                required
              />
            </label>
          </div>
          <button type="submit" aria-busy={adding} disabled={adding}>
            Add transaction
          </button>
        </form>
      </article>
    </>
  )
}

function Entry({
  transaction,
  accounts,
  categories,
  tags,
  accountName,
  tagNames,
  busy,
  correcting,
  selected,
  savingCorrection,
  onSelect,
  onReclassify,
  onCorrect,
  onCancelCorrection,
  onDirtyChange,
  onSaveCorrection,
  onCreateTag,
  onDelete,
}: {
  transaction: Transaction
  accounts: Account[]
  categories: Category[]
  tags: Tag[]
  accountName: (accountId: number) => string
  tagNames: (tagIds: number[]) => string[]
  busy: boolean
  correcting: boolean
  selected: boolean
  savingCorrection: boolean
  onSelect: (checked: boolean) => void
  onReclassify: (transaction: Transaction, change: Partial<TransactionInput>) => void
  onCorrect: () => void
  onCancelCorrection: () => void
  onDirtyChange: (dirty: boolean) => void
  onSaveCorrection: (correction: Correction) => void
  onCreateTag: (name: string) => Promise<Tag | null>
  onDelete: () => void
}) {
  return (
    <>
      <tr data-selected={selected} data-correcting={correcting} aria-busy={busy}>
        <td className="col-select">
          <input
            type="checkbox"
            aria-label={`Select ${transaction.description}`}
            checked={selected}
            onChange={(e) => onSelect(e.target.checked)}
          />
        </td>
        <td className="col-date">{transaction.date}</td>
        <td className="col-account">{accountName(transaction.accountId)}</td>
        {/* Category and Need/Want carry the queue: an imported entry arrives with everything
            else already right, so these two change in place rather than through a form. */}
        <td>
          <CategorySelect
            label={`Category for ${transaction.description}`}
            categories={categories}
            value={transaction.categoryId}
            onChange={(categoryId) => categoryId !== '' && onReclassify(transaction, { categoryId })}
            required
          />
        </td>
        <td>{transaction.description}</td>
        <td className="money">{transaction.amount.toFixed(2)}</td>
        <td className="col-needwant">
          <NeedWantSelect
            label={`Need or Want for ${transaction.description}`}
            value={transaction.needWant}
            onChange={(needWant) => needWant !== '' && onReclassify(transaction, { needWant })}
            required
          />
        </td>
        <td className="col-tags">
          {transaction.tagIds.length === 0 ? (
            <span className="tag-none">—</span>
          ) : (
            <ul className="tag-marks">
              {tagNames(transaction.tagIds).map((name) => (
                <li key={name}>
                  <span className="tag-mark">{name}</span>
                </li>
              ))}
            </ul>
          )}
        </td>
        <td>
          <button className="secondary" aria-expanded={correcting} onClick={onCorrect}>
            Correct
          </button>{' '}
          <button className="contrast" onClick={onDelete}>
            Delete
          </button>
        </td>
      </tr>

      {correcting && (
        <CorrectionSlip
          transaction={transaction}
          accounts={accounts}
          categories={categories}
          tags={tags}
          columnCount={COLUMN_COUNT}
          saving={savingCorrection}
          onCreateTag={onCreateTag}
          onDirtyChange={onDirtyChange}
          onCancel={onCancelCorrection}
          onSave={onSaveCorrection}
        />
      )}
    </>
  )
}
