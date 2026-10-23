import type { Category } from '../api/categories'
import { NameCombobox } from './NameCombobox'

/**
 * One category, written rather than hunted: type-ahead over the household's categories, and Enter
 * on a name they don't have yet creates it on the spot — so the first rule of a fresh ledger does
 * not mean a trip to the Categories page. Clearing the text leaves the category unset.
 */
export function CategoryCombobox({
  id,
  categories,
  value,
  onChange,
  onCreate,
  onBusyChange,
  placeholder = 'Write a category…',
}: {
  /** For a `<label htmlFor>` that names the field. */
  id: string
  categories: Category[]
  value: number | ''
  onChange: (value: number | '') => void
  onCreate: (name: string) => Promise<Category | null>
  onBusyChange?: (busy: boolean) => void
  placeholder?: string
}) {
  const chosen = value === '' ? null : (categories.find((category) => category.id === value) ?? null)

  return (
    <NameCombobox
      id={id}
      items={categories}
      value={chosen}
      placeholder={placeholder}
      createLabel={(name) => `New category “${name}”`}
      onPick={(category) => onChange(category.id)}
      onClear={() => onChange('')}
      onCreate={onCreate}
      onBusyChange={onBusyChange}
    />
  )
}
