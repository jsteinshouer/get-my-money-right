import type { Tag } from '../api/tags'
import { NameCombobox } from './NameCombobox'

/**
 * The annotation line of the cash book: you write the tag rather than hunting it in a list, and
 * Enter on a name that doesn't yet exist creates it. Each pick clears the line for the next tag.
 */
export function TagCombobox({
  label,
  tags,
  excludeIds = [],
  placeholder = 'Write a tag…',
  disabled = false,
  onPick,
  onCreate,
  onBusyChange,
}: {
  label: string
  tags: Tag[]
  excludeIds?: number[]
  placeholder?: string
  disabled?: boolean
  onPick: (tag: Tag) => void
  onCreate: (name: string) => Promise<Tag | null>
  onBusyChange?: (busy: boolean) => void
}) {
  return (
    <NameCombobox
      label={label}
      items={tags}
      excludeIds={excludeIds}
      placeholder={placeholder}
      disabled={disabled}
      createLabel={(name) => `New tag “${name}”`}
      renderMeta={(tag) => tag.transactionCount}
      onPick={onPick}
      onCreate={onCreate}
      onBusyChange={onBusyChange}
    />
  )
}
