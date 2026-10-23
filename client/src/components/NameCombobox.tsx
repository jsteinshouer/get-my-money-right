import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent, type ReactNode } from 'react'

const NO_ACTIVE_OPTION = -1

export interface Named {
  id: number
  name: string
}

/**
 * Writing a name rather than hunting it in a list: type-ahead over what exists, and Enter on a name
 * that doesn't yet exist creates it — so a tag or a category can be invented at the moment the work
 * needs it, without leaving the page.
 *
 * Two ways to hold what is picked. Without `value` it is a line for adding one name after another:
 * a pick clears the line for the next. With `value` it holds a single choice and shows its name;
 * editing the text lets go of the choice, and leaving the field settles on an exact match or clears.
 */
export function NameCombobox<T extends Named>({
  id,
  label,
  items,
  excludeIds = [],
  placeholder,
  disabled = false,
  value,
  createLabel,
  renderMeta,
  onPick,
  onClear,
  onCreate,
  onBusyChange,
}: {
  id?: string
  /** An accessible name of its own, for a field with no visible `<label>` pointing at `id`. */
  label?: string
  items: T[]
  excludeIds?: number[]
  placeholder: string
  disabled?: boolean
  /** Single-choice mode: the item currently chosen, or null for none. Omit for a line of picks. */
  value?: T | null
  createLabel: (name: string) => string
  renderMeta?: (item: T) => ReactNode
  onPick: (item: T) => void
  /** Single-choice mode: the text no longer names the chosen item. */
  onClear?: () => void
  onCreate: (name: string) => Promise<T | null>
  /** Creating is a round-trip; whoever owns the surrounding form must not let it be submitted
      mid-flight, or the new name is silently dropped. */
  onBusyChange?: (busy: boolean) => void
}) {
  const single = value !== undefined
  const chosenName = value?.name ?? ''
  const [query, setQuery] = useState(chosenName)
  const [activeIndex, setActiveIndex] = useState(NO_ACTIVE_OPTION)
  const [busy, setBusy] = useState(false)
  const [openAbove, setOpenAbove] = useState(false)
  const [suggesting, setSuggesting] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const listboxId = useId()
  const optionId = (index: number) => `${listboxId}-option-${index}`

  // A choice made or cleared from outside — a form reset after saving — shows in the text.
  useEffect(() => {
    if (single) setQuery(chosenName)
  }, [single, chosenName])

  const trimmed = query.trim()
  // Showing the chosen name is not a search for it: the whole list stays on offer.
  const needle = single && trimmed === chosenName ? '' : trimmed.toLowerCase()

  const matches = useMemo(() => {
    const available = items.filter((item) => !excludeIds.includes(item.id))
    if (needle === '') return available.slice(0, 8)
    return available.filter((item) => item.name.toLowerCase().includes(needle)).slice(0, 8)
  }, [items, excludeIds, needle])

  // A name that already exists — chosen, or already applied — must never offer "create".
  const isExisting = trimmed !== '' && items.some((item) => item.name.toLowerCase() === trimmed.toLowerCase())
  const canCreate = trimmed !== '' && !isExisting
  const optionCount = matches.length + (canCreate ? 1 : 0)
  // Only once the writer has asked for it — by typing, or by arrowing down. Springing open on
  // focus, or again after a pick, puts a slip over whatever sits beneath the line.
  const isOpen = suggesting && optionCount > 0
  const createIndex = canCreate ? matches.length : NO_ACTIVE_OPTION

  // The slip may live inside a table's own scroll box, which clips it; near the foot of the
  // ledger there is no room below, so it opens upward instead of disappearing.
  useEffect(() => {
    if (!isOpen) return
    const input = inputRef.current
    const clipper = input?.closest('.table-scroll')
    if (!input || !clipper) return
    const room = clipper.getBoundingClientRect().bottom - input.getBoundingClientRect().bottom
    setOpenAbove(room < 220)
  }, [isOpen, optionCount])

  function close(text: string) {
    setQuery(text)
    setActiveIndex(NO_ACTIVE_OPTION)
    setSuggesting(false)
  }

  function pick(item: T) {
    onPick(item)
    close(single ? item.name : '')
    inputRef.current?.focus()
  }

  async function create(name: string) {
    setBusy(true)
    onBusyChange?.(true)
    try {
      const created = await onCreate(name)
      if (created) pick(created)
    } finally {
      setBusy(false)
      onBusyChange?.(false)
      inputRef.current?.focus()
    }
  }

  async function commit(index: number) {
    if (index === createIndex && canCreate) {
      await create(trimmed)
      return
    }
    const item = matches[index] ?? (activeIndex === NO_ACTIVE_OPTION ? matches[0] : undefined)
    if (item) {
      pick(item)
      return
    }
    if (canCreate) await create(trimmed)
  }

  /** Leaving a single-choice field never leaves text standing that names nothing. */
  function settle() {
    setSuggesting(false)
    if (!single || busy) return
    const exact = items.find((item) => item.name.toLowerCase() === trimmed.toLowerCase())
    if (exact && exact.id !== value?.id) {
      pick(exact)
    } else if (!exact) {
      if (value) onClear?.()
      close('')
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      setSuggesting(true)
      if (optionCount === 0) return
      const step = event.key === 'ArrowDown' ? 1 : -1
      const next = activeIndex === NO_ACTIVE_OPTION ? (step === 1 ? 0 : optionCount - 1) : activeIndex + step
      setActiveIndex((next + optionCount) % optionCount)
      return
    }
    if (event.key === 'Enter') {
      event.preventDefault()
      if (activeIndex === NO_ACTIVE_OPTION && (trimmed === '' || (single && trimmed === chosenName))) return
      void commit(activeIndex)
      return
    }
    if (event.key === 'Escape') {
      close(single ? chosenName : '')
    }
  }

  return (
    <div className="combobox">
      <input
        ref={inputRef}
        id={id}
        type="text"
        role="combobox"
        aria-label={label}
        aria-expanded={isOpen}
        aria-controls={listboxId}
        aria-autocomplete="list"
        aria-activedescendant={activeIndex === NO_ACTIVE_OPTION ? undefined : optionId(activeIndex)}
        aria-busy={busy}
        autoComplete="off"
        disabled={disabled || busy}
        placeholder={placeholder}
        value={query}
        onChange={(e) => {
          setQuery(e.target.value)
          setActiveIndex(NO_ACTIVE_OPTION)
          setSuggesting(true)
          if (single && value && e.target.value.trim() !== value.name) onClear?.()
        }}
        // Options commit on mousedown with preventDefault, so blur never races a pick.
        onBlur={settle}
        onKeyDown={handleKeyDown}
      />
      <ul
        className={openAbove ? 'combobox-suggestions is-above' : 'combobox-suggestions'}
        role="listbox"
        id={listboxId}
        hidden={!isOpen}
      >
        {matches.map((item, index) => (
          <li
            key={item.id}
            id={optionId(index)}
            role="option"
            aria-selected={index === activeIndex}
            className={index === activeIndex ? 'is-active' : undefined}
            onMouseDown={(e) => {
              e.preventDefault()
              pick(item)
            }}
          >
            <span>{item.name}</span>
            {renderMeta && <span className="combobox-meta">{renderMeta(item)}</span>}
          </li>
        ))}
        {canCreate && (
          <li
            id={optionId(createIndex)}
            role="option"
            aria-selected={createIndex === activeIndex}
            className={createIndex === activeIndex ? 'is-active is-create' : 'is-create'}
            onMouseDown={(e) => {
              e.preventDefault()
              void create(trimmed)
            }}
          >
            {createLabel(trimmed)}
          </li>
        )}
      </ul>
    </div>
  )
}
