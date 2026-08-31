import type { Tag } from '../api/tags'

// A transaction carries several tags at once, so this is a checkbox list rather than a
// <select multiple>: every option stays visible and one click away.
export function TagMultiSelect({
  tags,
  selectedIds,
  onChange,
}: {
  tags: Tag[]
  selectedIds: number[]
  onChange: (selectedIds: number[]) => void
}) {
  if (tags.length === 0) {
    return <small>No tags yet.</small>
  }

  function toggle(tagId: number, checked: boolean) {
    onChange(checked ? [...selectedIds, tagId] : selectedIds.filter((id) => id !== tagId))
  }

  return (
    <fieldset className="tag-picker">
      <legend className="visually-hidden">Tags</legend>
      {tags.map((tag) => (
        <label key={tag.id} className="tag-option">
          <input
            type="checkbox"
            checked={selectedIds.includes(tag.id)}
            onChange={(e) => toggle(tag.id, e.target.checked)}
          />
          {tag.name}
        </label>
      ))}
    </fieldset>
  )
}
