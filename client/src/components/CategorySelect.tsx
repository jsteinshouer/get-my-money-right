import type { Category } from '../api/categories'

export function CategorySelect({
  id,
  label,
  categories,
  value,
  onChange,
  includeAllOption = false,
  required = false,
  placeholder = 'Select a category',
}: {
  id?: string
  label?: string
  categories: Category[]
  value: number | ''
  onChange: (value: number | '') => void
  includeAllOption?: boolean
  required?: boolean
  /** What the empty choice reads as, when there is no "all" option to show instead. */
  placeholder?: string
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
        {includeAllOption ? 'All categories' : placeholder}
      </option>
      {categories.map((category) => (
        <option key={category.id} value={category.id}>
          {category.name}
        </option>
      ))}
    </select>
  )
}
