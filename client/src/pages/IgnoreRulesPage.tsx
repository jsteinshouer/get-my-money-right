import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { accountsApi, type Account } from '../api/accounts'
import { importApi, matchTypeLabel, type IgnoreRule } from '../api/import'
import { IgnoreRuleForm } from '../components/IgnoreRuleForm'

/**
 * The ledger of ignore rules. Rules are normally born from the preview row that provoked them; this
 * page is where they are read back and struck out. There is no active/inactive switch — deleting a
 * rule is how it is turned off, so the whole list is rules that are in force right now.
 */
export function IgnoreRulesPage() {
  const [rules, setRules] = useState<IgnoreRule[] | null>(null)
  const [accounts, setAccounts] = useState<Account[]>([])
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [confirming, setConfirming] = useState<IgnoreRule | null>(null)

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

  useEffect(() => {
    void load()
    void loadAccounts()
  }, [load, loadAccounts])

  async function handleDelete(rule: IgnoreRule) {
    setError(null)
    setNotice(null)
    setConfirming(null)
    try {
      await importApi.deleteIgnoreRule(rule.id)
      setNotice(`Deleted the rule for rows whose description ${describe(rule)}. Rows it was catching will import.`)
      await load()
    } catch {
      setError('Failed to delete the rule.')
    }
  }

  return (
    <>
      <hgroup>
        <h1>Ignore rules</h1>
        <p>
          Rows whose description matches a rule are struck out in an import preview and never reach the
          transaction list — recurring noise, and the transfer between your own accounts.
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
            Delete the rule for rows whose description <strong>{describe(confirming)}</strong>
            {confirming.accountName === null ? ' on any account' : ` on ${confirming.accountName}`}? Rows it was
            catching will import from now on.
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

      <article>
        <hgroup>
          <h2>Add a rule</h2>
          <p>Matching ignores capitalisation, and reads the description exactly as the preview prints it.</p>
        </hgroup>
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
            <caption className="visually-hidden">Ignore rules, those applying to every account first</caption>
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
                    <button className="contrast" onClick={() => setConfirming(rule)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p className="memo">
        Back to <Link to="/import">Import</Link>. A rule takes effect the next time a preview is read, so a
        preview already on screen re-reads when you return to it.
      </p>
    </>
  )
}

function describe(rule: IgnoreRule): string {
  return `${matchTypeLabel(rule.matchType)} "${rule.matchText}"`
}
