import type { Tag } from '../api/tags'

export function TagSelect({
  id,
  label,
  tags,
  value,
  onChange,
}: {
  id?: string
  label?: string
  tags: Tag[]
  value: number | ''
  onChange: (value: number | '') => void
}) {
  return (
    <select
      id={id}
      aria-label={label}
      value={value}
      onChange={(e) => onChange(e.target.value === '' ? '' : Number(e.target.value))}
    >
      <option value="">All tags</option>
      {tags.map((tag) => (
        <option key={tag.id} value={tag.id}>
          {tag.name}
        </option>
      ))}
    </select>
  )
}
