import type { Tag } from '../api/tags'

export function TagSelect({
  id,
  label,
  tags,
  value,
  onChange,
  includeAllOption = true,
  required = false,
}: {
  id?: string
  label?: string
  tags: Tag[]
  value: number | ''
  onChange: (value: number | '') => void
  includeAllOption?: boolean
  required?: boolean
}) {
  return (
    <select
      id={id}
      aria-label={label}
      value={value}
      required={required}
      onChange={(e) => onChange(e.target.value === '' ? '' : Number(e.target.value))}
    >
      <option value="" disabled={required && !includeAllOption}>
        {includeAllOption ? 'All tags' : 'Select a tag'}
      </option>
      {tags.map((tag) => (
        <option key={tag.id} value={tag.id}>
          {tag.name}
        </option>
      ))}
    </select>
  )
}
