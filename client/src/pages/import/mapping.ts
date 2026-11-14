import type { ColumnRole, MappingDraft, UploadPreview } from '../../api/import'

type Role = Exclude<ColumnRole, null>

const ROLE_KEYS = ['dateColumn', 'descriptionColumn', 'amountColumn', 'debitColumn', 'creditColumn'] as const

type RoleKey = (typeof ROLE_KEYS)[number]

const ROLE_OF_KEY: Record<RoleKey, Role> = {
  dateColumn: 'date',
  descriptionColumn: 'description',
  amountColumn: 'amount',
  debitColumn: 'debit',
  creditColumn: 'credit',
}

const KEY_OF_ROLE: Record<Role, RoleKey> = {
  date: 'dateColumn',
  description: 'descriptionColumn',
  amount: 'amountColumn',
  debit: 'debitColumn',
  credit: 'creditColumn',
}

export const ROLE_LABELS: { value: Role; label: string }[] = [
  { value: 'date', label: 'Date' },
  { value: 'description', label: 'Description' },
  { value: 'amount', label: 'Amount' },
  { value: 'debit', label: 'Debit' },
  { value: 'credit', label: 'Credit' },
]

/** Pre-fills from what this account was mapped with last time, dropping columns the file has lost. */
export function draftFrom(preview: UploadPreview): MappingDraft {
  const saved = preview.saved
  const present = (column: string | null | undefined) =>
    column && preview.columns.includes(column) ? column : null

  if (!saved) {
    return {
      delimiter: preview.delimiter,
      hasHeaderRow: preview.hasHeaderRow,
      dateFormat: preview.dateFormat,
      dateColumn: null,
      descriptionColumn: null,
      amountColumn: null,
      debitColumn: null,
      creditColumn: null,
    }
  }

  return {
    delimiter: saved.delimiter,
    hasHeaderRow: saved.hasHeaderRow,
    dateFormat: saved.dateFormat,
    dateColumn: present(saved.dateColumn),
    descriptionColumn: present(saved.descriptionColumn),
    amountColumn: present(saved.amountColumn),
    debitColumn: present(saved.debitColumn),
    creditColumn: present(saved.creditColumn),
  }
}

export function sameMapping(a: MappingDraft, b: MappingDraft): boolean {
  return (
    a.delimiter === b.delimiter &&
    a.hasHeaderRow === b.hasHeaderRow &&
    a.dateFormat === b.dateFormat &&
    ROLE_KEYS.every((key) => a[key] === b[key])
  )
}

/** Which role each column holds in the draft, for the mapper's head cells. */
export function rolesByColumn(draft: MappingDraft): Map<string, Role> {
  const map = new Map<string, Role>()
  for (const key of ROLE_KEYS) {
    const column = draft[key]
    if (column) {
      map.set(column, ROLE_OF_KEY[key])
    }
  }
  return map
}

/** The draft with `column` given `role` — or no role, when `role` is null. */
export function assignRole(draft: MappingDraft, column: string, role: ColumnRole): MappingDraft {
  const next = { ...draft }
  // A column holds one role, and a role sits in one column: assigning either side clears the other.
  for (const key of ROLE_KEYS) {
    if (next[key] === column) {
      next[key] = null
    }
  }
  if (role) {
    next[KEY_OF_ROLE[role]] = column
    // An amount arrives as one signed column or as a debit/credit pair, never as both.
    if (role === 'amount') {
      next.debitColumn = null
      next.creditColumn = null
    } else if (role === 'debit' || role === 'credit') {
      next.amountColumn = null
    }
  }
  return next
}

/**
 * Changing the separator or the header flag changes which columns exist at all, so a role pointing at
 * a column this file no longer has is dropped rather than quietly saved. The same draft comes back
 * when nothing was dropped, so a re-render is not set off for nothing.
 */
export function withoutMissingColumns(draft: MappingDraft, columns: string[]): MappingDraft {
  const next = { ...draft }
  let changed = false
  for (const key of ROLE_KEYS) {
    const column = next[key]
    if (column && !columns.includes(column)) {
      next[key] = null
      changed = true
    }
  }
  return changed ? next : draft
}

/** The first thing still missing before the mapping can be saved, named as an instruction. */
export function unmetRequirement(draft: MappingDraft): string | null {
  if (!draft.dateColumn) {
    return 'Give one column the Date role before saving.'
  }
  if (!draft.descriptionColumn) {
    return 'Give one column the Description role before saving.'
  }
  if (draft.amountColumn) {
    return null
  }
  if (draft.debitColumn && draft.creditColumn) {
    return null
  }
  if (draft.debitColumn || draft.creditColumn) {
    return 'A debit column needs a credit column beside it. Assign both, or use one Amount column instead.'
  }
  return 'Give one column the Amount role, or give two columns the Debit and Credit roles.'
}
