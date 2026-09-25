import { useId, useState, type FormEvent } from 'react'
import type { Account } from '../api/accounts'
import { ApiError } from '../api/client'
import { importApi, matchTypes, type IgnoreMatchType, type IgnoreRule } from '../api/import'
import { AccountSelect } from './AccountSelect'

/**
 * One sentence with three holes in it: skip rows where the description [contains] [AUTOPAY] for
 * [Sapphire Card]. The same form writes a rule from a preview row and from the rules page, so the
 * rule a household member reads back is worded the same way wherever they wrote it.
 */
export function IgnoreRuleForm({
  accounts,
  initialMatchText = '',
  initialAccountId = null,
  submitLabel,
  clearAfterSave = false,
  onSaved,
  onCancel,
}: {
  accounts: Account[]
  initialMatchText?: string
  /** The account the rule starts scoped to; null means every account. */
  initialAccountId?: number | null
  submitLabel: string
  /** For a form that stays on screen to write the next rule, rather than closing behind one. */
  clearAfterSave?: boolean
  onSaved: (rule: IgnoreRule) => void
  onCancel?: () => void
}) {
  const fieldId = useId()
  const [matchType, setMatchType] = useState<IgnoreMatchType>('Contains')
  const [matchText, setMatchText] = useState(initialMatchText)
  const [accountId, setAccountId] = useState<number | ''>(initialAccountId ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()

    // Validated here rather than by the browser so the refusal is in the app's own words, beside
    // the field it belongs to.
    if (matchText.trim() === '') {
      setError('Type the words the rule should look for.')
      return
    }

    setError(null)
    setSaving(true)
    try {
      const rule = await importApi.createIgnoreRule({
        accountId: accountId === '' ? null : accountId,
        matchText,
        matchType,
      })
      if (clearAfterSave) {
        setMatchText('')
        setMatchType('Contains')
        setAccountId(initialAccountId ?? '')
      }
      onSaved(rule)
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? 'That rule is already written — same words, same kind of match, same accounts.'
          : 'Failed to save the rule. Try again.',
      )
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="rule-form" onSubmit={handleSubmit} noValidate>
      <p className="rule-lead">Skip rows where the description</p>

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

      {error && (
        <p className="field-error" id={`${fieldId}-error`} role="alert">
          {error}
        </p>
      )}

      <div className="rule-actions">
        <button type="submit" disabled={saving} aria-busy={saving || undefined}>
          {submitLabel}
        </button>
        {onCancel && (
          <button type="button" className="secondary" onClick={onCancel}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
