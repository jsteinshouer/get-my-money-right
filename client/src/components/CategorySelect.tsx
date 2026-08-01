import type { Category } from '../api/categories'

export function CategorySelect({
  id,
  label,
  categories,
  value,
  onChange,
  includeAllOption = false,
  required = false,
}: {
  id?: string
  label?: string
  categories: Category[]
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
        {includeAllOption ? 'All categories' : 'Select a category'}
      </option>
      {categories.map((category) => (
        <option key={category.id} value={category.id}>
          {category.name}
        </option>
      ))}
    </select>
  )
}
