import type { Tag } from '../api/tags'
import { TagCombobox } from './TagCombobox'

const MOST_USED_SHOWN = 5

/**
 * A transaction's tags as they read in the book: the applied ones written out as marks, and one
 * line to write another. The most-used tags sit beneath it so the common case stays a single
 * click and only the long tail costs typing.
 */
export function TagLine({
  tags,
  selectedIds,
  onChange,
  onCreate,
  onBusyChange,
}: {
  tags: Tag[]
  selectedIds: number[]
  onChange: (selectedIds: number[]) => void
  onCreate: (name: string) => Promise<Tag | null>
  onBusyChange?: (busy: boolean) => void
}) {
  const selected = selectedIds
    .map((id) => tags.find((tag) => tag.id === id))
    .filter((tag): tag is Tag => tag !== undefined)

  const mostUsed = tags
    .filter((tag) => !selectedIds.includes(tag.id) && tag.transactionCount > 0)
    .sort((a, b) => b.transactionCount - a.transactionCount || a.name.localeCompare(b.name))
    .slice(0, MOST_USED_SHOWN)

  return (
    <div className="tag-line">
      {selected.length > 0 && (
        <ul className="tag-marks">
          {selected.map((tag) => (
            <li key={tag.id}>
              <span className="tag-mark">{tag.name}</span>
              <button
                type="button"
                className="tag-remove"
                aria-label={`Remove tag ${tag.name}`}
                onClick={() => onChange(selectedIds.filter((id) => id !== tag.id))}
              >
                ×
              </button>
            </li>
          ))}
        </ul>
      )}

      <TagCombobox
        label="Add a tag"
        tags={tags}
        excludeIds={selectedIds}
        onPick={(tag) => onChange([...selectedIds, tag.id])}
        onCreate={onCreate}
        onBusyChange={onBusyChange}
      />

      {mostUsed.length > 0 && (
        <div className="tag-recent">
          <span className="tag-recent-label">Most used</span>
          {mostUsed.map((tag) => (
            <button
              key={tag.id}
              type="button"
              className="tag-recent-option"
              onClick={() => onChange([...selectedIds, tag.id])}
            >
              {tag.name}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
