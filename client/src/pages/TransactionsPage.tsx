import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { accountsApi, type Account } from '../api/accounts'
import { ApiError } from '../api/client'
import { categoriesApi, type Category } from '../api/categories'
import { tagsApi, type Tag } from '../api/tags'
import { transactionsApi, type NeedWant, type Transaction, type TransactionInput } from '../api/transactions'
import { AccountSelect } from '../components/AccountSelect'
import { CategorySelect } from '../components/CategorySelect'
import { NeedWantSelect } from '../components/NeedWantSelect'
import { TagCombobox } from '../components/TagCombobox'
import { TagLine } from '../components/TagLine'
import { TagSelect } from '../components/TagSelect'

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
  const [editingId, setEditingId] = useState<number | null>(null)
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

  async function handleDelete(id: number) {
    setError(null)
    try {
      await transactionsApi.delete(id)
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
      const already =
        result.alreadyTaggedCount > 0 ? ` ${result.alreadyTaggedCount} already carried it.` : ''
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

  function categoryName(categoryId: number) {
    return categories.find((c) => c.id === categoryId)?.name ?? 'Unknown category'
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

  function toggleAll(checked: boolean) {
    setSelectedIds(checked ? (transactions ?? []).map((t) => t.id) : [])
  }

  return (
    <>
      <h1>Transactions</h1>

      {error && <p role="alert">{error}</p>}
      {notice && <p role="status" className="notice">{notice}</p>}

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
          <p className="selection-count">
            {selectedIds.length} selected
          </p>
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
                    onChange={(e) => toggleAll(e.target.checked)}
                  />
                </th>
                <th scope="col" className="col-date">Date</th>
                <th scope="col" className="col-account">Account</th>
                <th scope="col">Category</th>
                <th scope="col">Description</th>
                <th scope="col" className="money">Amount</th>
                <th scope="col" className="col-needwant">Need/Want</th>
                <th scope="col">Tags</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((transaction) =>
                editingId === transaction.id ? (
                  <EditRow
                    key={transaction.id}
                    transaction={transaction}
                    accounts={accounts}
                    categories={categories}
                    tags={tags}
                    onCreateTag={createTag}
                    onDone={() => setEditingId(null)}
                    onSaved={load}
                    onError={setError}
                  />
                ) : (
                  <tr key={transaction.id} data-selected={selectedIds.includes(transaction.id)}>
                    <td className="col-select">
                      <input
                        type="checkbox"
                        aria-label={`Select ${transaction.description}`}
                        checked={selectedIds.includes(transaction.id)}
                        onChange={(e) =>
                          setSelectedIds(
                            e.target.checked
                              ? [...selectedIds, transaction.id]
                              : selectedIds.filter((id) => id !== transaction.id),
                          )
                        }
                      />
                    </td>
                    <td className="col-date">{transaction.date}</td>
                    <td className="col-account">{accountName(transaction.accountId)}</td>
                    <td>{categoryName(transaction.categoryId)}</td>
                    <td>{transaction.description}</td>
                    <td className="money">{transaction.amount.toFixed(2)}</td>
                    <td className="col-needwant">{transaction.needWant}</td>
                    <td>
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
                      <button className="secondary" onClick={() => setEditingId(transaction.id)}>
                        Edit
                      </button>{' '}
                      <button className="contrast" onClick={() => void handleDelete(transaction.id)}>
                        Delete
                      </button>
                    </td>
                  </tr>
                ),
              )}
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
          <div className="grid">
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
            <label htmlFor="new-transaction-description">
              Description
              <input
                id="new-transaction-description"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
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

function EditRow({
  transaction,
  accounts,
  categories,
  tags,
  onCreateTag,
  onDone,
  onSaved,
  onError,
}: {
  transaction: Transaction
  accounts: Account[]
  categories: Category[]
  tags: Tag[]
  onCreateTag: (name: string) => Promise<Tag | null>
  onDone: () => void
  onSaved: () => Promise<void>
  onError: (message: string) => void
}) {
  const [accountId, setAccountId] = useState<number | ''>(transaction.accountId)
  const [categoryId, setCategoryId] = useState<number | ''>(transaction.categoryId)
  const [date, setDate] = useState(transaction.date)
  const [amount, setAmount] = useState(String(transaction.amount))
  const [description, setDescription] = useState(transaction.description)
  const [needWant, setNeedWant] = useState<NeedWant | ''>(transaction.needWant)
  const [tagIds, setTagIds] = useState<number[]>(transaction.tagIds)
  const [saving, setSaving] = useState(false)
  const [creatingTag, setCreatingTag] = useState(false)
  const originalTagIds = useRef(transaction.tagIds)

  async function handleSave() {
    if (accountId === '' || categoryId === '' || needWant === '') {
      onError('Account, category, and Need/Want are required.')
      return
    }

    setSaving(true)

    try {
      const input: TransactionInput = { accountId, categoryId, date, amount: Number(amount), description, needWant }
      await transactionsApi.update(transaction.id, input)
    } catch (err) {
      // Nothing was persisted, so the row stays open with the edits still in it.
      setSaving(false)
      if (err instanceof ApiError && err.status === 409) {
        onError('That account or category no longer exists.')
      } else {
        onError('Failed to save the transaction.')
      }
      return
    }

    // Tags live behind their own assign/remove endpoints, so only what actually changed is sent.
    // The transaction itself is saved by this point, so a failure here closes the row and reloads
    // anyway: the list then shows which tag changes stuck, rather than leaving the form to retry
    // against a baseline the server has already moved past.
    const original = originalTagIds.current
    try {
      await Promise.all([
        ...tagIds.filter((id) => !original.includes(id)).map((id) => tagsApi.assign(transaction.id, id)),
        ...original.filter((id) => !tagIds.includes(id)).map((id) => tagsApi.remove(transaction.id, id)),
      ])
    } catch {
      onError('The transaction was saved, but its tags could not all be updated.')
    } finally {
      setSaving(false)
      onDone()
      await onSaved()
    }
  }

  return (
    <tr className="is-editing">
      <td className="col-select" />
      <td className="col-date">
        <input aria-label="Date" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </td>
      <td className="col-account">
        <AccountSelect label="Account" accounts={accounts} value={accountId} onChange={setAccountId} required />
      </td>
      <td>
        <CategorySelect label="Category" categories={categories} value={categoryId} onChange={setCategoryId} required />
      </td>
      <td>
        <input aria-label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
      </td>
      <td className="money">
        <input aria-label="Amount" type="number" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} />
      </td>
      <td className="col-needwant">
        <NeedWantSelect label="Need/Want" value={needWant} onChange={setNeedWant} required />
      </td>
      <td>
        <TagLine
          tags={tags}
          selectedIds={tagIds}
          onChange={setTagIds}
          onCreate={onCreateTag}
          onBusyChange={setCreatingTag}
        />
      </td>
      <td>
        <button aria-busy={saving || creatingTag} disabled={saving || creatingTag} onClick={() => void handleSave()}>
          Save
        </button>{' '}
        <button className="secondary" onClick={onDone}>
          Cancel
        </button>
      </td>
    </tr>
  )
}
