import { useId, useState, type FormEvent } from 'react'
import type { Account } from '../api/accounts'
import type { Category } from '../api/categories'
import { ApiError } from '../api/client'
import { importApi, matchTypes, type AssignmentRule, type IgnoreMatchType } from '../api/import'
import type { Tag } from '../api/tags'
import type { NeedWant } from '../api/transactions'
import { AccountSelect } from './AccountSelect'
import { CategorySelect } from './CategorySelect'
import { NeedWantSelect } from './NeedWantSelect'
import { TagLine } from './TagLine'

/**
 * The ignore rule's sentence with a second clause: classify rows where the description [contains]
 * [KROGER] for [every account] as [Groceries] [Need] [+tags]. Any of the three assignments may be
 * left blank, but not all of them — a rule that sets nothing cannot be read back as a sentence.
 */
export function AssignmentRuleForm({
  accounts,
  categories,
  tags,
  onCreateTag,
  onSaved,
}: {
  accounts: Account[]
  categories: Category[]
  tags: Tag[]
  onCreateTag: (name: string) => Promise<Tag | null>
  onSaved: (rule: AssignmentRule) => void
}) {
  const fieldId = useId()
  const [matchType, setMatchType] = useState<IgnoreMatchType>('Contains')
  const [matchText, setMatchText] = useState('')
  const [accountId, setAccountId] = useState<number | ''>('')
  const [categoryId, setCategoryId] = useState<number | ''>('')
  const [needWant, setNeedWant] = useState<NeedWant | ''>('')
  const [tagIds, setTagIds] = useState<number[]>([])
  const [creatingTag, setCreatingTag] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const setsNothing = categoryId === '' && needWant === '' && tagIds.length === 0

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (setsNothing) return

    // Validated here rather than by the browser so the refusal is in the app's own words, beside
    // the field it belongs to.
    if (matchText.trim() === '') {
      setError('Type the words the rule should look for.')
      return
    }

    setError(null)
    setSaving(true)
    try {
      const rule = await importApi.createAssignmentRule({
        accountId: accountId === '' ? null : accountId,
        matchText,
        matchType,
        categoryId: categoryId === '' ? null : categoryId,
        needWant: needWant === '' ? null : needWant,
        tagIds,
      })
      setMatchText('')
      setMatchType('Contains')
      setAccountId('')
      setCategoryId('')
      setNeedWant('')
      setTagIds([])
      onSaved(rule)
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? 'That rule is already written — same words, same kind of match, same accounts.'
          : err instanceof ApiError && err.status === 404
            ? 'An account, category or tag this rule names no longer exists. Reload the page and try again.'
            : 'Failed to save the rule. Try again.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="rule-form" onSubmit={handleSubmit} noValidate>
      <p className="rule-lead">Classify rows where the description</p>

      <div className="rule-fields">
        <label htmlFor={`${fieldId}-type`}>
          Match
          <select
            id={`${fieldId}-type`}
            value={matchType}
            onChange={(event) => setMatchType(event.target.value as IgnoreMatchType)}
          >
            {matchTypes.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label htmlFor={`${fieldId}-text`}>
          These words
          <input
            id={`${fieldId}-text`}
            value={matchText}
            onChange={(event) => setMatchText(event.target.value)}
            aria-invalid={error !== null}
            aria-describedby={error ? `${fieldId}-error` : undefined}
            maxLength={200}
          />
        </label>

        <label htmlFor={`${fieldId}-scope`}>
          For
          <AccountSelect
            id={`${fieldId}-scope`}
            accounts={accounts}
            value={accountId}
            onChange={setAccountId}
            includeAllOption
          />
        </label>
      </div>

      <p className="rule-lead rule-lead-then">As</p>

      <div className="rule-fields rule-assignments">
        <label htmlFor={`${fieldId}-category`}>
          Category
          <CategorySelect
            id={`${fieldId}-category`}
            categories={categories}
            value={categoryId}
            onChange={setCategoryId}
            placeholder="Leave unset"
          />
        </label>

        <label htmlFor={`${fieldId}-needwant`}>
          Need or want
          <NeedWantSelect
            id={`${fieldId}-needwant`}
            value={needWant}
            onChange={setNeedWant}
            placeholder="Leave unset"
          />
        </label>

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
      </div>

      {error && (
        <p className="field-error" id={`${fieldId}-error`} role="alert">
          {error}
        </p>
      )}

      <div className="rule-actions">
        <button
          type="submit"
          disabled={saving || creatingTag || setsNothing}
          aria-busy={saving || creatingTag || undefined}
          aria-describedby={setsNothing ? `${fieldId}-reason` : undefined}
        >
          Save rule
        </button>
        {setsNothing && (
          <p className="rule-reason" id={`${fieldId}-reason`}>
            Set a category, Need or Want, or a tag to save this rule
          </p>
        )}
      </div>
    </form>
  )
}
