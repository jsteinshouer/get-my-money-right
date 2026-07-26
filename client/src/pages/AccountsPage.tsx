import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { accountsApi, type Account, type AccountType } from '../api/accounts'
import { AccountTypeSelect } from '../components/AccountTypeSelect'

export function AccountsPage() {
  const [accounts, setAccounts] = useState<Account[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)

  const [newName, setNewName] = useState('')
  const [newType, setNewType] = useState<AccountType>('Checking')
  const [adding, setAdding] = useState(false)

  const load = useCallback(async () => {
    try {
      setAccounts(await accountsApi.fetchAll(true))
    } catch {
      setError('Failed to load accounts.')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function handleAdd(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setAdding(true)
    try {
      await accountsApi.create(newName, newType)
      setNewName('')
      setNewType('Checking')
      await load()
    } catch {
      setError('Failed to add the account.')
    } finally {
      setAdding(false)
    }
  }

  async function handleDeactivate(id: number) {
    setError(null)
    try {
      await accountsApi.deactivate(id)
      await load()
    } catch {
      setError('Failed to deactivate the account.')
    }
  }

  return (
    <>
      <h1>Accounts</h1>

      {error && <p role="alert">{error}</p>}

      {accounts === null ? (
        <p aria-busy="true">Loading…</p>
      ) : accounts.length === 0 ? (
        <p>No accounts yet — add one below.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Type</th>
              <th scope="col">Status</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {accounts.map((account) =>
              editingId === account.id ? (
                <EditRow key={account.id} account={account} onDone={() => setEditingId(null)} onSaved={load} onError={setError} />
              ) : (
                <tr key={account.id}>
                  <td>{account.name}</td>
                  <td>{account.type}</td>
                  <td>{account.isActive ? 'Active' : 'Inactive'}</td>
                  <td>
                    <button className="secondary" onClick={() => setEditingId(account.id)}>
                      Edit
                    </button>{' '}
                    {account.isActive && (
                      <button className="contrast" onClick={() => void handleDeactivate(account.id)}>
                        Deactivate
                      </button>
                    )}
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      )}

      <article>
        <h2>Add an account</h2>
        <form onSubmit={handleAdd}>
          <label htmlFor="new-account-name">
            Name
            <input id="new-account-name" value={newName} onChange={(e) => setNewName(e.target.value)} required />
          </label>
          <label htmlFor="new-account-type">
            Type
            <AccountTypeSelect id="new-account-type" value={newType} onChange={setNewType} />
          </label>
          <button type="submit" aria-busy={adding} disabled={adding}>
            Add account
          </button>
        </form>
      </article>
    </>
  )
}

function EditRow({
  account,
  onDone,
  onSaved,
  onError,
}: {
  account: Account
  onDone: () => void
  onSaved: () => Promise<void>
  onError: (message: string) => void
}) {
  const [name, setName] = useState(account.name)
  const [type, setType] = useState<AccountType>(account.type)
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      await accountsApi.update(account.id, name, type)
      onDone()
      await onSaved()
    } catch {
      onError('Failed to save the account.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <tr>
      <td>
        <input aria-label="Name" value={name} onChange={(e) => setName(e.target.value)} />
      </td>
      <td>
        <AccountTypeSelect label="Type" value={type} onChange={setType} />
      </td>
      <td>{account.isActive ? 'Active' : 'Inactive'}</td>
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
