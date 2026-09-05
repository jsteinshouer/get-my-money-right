import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import { tagsApi, type Tag } from '../api/tags'

export function TagsPage() {
  const [tags, setTags] = useState<Tag[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [confirming, setConfirming] = useState<Tag | null>(null)

  const [newName, setNewName] = useState('')
  const [adding, setAdding] = useState(false)

  const load = useCallback(async () => {
    try {
      setTags(await tagsApi.fetchAll())
    } catch {
      setError('Failed to load tags.')
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function handleAdd(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setNotice(null)
    setAdding(true)
    try {
      await tagsApi.create(newName)
      setNewName('')
      await load()
    } catch (err) {
      setError(
        err instanceof ApiError && err.status === 409
          ? `A tag called “${newName.trim()}” already exists.`
          : 'Failed to add the tag.',
      )
    } finally {
      setAdding(false)
    }
  }

  async function handleDelete(tag: Tag) {
    setError(null)
    setNotice(null)
    setConfirming(null)
    try {
      await tagsApi.delete(tag.id)
      // Deleting a tag detaches it from every transaction carrying it, so the count is reported
      // back rather than the rows changing quietly underneath.
      setNotice(
        tag.transactionCount === 0
          ? `Deleted “${tag.name}”.`
          : `Deleted “${tag.name}” and removed it from ${tag.transactionCount} ${
              tag.transactionCount === 1 ? 'transaction' : 'transactions'
            }.`,
      )
      await load()
    } catch {
      setError('Failed to delete the tag.')
    }
  }

  return (
    <>
      <h1>Tags</h1>

      {error && <p role="alert">{error}</p>}
      {notice && <p role="status" className="notice">{notice}</p>}

      {confirming !== null && (
        <div className="confirm" role="alertdialog" aria-labelledby="confirm-delete-tag">
          <p id="confirm-delete-tag">
            {confirming.transactionCount === 0 ? (
              <>Delete “{confirming.name}”? Nothing carries it.</>
            ) : (
              <>
                Delete “{confirming.name}”? It will be removed from{' '}
                <strong>
                  {confirming.transactionCount}{' '}
                  {confirming.transactionCount === 1 ? 'transaction' : 'transactions'}
                </strong>
                . The transactions themselves are kept.
              </>
            )}
          </p>
          <div className="confirm-actions">
            <button className="contrast" onClick={() => void handleDelete(confirming)}>
              Delete tag
            </button>{' '}
            <button className="secondary" onClick={() => setConfirming(null)}>
              Keep it
            </button>
          </div>
        </div>
      )}

      {tags === null ? (
        <p aria-busy="true">Loading…</p>
      ) : tags.length === 0 ? (
        <p>No tags yet — add one below, or write one straight onto a transaction.</p>
      ) : (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col" className="money">Transactions</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {tags.map((tag) => (
                <tr key={tag.id}>
                  <td>
                    <span className="tag-mark">{tag.name}</span>
                  </td>
                  <td className="money">{tag.transactionCount}</td>
                  <td>
                    <button className="contrast" onClick={() => setConfirming(tag)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <article>
        <hgroup>
          <h2>Add a tag</h2>
          <p>
            Tags cut across categories — “Vacation 2026”, “Kids”, “Tax deductible” — and a transaction can
            carry several. You can also write a new one straight onto a transaction as you tag it.
          </p>
        </hgroup>
        <form onSubmit={handleAdd}>
          <label htmlFor="new-tag-name">
            Name
            <input id="new-tag-name" value={newName} onChange={(e) => setNewName(e.target.value)} required />
          </label>
          <button type="submit" aria-busy={adding} disabled={adding}>
            Add tag
          </button>
        </form>
      </article>
    </>
  )
}
