import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { importApi, type ColumnRole, type MappingDraft, type Reading, type UploadPreview } from '../../api/import'
import { readFailure, type ReadFailure } from './failures'
import { FormatNote } from './FormatNote'
import { readableTimestamp } from './format'
import {
  ROLE_LABELS,
  assignRole,
  draftFrom,
  rolesByColumn,
  sameMapping,
  unmetRequirement,
  withoutMissingColumns,
} from './mapping'
import { ReadingLine } from './ReadingLine'

/**
 * Station 2: the document is the form. The file's first rows print verbatim with a role selector on
 * each column, and under the double rule the same rows re-print as the app would store them — the
 * proof, before a single row is written, that the file is being read correctly.
 */
export function MapStep({
  heading,
  preview,
  draft,
  accountName,
  onDraftChange,
  onPreview,
  onStartOver,
}: {
  heading: ReactNode
  preview: UploadPreview
  draft: MappingDraft
  accountName: string
  onDraftChange: (update: (draft: MappingDraft) => MappingDraft) => void
  onPreview: () => void
  onStartOver: () => void
}) {
  const [reading, setReading] = useState<Reading | null>(null)
  const [readingFailure, setReadingFailure] = useState<ReadFailure | null>(null)
  /** Bumped by "Try again", so the reading is fetched again for a mapping that has not changed. */
  const [readingAttempt, setReadingAttempt] = useState(0)
  const [showFormat, setShowFormat] = useState(false)

  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [savedAt, setSavedAt] = useState<string | null>(null)

  const readingRequest = useRef(0)

  // The reading is re-fetched whenever the mapping changes, because the delimiter and the header
  // flag change the columns themselves — not just how they are read.
  useEffect(() => {
    const sequence = ++readingRequest.current
    importApi
      .readPreview(preview.token, draft)
      .then((result) => {
        if (readingRequest.current !== sequence) {
          return
        }
        setReading(result)
        setReadingFailure(null)
        onDraftChange((current) => withoutMissingColumns(current, result.columns))
      })
      .catch((error: unknown) => {
        // A reading of some other mapping would be misleading, so it goes — but only an upload
        // that is really gone sends the household back for the file.
        if (readingRequest.current === sequence) {
          setReading(null)
          setReadingFailure(readFailure(error))
        }
      })
  }, [preview.token, draft, readingAttempt, onDraftChange])

  /** A change the household made: the saved mapping no longer matches what is on screen. */
  function changeDraft(update: (draft: MappingDraft) => MappingDraft) {
    onDraftChange(update)
    setSavedAt(null)
  }

  const columns = reading?.columns ?? preview.columns
  const sampleRows = reading?.sampleRows ?? preview.sampleRows
  const roleByColumn = useMemo(() => rolesByColumn(draft), [draft])
  const requirement = unmetRequirement(draft)

  // The fifth import from the same bank: the mapping on screen is the one already saved, so the
  // next step is to read the file, not to save what is saved. Import saves it again anyway.
  const remembered =
    preview.saved !== null && preview.missingColumns.length === 0 && sameMapping(draft, draftFrom(preview))

  /** What stops the mapping going any further, said as the instruction that clears it. */
  function blocker(): string | null {
    return requirement ?? readingFailure?.message ?? null
  }

  async function handleSave(event: FormEvent) {
    event.preventDefault()
    const blocked = blocker()
    setSaveError(blocked)
    if (blocked) {
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

  /** Station 2 settles the mapping; station 3 is the same reading carrying the whole file. */
  function goToPreview() {
    const blocked = blocker()
    setSaveError(blocked)
    if (!blocked) {
      onPreview()
    }
  }

  return (
    <form onSubmit={handleSave}>
      {heading}
      <FormatNote
        draft={draft}
        fileName={preview.fileName}
        open={showFormat}
        onToggle={() => setShowFormat((open) => !open)}
        onChange={(patch) => changeDraft((current) => ({ ...current, ...patch }))}
      />

      {preview.saved && preview.missingColumns.length === 0 && (
        <p className="note">
          Remembered from the last import for <strong>{accountName}</strong>,{' '}
          {readableTimestamp(preview.saved.updatedAt)}. Check it still reads correctly, then preview every row.
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
                      onChange={(event) =>
                        changeDraft((current) =>
                          assignRole(current, column, (event.target.value || null) as ColumnRole),
                        )
                      }
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
        {readingFailure ? (
          <p className="reading-error" role="alert">
            {readingFailure.message}{' '}
            {!readingFailure.gone && (
              <button type="button" className="linklike" onClick={() => setReadingAttempt((n) => n + 1)}>
                Try again
              </button>
            )}
          </p>
        ) : reading === null ? (
          <p aria-busy="true">Reading…</p>
        ) : (
          reading.rows.map((row, index) => (
            <div className="reading-row" key={index} data-error={row.error ? 'true' : 'false'}>
              {row.error ? <p className="reading-error">{row.error}</p> : <ReadingLine row={row} />}
            </div>
          ))
        )}
      </section>

      {savedAt && (
        <p className="note" role="status">
          Mapping saved for <strong>{accountName}</strong>. The next export from this account arrives
          already mapped.
        </p>
      )}

      {saveError && (
        <p className="field-error" role="alert">
          {saveError}
        </p>
      )}

      <div className="step-actions">
        {remembered && (
          <button type="button" onClick={goToPreview}>
            Preview all rows
          </button>
        )}
        <button
          type="submit"
          className={remembered ? 'secondary' : undefined}
          disabled={saving}
          aria-busy={saving || undefined}
        >
          {saving ? 'Saving…' : 'Save mapping'}
        </button>
        {!remembered && (
          <button type="button" className="secondary" onClick={goToPreview}>
            Preview all rows
          </button>
        )}
        <button type="button" className="secondary" onClick={onStartOver}>
          Choose another file
        </button>
      </div>
    </form>
  )
}
