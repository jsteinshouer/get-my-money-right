import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import { categoriesApi, type Category } from '../api/categories'

export function CategoriesPage() {
  const [categories, setCategories] = useState<Category[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editingId, setEditingId] = useState<number | null>(null)

  const [newName, setNewName] = useState('')
  const [adding, setAdding] = useState(false)

  const load = useCallback(async () => {
    try {
      setCategories(await categoriesApi.fetchAll())
    } catch {
      setError('Failed to load categories.')
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
      await categoriesApi.create(newName)
      setNewName('')
      await load()
    } catch {
      setError('Failed to add the category.')
    } finally {
      setAdding(false)
    }
  }

  async function handleDelete(id: number) {
    setError(null)
    try {
      await categoriesApi.delete(id)
      await load()
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        setError('This category still has transactions attached and cannot be deleted.')
      } else {
        setError('Failed to delete the category.')
      }
    }
  }

  return (
    <>
      <h1>Categories</h1>

      {error && <p role="alert">{error}</p>}

      {categories === null ? (
        <p aria-busy="true">Loading…</p>
      ) : categories.length === 0 ? (
        <p>No categories yet — add one below.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {categories.map((category) =>
              editingId === category.id ? (
                <EditRow
                  key={category.id}
                  category={category}
                  onDone={() => setEditingId(null)}
                  onSaved={load}
                  onError={setError}
                />
              ) : (
                <tr key={category.id}>
                  <td>{category.name}</td>
                  <td>
                    <button className="secondary" onClick={() => setEditingId(category.id)}>
                      Rename
                    </button>{' '}
                    <button className="contrast" onClick={() => void handleDelete(category.id)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      )}

      <article>
        <h2>Add a category</h2>
        <form onSubmit={handleAdd}>
          <label htmlFor="new-category-name">
            Name
            <input id="new-category-name" value={newName} onChange={(e) => setNewName(e.target.value)} required />
          </label>
          <button type="submit" aria-busy={adding} disabled={adding}>
            Add category
          </button>
        </form>
      </article>
    </>
  )
}

function EditRow({
  category,
  onDone,
  onSaved,
  onError,
}: {
  category: Category
  onDone: () => void
  onSaved: () => Promise<void>
  onError: (message: string) => void
}) {
  const [name, setName] = useState(category.name)
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    setSaving(true)
    try {
      await categoriesApi.update(category.id, name)
      onDone()
      await onSaved()
    } catch {
      onError('Failed to save the category.')
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
