import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { accountsApi, type Account } from '../api/accounts'
import { categoriesApi, type Category } from '../api/categories'
import { ApiError } from '../api/client'
import {
  describeAssignments,
  importApi,
  matchTypeLabel,
  type AssignmentRule,
  type IgnoreRule,
} from '../api/import'
import { tagsApi, type Tag } from '../api/tags'
import { AssignmentRuleForm } from '../components/AssignmentRuleForm'
import { IgnoreRuleForm } from '../components/IgnoreRuleForm'

/** The rule awaiting a delete confirmation, and which kind it is. */
type Confirming = { kind: 'assign'; rule: AssignmentRule } | { kind: 'ignore'; rule: IgnoreRule }

/**
 * The import rules, both kinds on one page: what an import does to your rows, answered in one
 * place. Classify first, skip second — the household writes far more of the first. There is no
 * edit and no active/inactive switch: deleting a rule is how it is turned off or changed, so every
 * rule listed is in force right now.
 */
export function IgnoreRulesPage() {
  const [assignmentRules, setAssignmentRules] = useState<AssignmentRule[] | null>(null)
  const [rules, setRules] = useState<IgnoreRule[] | null>(null)
  const [accounts, setAccounts] = useState<Account[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [tags, setTags] = useState<Tag[]>([])
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [confirming, setConfirming] = useState<Confirming | null>(null)

  const loadAssignmentRules = useCallback(async () => {
    try {
      setAssignmentRules(await importApi.fetchAssignmentRules())
    } catch {
      setError('Failed to load the assignment rules.')
    }
  }, [])

  const load = useCallback(async () => {
    try {
      setRules(await importApi.fetchIgnoreRules())
    } catch {
      setError('Failed to load the ignore rules.')
    }
  }, [])

  const loadAccounts = useCallback(async () => {
    try {
      setAccounts(await accountsApi.fetchAll())
    } catch {
      setError('Failed to load accounts.')
    }
  }, [])

  const loadCategories = useCallback(async () => {
    try {
      setCategories(await categoriesApi.fetchAll())
    } catch {
      setError('Failed to load categories.')
    }
  }, [])

  const loadTags = useCallback(async () => {
    try {
      setTags(await tagsApi.fetchAll())
    } catch {
      setError('Failed to load tags.')
    }
  }, [])

  useEffect(() => {
    void loadAssignmentRules()
    void load()
    void loadAccounts()
    void loadCategories()
    void loadTags()
  }, [loadAssignmentRules, load, loadAccounts, loadCategories, loadTags])

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

  async function handleDelete(target: Confirming) {
    setError(null)
    setNotice(null)
    setConfirming(null)
    try {
      if (target.kind === 'assign') {
        await importApi.deleteAssignmentRule(target.rule.id)
        setNotice(
          `Deleted the rule for rows whose description ${describe(target.rule)}. Transactions already in the ledger keep what they have.`,
        )
        await loadAssignmentRules()
      } else {
        await importApi.deleteIgnoreRule(target.rule.id)
        setNotice(
          `Deleted the rule for rows whose description ${describe(target.rule)}. Rows it was catching will import.`,
        )
        await load()
      }
    } catch {
      setError('Failed to delete the rule.')
    }
  }

  return (
    <>
      <hgroup>
        <h1>Import rules</h1>
        <p>
          How an import reads your rows. Matching ignores capitalisation and reads the description exactly as
          the preview prints it. A rule affects future imports only — it never changes a transaction already in
          the ledger.
        </p>
      </hgroup>

      {error && <p role="alert">{error}</p>}
      {notice && (
        <p role="status" className="notice">
          {notice}
        </p>
      )}

      {confirming !== null && (
        <div className="confirm" role="alertdialog" aria-labelledby="confirm-delete-rule">
          <p id="confirm-delete-rule">
            Delete the rule {confirming.kind === 'assign' ? 'that classifies' : 'for'} rows whose description{' '}
            <strong>{describe(confirming.rule)}</strong>
            {confirming.rule.accountName === null ? ' on any account' : ` on ${confirming.rule.accountName}`}?{' '}
            {confirming.kind === 'assign'
              ? 'Future imports will no longer classify rows by it.'
              : 'Rows it was catching will import from now on.'}
          </p>
          <div className="confirm-actions">
            <button className="contrast" onClick={() => void handleDelete(confirming)}>
              Delete rule
            </button>{' '}
            <button className="secondary" onClick={() => setConfirming(null)}>
              Keep it
            </button>
          </div>
        </div>
      )}

      <section aria-labelledby="classify-rules-head">
        <div className="section-head">
          <h2 id="classify-rules-head">Rules that classify a row</h2>
        </div>

        <article>
          <AssignmentRuleForm
            accounts={accounts}
            categories={categories}
            tags={tags}
            onCreateTag={createTag}
            onSaved={(rule) => {
              setError(null)
              setNotice(
                `Added a rule: rows whose description ${describe(rule)}${
                  rule.accountName === null ? ' on any account' : ` on ${rule.accountName}`
                } are ${describeAssignments(rule)}.`,
              )
              void loadAssignmentRules()
            }}
          />
        </article>

        {assignmentRules === null ? (
          <p aria-busy="true">Loading…</p>
        ) : assignmentRules.length === 0 ? (
          <div className="empty">
            <p>No rules yet. Write one from a row in an import preview.</p>
          </div>
        ) : (
          <div className="table-scroll">
            <table>
              <caption className="visually-hidden">
                Rules that classify a row, those applying to every account first
              </caption>
              <thead>
                <tr>
                  <th scope="col">Match</th>
                  <th scope="col">Assigns</th>
                  <th scope="col">Scope</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {assignmentRules.map((rule) => (
                  <tr key={rule.id}>
                    <td>
                      {matchTypeLabel(rule.matchType)} <strong>{rule.matchText}</strong>
                    </td>
                    <td>{describeAssignments(rule)}</td>
                    <td>{rule.accountName ?? 'All accounts'}</td>
                    <td>
                      <button className="contrast" onClick={() => setConfirming({ kind: 'assign', rule })}>
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section aria-labelledby="skip-rules-head">
        <div className="section-head">
          <h2 id="skip-rules-head">Rules that skip a row</h2>
        </div>

        <article>
          <IgnoreRuleForm
            accounts={accounts}
            submitLabel="Add rule"
            clearAfterSave
            onSaved={(rule) => {
              setError(null)
              setNotice(
                `Added a rule for rows whose description ${describe(rule)}${
                  rule.accountName === null ? ' on any account' : ` on ${rule.accountName}`
                }.`,
              )
              void load()
            }}
          />
        </article>

        {rules === null ? (
          <p aria-busy="true">Loading…</p>
        ) : rules.length === 0 ? (
          <div className="empty">
            <h3>No ignore rules yet</h3>
            <p>Add one above, or from a row in an import preview — which is where you usually spot the noise.</p>
            <Link className="button" to="/import">
              Go to import
            </Link>
          </div>
        ) : (
          <div className="table-scroll">
            <table>
              <caption className="visually-hidden">Rules that skip a row, those applying to every account first</caption>
              <thead>
                <tr>
                  <th scope="col">Match</th>
                  <th scope="col">Scope</th>
                  <th scope="col">Actions</th>
                </tr>
              </thead>
              <tbody>
                {rules.map((rule) => (
                  <tr key={rule.id}>
                    <td>
                      {matchTypeLabel(rule.matchType)} <strong>{rule.matchText}</strong>
                    </td>
                    <td>{rule.accountName ?? 'All accounts'}</td>
                    <td>
                      <button className="contrast" onClick={() => setConfirming({ kind: 'ignore', rule })}>
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <p className="memo">
        Back to <Link to="/import">Import</Link>. A rule takes effect the next time a preview is read, so a
        preview already on screen re-reads when you return to it.
      </p>
    </>
  )
}

function describe(rule: IgnoreRule | AssignmentRule): string {
  return `${matchTypeLabel(rule.matchType)} "${rule.matchText}"`
}
