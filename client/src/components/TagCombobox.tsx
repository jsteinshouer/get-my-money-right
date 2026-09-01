import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent } from 'react'
import type { Tag } from '../api/tags'

const NO_ACTIVE_OPTION = -1

/**
 * The annotation line of the cash book: you write the tag rather than hunting it in a list.
 * Type-ahead over what exists, Enter on a name that doesn't yet exist creates it — so a tag can
 * be invented at the moment the investigation needs it, without leaving the page.
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
  /** Creating a tag is a round-trip; whoever owns the surrounding form must not let it be
      submitted mid-flight, or the new tag is silently dropped. */
  onBusyChange?: (busy: boolean) => void
}) {
  const [query, setQuery] = useState('')
  const [activeIndex, setActiveIndex] = useState(NO_ACTIVE_OPTION)
  const [busy, setBusy] = useState(false)
  const [openAbove, setOpenAbove] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const listboxId = useId()
  const optionId = (index: number) => `${listboxId}-option-${index}`

  const trimmed = query.trim()

  const matches = useMemo(() => {
    const available = tags.filter((tag) => !excludeIds.includes(tag.id))
    if (trimmed === '') return available.slice(0, 8)
    const needle = trimmed.toLowerCase()
    return available.filter((tag) => tag.name.toLowerCase().includes(needle)).slice(0, 8)
  }, [tags, excludeIds, trimmed])

  // A name that already exists — on this tag or on one already applied — must never offer "create".
  const isExisting = trimmed !== '' && tags.some((tag) => tag.name.toLowerCase() === trimmed.toLowerCase())
  const canCreate = trimmed !== '' && !isExisting
  const optionCount = matches.length + (canCreate ? 1 : 0)
  const isOpen = optionCount > 0
  const createIndex = canCreate ? matches.length : NO_ACTIVE_OPTION

  // The slip lives inside the table's own scroll box, which clips it; near the foot of the
  // ledger there is no room below, so it opens upward instead of disappearing.
  useEffect(() => {
    if (!isOpen) return
    const input = inputRef.current
    const clipper = input?.closest('.table-scroll')
    if (!input || !clipper) return
    const room = clipper.getBoundingClientRect().bottom - input.getBoundingClientRect().bottom
    setOpenAbove(room < 220)
  }, [isOpen, optionCount])

  function reset() {
    setQuery('')
    setActiveIndex(NO_ACTIVE_OPTION)
  }

  function pick(tag: Tag) {
    onPick(tag)
    reset()
    inputRef.current?.focus()
  }

  async function create(name: string) {
    setBusy(true)
    onBusyChange?.(true)
    try {
      const created = await onCreate(name)
      if (created) {
        onPick(created)
        reset()
      }
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
    const tag = matches[index] ?? (activeIndex === NO_ACTIVE_OPTION ? matches[0] : undefined)
    if (tag) {
      pick(tag)
      return
    }
    if (canCreate) await create(trimmed)
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      if (optionCount === 0) return
      const step = event.key === 'ArrowDown' ? 1 : -1
      const next = activeIndex === NO_ACTIVE_OPTION ? (step === 1 ? 0 : optionCount - 1) : activeIndex + step
      setActiveIndex((next + optionCount) % optionCount)
      return
    }
    if (event.key === 'Enter') {
      event.preventDefault()
      if (trimmed === '' && activeIndex === NO_ACTIVE_OPTION) return
      void commit(activeIndex)
      return
    }
    if (event.key === 'Escape') {
      reset()
    }
  }

  return (
    <div className="tag-combobox">
      <input
        ref={inputRef}
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
        }}
        onKeyDown={handleKeyDown}
      />
      <ul
        className={openAbove ? 'tag-suggestions is-above' : 'tag-suggestions'}
        role="listbox"
        id={listboxId}
        hidden={!isOpen}
      >
        {matches.map((tag, index) => (
          <li
            key={tag.id}
            id={optionId(index)}
            role="option"
            aria-selected={index === activeIndex}
            className={index === activeIndex ? 'is-active' : undefined}
            onMouseDown={(e) => {
              e.preventDefault()
              pick(tag)
            }}
          >
            <span>{tag.name}</span>
            <span className="tag-suggestion-count">{tag.transactionCount}</span>
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
            New tag “{trimmed}”
          </li>
        )}
      </ul>
    </div>
  )
}
