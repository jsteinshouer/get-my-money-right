import { useEffect, useRef, useState, type FormEvent } from 'react'
import type { Account } from '../api/accounts'
import type { Category } from '../api/categories'
import type { Tag } from '../api/tags'
import type { NeedWant, Transaction, TransactionInput } from '../api/transactions'
import { AccountSelect } from './AccountSelect'
import { CategorySelect } from './CategorySelect'
import { NeedWantSelect } from './NeedWantSelect'
import { TagLine } from './TagLine'

export interface Correction {
  input: TransactionInput
  tagIds: number[]
}

/**
 * What a cash book does with a wrong entry: the entry stays on its line, and the correction is
 * written on a slip beneath it. A real form, so Enter commits and the browser enforces every
 * required field — which a row of loose inputs never could.
 */
export function CorrectionSlip({
  transaction,
  accounts,
  categories,
  tags,
  columnCount,
  saving,
  onCreateTag,
  onDirtyChange,
  onCancel,
  onSave,
}: {
  transaction: Transaction
  accounts: Account[]
  categories: Category[]
  tags: Tag[]
  columnCount: number
  saving: boolean
  onCreateTag: (name: string) => Promise<Tag | null>
  onDirtyChange: (dirty: boolean) => void
  onCancel: () => void
  onSave: (correction: Correction) => void
}) {
  const [accountId, setAccountId] = useState<number | ''>(transaction.accountId)
  const [categoryId, setCategoryId] = useState<number | ''>(transaction.categoryId)
  const [date, setDate] = useState(transaction.date)
  const [amount, setAmount] = useState(String(transaction.amount))
  const [description, setDescription] = useState(transaction.description)
  const [needWant, setNeedWant] = useState<NeedWant | ''>(transaction.needWant)
  const [tagIds, setTagIds] = useState<number[]>(transaction.tagIds)
  const [creatingTag, setCreatingTag] = useState(false)
  const [amountError, setAmountError] = useState<string | null>(null)
  const [descriptionError, setDescriptionError] = useState<string | null>(null)
  const slipRef = useRef<HTMLTableRowElement>(null)

  const dirty =
    accountId !== transaction.accountId ||
    categoryId !== transaction.categoryId ||
    date !== transaction.date ||
    Number(amount) !== transaction.amount ||
    description !== transaction.description ||
    needWant !== transaction.needWant ||
    tagIds.length !== transaction.tagIds.length ||
    tagIds.some((id) => !transaction.tagIds.includes(id))

  useEffect(() => {
    onDirtyChange(dirty)
  }, [dirty, onDirtyChange])

  // Escape means the same thing everywhere on the slip: leave the correction unmade.
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== 'Escape') return
      const target = event.target as HTMLElement | null
      // The tag combobox owns Escape while its suggestions are open.
      if (target?.getAttribute('role') === 'combobox' && target.getAttribute('aria-expanded') === 'true') return
      onCancel()
    }
    const slip = slipRef.current
    slip?.addEventListener('keydown', handleKeyDown)
    return () => slip?.removeEventListener('keydown', handleKeyDown)
  }, [onCancel])

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (accountId === '' || categoryId === '' || needWant === '') return

    // An empty amount used to reach the ledger as 0.00. The figure a household is asked to trust
    // is never inferred from a blank box. Validated here rather than by the browser so the
    // problem is named in the app's own words, beside the field it belongs to.
    const parsedAmount = Number(amount)
    const badAmount = amount.trim() === '' || !Number.isFinite(parsedAmount)
    const badDescription = description.trim() === ''
    setAmountError(badAmount ? 'Enter an amount — negative for money out, positive for money in.' : null)
    setDescriptionError(badDescription ? 'Enter what this entry was for.' : null)
    if (badAmount || badDescription) return

    onSave({
      input: { accountId, categoryId, date, amount: parsedAmount, description, needWant },
      tagIds,
    })
  }

  return (
    <tr className="correction" ref={slipRef}>
      <td colSpan={columnCount}>
        <form className="correction-slip" onSubmit={handleSubmit} noValidate>
          <div className="correction-head">
            <h3>Correcting this entry</h3>
            <p>{transaction.description}</p>
          </div>

          <div className="grid">
            <label htmlFor={`correct-date-${transaction.id}`}>
              Date
              <input
                id={`correct-date-${transaction.id}`}
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                required
              />
            </label>
            <label htmlFor={`correct-account-${transaction.id}`}>
              Account
              <AccountSelect
                id={`correct-account-${transaction.id}`}
                accounts={accounts}
                value={accountId}
                onChange={setAccountId}
                required
              />
            </label>
            <label htmlFor={`correct-category-${transaction.id}`}>
              Category
              <CategorySelect
                id={`correct-category-${transaction.id}`}
                categories={categories}
                value={categoryId}
                onChange={setCategoryId}
                required
              />
            </label>
          </div>

          <div className="grid">
            <label htmlFor={`correct-description-${transaction.id}`}>
              Description
              <input
                id={`correct-description-${transaction.id}`}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                aria-describedby={`correct-description-error-${transaction.id}`}
                aria-invalid={descriptionError !== null}
                required
              />
              {descriptionError && (
                <small id={`correct-description-error-${transaction.id}`} className="field-error">
                  {descriptionError}
                </small>
              )}
            </label>
            <label htmlFor={`correct-amount-${transaction.id}`}>
              Amount
              <input
                id={`correct-amount-${transaction.id}`}
                type="number"
                step="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                aria-describedby={`correct-amount-hint-${transaction.id}`}
                aria-invalid={amountError !== null}
                required
              />
              <small id={`correct-amount-hint-${transaction.id}`} className={amountError ? 'field-error' : undefined}>
                {amountError ?? 'Negative for money out, positive for money in.'}
              </small>
            </label>
            <label htmlFor={`correct-need-want-${transaction.id}`}>
              Need/Want
              <NeedWantSelect
                id={`correct-need-want-${transaction.id}`}
                value={needWant}
                onChange={setNeedWant}
                required
              />
            </label>
          </div>

          <div className="correction-tags">
            <span className="correction-tags-label">Tags</span>
            <TagLine
              tags={tags}
              selectedIds={tagIds}
              onChange={setTagIds}
              onCreate={onCreateTag}
              onBusyChange={setCreatingTag}
            />
          </div>

          <div className="correction-actions">
            <button type="submit" aria-busy={saving || creatingTag} disabled={saving || creatingTag}>
              Save correction
            </button>
            <button type="button" className="secondary" onClick={onCancel}>
              Cancel
            </button>
          </div>
        </form>
      </td>
    </tr>
  )
}
