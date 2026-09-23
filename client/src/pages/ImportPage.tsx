import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { accountsApi, type Account } from '../api/accounts'
import {
  dateFormatLabel,
  dateFormats,
  delimiterLabel,
  delimiters,
  importApi,
  type ColumnRole,
  type MappingDraft,
  type Reading,
  type UploadPreview,
} from '../api/import'

type Step = 'upload' | 'map'

const ROLE_KEYS = ['dateColumn', 'descriptionColumn', 'amountColumn', 'debitColumn', 'creditColumn'] as const

type RoleKey = (typeof ROLE_KEYS)[number]

const ROLE_OF_KEY: Record<RoleKey, Exclude<ColumnRole, null>> = {
  dateColumn: 'date',
  descriptionColumn: 'description',
  amountColumn: 'amount',
  debitColumn: 'debit',
  creditColumn: 'credit',
}

const KEY_OF_ROLE: Record<Exclude<ColumnRole, null>, RoleKey> = {
  date: 'dateColumn',
  description: 'descriptionColumn',
  amount: 'amountColumn',
  debit: 'debitColumn',
  credit: 'creditColumn',
}

const ROLE_LABELS: { value: Exclude<ColumnRole, null>; label: string }[] = [
  { value: 'date', label: 'Date' },
  { value: 'description', label: 'Description' },
  { value: 'amount', label: 'Amount' },
  { value: 'debit', label: 'Debit' },
  { value: 'credit', label: 'Credit' },
]

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

/** The server hands back an ISO date; a reader wants to see the month said out loud. */
function readableDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  return `${MONTHS[month - 1]} ${day}, ${year}`
}

function readableTimestamp(iso: string): string {
  return readableDate(iso.slice(0, 10))
}

export function ImportPage() {
  const [accounts, setAccounts] = useState<Account[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [step, setStep] = useState<Step>('upload')
  const [accountId, setAccountId] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)

  const [preview, setPreview] = useState<UploadPreview | null>(null)
  const [draft, setDraft] = useState<MappingDraft | null>(null)
  const [reading, setReading] = useState<Reading | null>(null)
  const [readingError, setReadingError] = useState<string | null>(null)
  const [showFormat, setShowFormat] = useState(false)

  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [savedAt, setSavedAt] = useState<string | null>(null)

  useEffect(() => {
    accountsApi
      .fetchAll()
      .then(setAccounts)
      .catch(() => setLoadError('Failed to load accounts.'))
  }, [])

  const readingRequest = useRef(0)

  // The reading is re-fetched whenever the mapping changes, because the delimiter and the header
  // flag change the columns themselves — not just how they are read.
  useEffect(() => {
    if (!preview || !draft) {
      return
    }
    const sequence = ++readingRequest.current
    importApi
      .readPreview(preview.token, draft)
      .then((result) => {
        if (readingRequest.current !== sequence) {
          return
        }
        setReading(result)
        setReadingError(null)
        // Changing the separator or the header flag changes which columns exist at all, so a role
        // pointing at a column this file no longer has is dropped rather than quietly saved.
        setDraft((current) => {
          if (!current) {
            return current
          }
          const next = { ...current }
          let changed = false
          for (const key of ROLE_KEYS) {
            const column = next[key]
            if (column && !result.columns.includes(column)) {
              next[key] = null
              changed = true
            }
          }
          return changed ? next : current
        })
      })
      .catch(() => {
        if (readingRequest.current === sequence) {
          setReading(null)
          setReadingError(
            'The uploaded file is no longer held for this session. Choose the file again to carry on.',
          )
        }
      })
  }, [preview, draft])

  const selectedAccount = accounts?.find((a) => String(a.id) === accountId) ?? null

  async function handleUpload(event: FormEvent) {
    event.preventDefault()
    setUploadError(null)
    if (!accountId) {
      setUploadError('Choose the account this export came from.')
      return
    }
    if (!file) {
      setUploadError('Choose the CSV file you exported from the bank.')
      return
    }

    setUploading(true)
    try {
      const result = await importApi.uploadPreview(Number(accountId), file)
      setPreview(result)
      setDraft(draftFrom(result))
      setReading(null)
      setReadingError(null)
      setSavedAt(null)
      setSaveError(null)
      setShowFormat(false)
      setStep('map')
    } catch (error) {
      setUploadError(error instanceof Error ? error.message : 'That file could not be read as a CSV.')
    } finally {
      setUploading(false)
    }
  }

  const assign = useCallback((column: string, role: ColumnRole) => {
    setDraft((current) => {
      if (!current) {
        return current
      }
      const next = { ...current }
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
    })
    setSavedAt(null)
  }, [])

  const updateDraft = useCallback((patch: Partial<MappingDraft>) => {
    setDraft((current) => (current ? { ...current, ...patch } : current))
    setSavedAt(null)
  }, [])

  const columns = reading?.columns ?? preview?.columns ?? []
  const sampleRows = reading?.sampleRows ?? preview?.sampleRows ?? []

  const roleByColumn = useMemo(() => {
    const map = new Map<string, Exclude<ColumnRole, null>>()
    if (draft) {
      for (const key of ROLE_KEYS) {
        const column = draft[key]
        if (column) {
          map.set(column, ROLE_OF_KEY[key])
        }
      }
    }
    return map
  }, [draft])

  const requirement = draft ? unmetRequirement(draft) : null

  async function handleSave(event: FormEvent) {
    event.preventDefault()
    setSaveError(null)
    if (!preview || !draft) {
      return
    }
    if (requirement) {
      setSaveError(requirement)
      return
    }
    if (readingError) {
      setSaveError(readingError)
      return
    }

    setSaving(true)
    try {
      const result = await importApi.saveMapping(preview.accountId, draft)
      setSavedAt(result.updatedAt)
    } catch {
      setSaveError('Failed to save the mapping. Try again.')
    } finally {
      setSaving(false)
    }
  }

  function startOver() {
    setStep('upload')
    setFile(null)
    setSavedAt(null)
    setSaveError(null)
  }

  return (
    <>
      <hgroup>
        <h1>Import</h1>
        <p>Read a CSV export from the bank, and remember how that account's export is laid out.</p>
      </hgroup>

      <ol className="stations" aria-label="Import steps">
        <li className="station" data-state={step === 'upload' ? 'current' : 'done'}>
          Upload
        </li>
        <li className="station" data-state={step === 'map' ? 'current' : 'ahead'}>
          Map columns
        </li>
        <li className="station" data-state="ahead">
          Preview &amp; confirm
          <span className="station-note">Not built yet</span>
        </li>
      </ol>

      {loadError && <p role="alert">{loadError}</p>}

      {accounts === null ? (
        <p aria-busy="true">Loading…</p>
      ) : accounts.length === 0 ? (
        <div className="empty">
          <h3>No accounts yet</h3>
          <p>An import needs an account to belong to, and to remember its column layout against.</p>
          <Link className="button" to="/accounts">
            Add an account
          </Link>
        </div>
      ) : step === 'upload' ? (
        <form onSubmit={handleUpload}>
          <div className="well">
            <div className="well-fields">
              <label htmlFor="import-account">
                Account
                <select
                  id="import-account"
                  value={accountId}
                  onChange={(event) => setAccountId(event.target.value)}
                  required
                >
                  <option value="">Choose an account…</option>
                  {accounts.map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name}
                    </option>
                  ))}
                </select>
              </label>

              <label htmlFor="import-file">
                CSV file
                <input
                  id="import-file"
                  type="file"
                  accept=".csv,text/csv"
                  onChange={(event) => {
                    setFile(event.target.files?.[0] ?? null)
                    setUploadError(null)
                  }}
                  required
                />
              </label>
            </div>

            {file && (
              <p className="file-chosen">
                <strong>{file.name}</strong> — nothing is imported yet; this only reads the file.
              </p>
            )}
          </div>

          {uploadError && (
            <p className="field-error" role="alert">
              {uploadError}
            </p>
          )}

          <div className="step-actions">
            <button type="submit" disabled={uploading} aria-busy={uploading || undefined}>
              {uploading ? 'Reading…' : 'Read the file'}
            </button>
          </div>
        </form>
      ) : preview && draft ? (
        <form onSubmit={handleSave}>
          <FormatNote
            draft={draft}
            fileName={preview.fileName}
            open={showFormat}
            onToggle={() => setShowFormat((open) => !open)}
            onChange={updateDraft}
          />

          {preview.saved && preview.missingColumns.length === 0 && (
            <p className="note">
              Remembered from the last import for <strong>{selectedAccount?.name ?? 'this account'}</strong>,{' '}
              {readableTimestamp(preview.saved.updatedAt)}. Check it still reads correctly, then save.
            </p>
          )}

          {preview.missingColumns.length > 0 && (
            <p className="note" data-signal="true" role="alert">
              This export no longer has{' '}
              <strong>{preview.missingColumns.map((column) => `"${column}"`).join(', ')}</strong>. Assign those
              roles again below.
            </p>
          )}

          <div className="table-scroll">
            <table className="mapper">
              <caption className="visually-hidden">
                The first {sampleRows.length} rows of {preview.fileName}, with a role for each column
              </caption>
              <thead>
                <tr>
                  {columns.map((column) => {
                    const role = roleByColumn.get(column) ?? ''
                    return (
                      <th key={column} scope="col" data-assigned={role ? 'true' : 'false'}>
                        <select
                          aria-label={`Role for column ${column}`}
                          value={role}
                          onChange={(event) => assign(column, (event.target.value || null) as ColumnRole)}
                        >
                          <option value="">— not used —</option>
                          {ROLE_LABELS.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>
                        <span className="mapper-source">{column}</span>
                      </th>
                    )
                  })}
                </tr>
              </thead>
              <tbody>
                {sampleRows.map((row, rowIndex) => (
                  <tr key={rowIndex}>
                    {columns.map((column, columnIndex) => (
                      <td key={column} data-assigned={roleByColumn.has(column) ? 'true' : 'false'}>
                        {row[columnIndex] ?? ''}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <section className="reading" aria-label="Reads as" aria-live="polite">
            <p className="reading-head">Reads as</p>
            {readingError ? (
              <p className="reading-error" role="alert">
                {readingError}
              </p>
            ) : reading === null ? (
              <p aria-busy="true">Reading…</p>
            ) : (
              reading.rows.map((row, index) => (
                <div className="reading-row" key={index} data-error={row.error ? 'true' : 'false'}>
                  {row.error ? (
                    <p className="reading-error">{row.error}</p>
                  ) : (
                    <>
                      <span className="reading-date">
                        {row.date ? readableDate(row.date) : <span className="reading-missing">no date</span>}
                      </span>
                      <span className="reading-description">
                        {row.description ?? <span className="reading-missing">no description</span>}
                      </span>
                      <span className="reading-amount">
                        {row.amount === null ? (
                          <span className="reading-missing">no amount</span>
                        ) : (
                          row.amount.toFixed(2)
                        )}
                      </span>
                    </>
                  )}
                </div>
              ))
            )}
          </section>

          {savedAt && (
            <p className="note" role="status">
              Mapping saved for <strong>{selectedAccount?.name ?? 'this account'}</strong>. The next export from
              this account arrives already mapped. Importing the rows themselves is not built yet.
            </p>
          )}

          {saveError && (
            <p className="field-error" role="alert">
              {saveError}
            </p>
          )}

          <div className="step-actions">
            <button type="submit" disabled={saving} aria-busy={saving || undefined}>
              {saving ? 'Saving…' : 'Save mapping'}
            </button>
            <button type="button" className="secondary" onClick={startOver}>
              Choose another file
            </button>
          </div>
        </form>
      ) : null}
    </>
  )
}

/** Pre-fills from what this account was mapped with last time, dropping columns the file has lost. */
function draftFrom(preview: UploadPreview): MappingDraft {
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

/** The first thing still missing before the mapping can be saved, named as an instruction. */
function unmetRequirement(draft: MappingDraft): string | null {
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

/**
 * What the app worked out about the file's shape, in words rather than format strings — with the
 * controls to correct it, for the moment the reading below says the guess was wrong.
 */
function FormatNote({
  draft,
  fileName,
  open,
  onToggle,
  onChange,
}: {
  draft: MappingDraft
  fileName: string
  open: boolean
  onToggle: () => void
  onChange: (patch: Partial<MappingDraft>) => void
}) {
  return (
    <div className="note">
      <p>
        <strong>{fileName}</strong> reads as {delimiterLabel(draft.delimiter)}-separated,{' '}
        {draft.hasHeaderRow ? 'with a header row' : 'with no header row'}, dates as{' '}
        {dateFormatLabel(draft.dateFormat)}.{' '}
        <button type="button" className="linklike" onClick={onToggle} aria-expanded={open}>
          {open ? 'Done' : 'Change'}
        </button>
      </p>

      {open && (
        <div className="note-controls">
          <label htmlFor="import-delimiter">
            Column separator
            <select
              id="import-delimiter"
              value={draft.delimiter}
              onChange={(event) => onChange({ delimiter: event.target.value })}
            >
              {delimiters.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label htmlFor="import-date-format">
            Date format
            <select
              id="import-date-format"
              value={draft.dateFormat}
              onChange={(event) => onChange({ dateFormat: event.target.value })}
            >
              {dateFormats.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>

          <label className="checkline" htmlFor="import-header-row">
            <input
              id="import-header-row"
              type="checkbox"
              checked={draft.hasHeaderRow}
              onChange={(event) => onChange({ hasHeaderRow: event.target.checked })}
            />
            First row names the columns
          </label>
        </div>
      )}
    </div>
  )
}
