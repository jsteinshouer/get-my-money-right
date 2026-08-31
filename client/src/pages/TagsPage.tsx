import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { tagsApi, type Tag } from '../api/tags'

export function TagsPage() {
  const [tags, setTags] = useState<Tag[] | null>(null)
  const [error, setError] = useState<string | null>(null)

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
    setAdding(true)
    try {
      await tagsApi.create(newName)
      setNewName('')
      await load()
    } catch {
      setError('Failed to add the tag.')
    } finally {
      setAdding(false)
    }
  }

  async function handleDelete(id: number) {
    setError(null)
    try {
      await tagsApi.delete(id)
      await load()
    } catch {
      setError('Failed to delete the tag.')
    }
  }

  return (
    <>
      <h1>Tags</h1>

      {error && <p role="alert">{error}</p>}

      {tags === null ? (
        <p aria-busy="true">Loading…</p>
      ) : tags.length === 0 ? (
        <p>No tags yet — add one below.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {tags.map((tag) => (
              <tr key={tag.id}>
                <td>{tag.name}</td>
                <td>
                  <button className="contrast" onClick={() => void handleDelete(tag.id)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <article>
        <hgroup>
          <h2>Add a tag</h2>
          <p>Tags cut across categories — “vacation”, “kids”, “one-off” — and a transaction can carry several.</p>
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
