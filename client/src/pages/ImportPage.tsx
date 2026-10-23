import {
  Fragment,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
  type Ref,
} from 'react'
import { Link } from 'react-router-dom'
import { accountsApi, type Account } from '../api/accounts'
import { ApiError } from '../api/client'
import {
  dateFormatLabel,
  dateFormats,
  delimiterLabel,
  delimiters,
  importApi,
  type ColumnRole,
  type FullPreview,
  type ImportResult,
  type MappingDraft,
  type PreviewRow,
  type Reading,
  type UploadPreview,
} from '../api/import'
import { transactionsApi } from '../api/transactions'
import { IgnoreRuleForm } from '../components/IgnoreRuleForm'

type Step = 'upload' | 'map' | 'preview'

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

  const [rows, setRows] = useState<FullPreview | null>(null)
  const [rowsError, setRowsError] = useState<string | null>(null)
  /** Rows a rule has just caught, which draw their stroke rather than arriving already struck. */
  const [justStruck, setJustStruck] = useState<ReadonlySet<number>>(new Set())
  const [slipRow, setSlipRow] = useState<number | null>(null)

  const [importing, setImporting] = useState(false)
  const [importError, setImportError] = useState<string | null>(null)
  const [result, setResult] = useState<ImportResult | null>(null)
  /** The account's review queue after the import: the count the memo's link actually opens. */
  const [waiting, setWaiting] = useState(0)

  useEffect(() => {
    accountsApi
      .fetchAll()
      .then(setAccounts)
      .catch(() => setLoadError('Failed to load accounts.'))
  }, [])

  /**
   * Each step opens on its own heading. Without this, every station change drops keyboard and
   * screen-reader focus back to the top of the document, and the next thing read is the masthead.
   */
  const stepHeading = useRef<HTMLHeadingElement>(null)
  const firstStep = useRef(true)
  useEffect(() => {
    if (firstStep.current) {
      firstStep.current = false
      return
    }
    stepHeading.current?.focus()
  }, [step])

  /** The row a rule was just written from: focus returns there once the file re-reads. */
  const [focusRow, setFocusRow] = useState<number | null>(null)

  const readingRequest = useRef(0)
  /** What the file read as last time, so a row that has just been struck can draw its stroke. */
  const lastRead = useRef<PreviewRow[] | null>(null)

  // The reading is re-fetched whenever the mapping changes, because the delimiter and the header
  // flag change the columns themselves — not just how they are read.
  useEffect(() => {
    if (step !== 'map' || !preview || !draft) {
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
  }, [step, preview, draft])

  const selectedAccount = accounts?.find((a) => String(a.id) === accountId) ?? null

  /**
   * Reads the whole file again through the rules in force right now. Every arrival at station 3
   * re-reads, so a rule written or deleted elsewhere is applied rather than remembered.
   */
  const readRows = useCallback(
    async (markNewStrikes = false) => {
      if (!preview || !draft) {
        return
      }
      try {
        const result = await importApi.readAllRows(preview.token, draft)
        const before = lastRead.current
        setJustStruck(markNewStrikes && before ? newlyStruck(before, result.rows) : new Set())
        lastRead.current = result.rows
        setRows(result)
        setRowsError(null)
      } catch {
        lastRead.current = null
        setRows(null)
        setRowsError(
          'The uploaded file is no longer held for this session. Choose the file again to carry on.',
        )
      }
    },
    [preview, draft],
  )

  useEffect(() => {
    if (step === 'preview') {
      void readRows()
    }
  }, [step, readRows])

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
      setResult(null)
      setImportError(null)
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

  // The fifth import from the same bank: the mapping on screen is the one already saved, so the
  // next step is to read the file, not to save what is saved. Import saves it again anyway.
  const remembered =
    preview !== null &&
    draft !== null &&
    preview.saved !== null &&
    preview.missingColumns.length === 0 &&
    sameMapping(draft, draftFrom(preview))

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

  /** Station 2 settles the mapping; station 3 is the same reading carrying the whole file. */
  function goToPreview() {
    setSaveError(null)
    if (requirement) {
      setSaveError(requirement)
      return
    }
    if (readingError) {
      setSaveError(readingError)
      return
    }
    lastRead.current = null
    setRows(null)
    setRowsError(null)
    setJustStruck(new Set())
    setSlipRow(null)
    setImportError(null)
    setStep('preview')
  }

  function startOver() {
    setStep('upload')
    setFile(null)
    setSavedAt(null)
    setSaveError(null)
    lastRead.current = null
    setRows(null)
    setRowsError(null)
    setSlipRow(null)
    setResult(null)
    setImportError(null)
  }

  /**
   * The import reads the file through the account's saved mapping, so the mapping on screen is
   * saved first: what the household previewed is exactly what is written, and the next export from
   * this bank arrives already mapped. No confirmation dialog — the button names what it will do.
   */
  async function handleImport() {
    if (!preview || !draft) {
      return
    }
    setImportError(null)
    setImporting(true)
    try {
      try {
        await importApi.saveMapping(preview.accountId, draft)
      } catch {
        setImportError('The mapping could not be saved, so nothing was imported. Try again.')
        return
      }
      const outcome = await importApi.confirm(preview.token)
      // Counted after the import rather than taken from it, so rows left unfiled by an earlier
      // import of this account are in the figure too — they are in the list the memo opens.
      const queue = await transactionsApi
        .fetchAll({ accountId: preview.accountId, uncategorized: true })
        .catch(() => null)
      setWaiting(queue?.length ?? outcome.imported)
      setResult(outcome)
      setSlipRow(null)
    } catch (error) {
      // The server's own sentence when it refused; a plain one when it could not be reached at all.
      setImportError(
        error instanceof ApiError
          ? error.message
          : 'The import could not reach the server, so nothing was written. Try again.',
      )
    } finally {
      setImporting(false)
    }
  }

  /** No confirmation line: the row striking itself and the tally re-counting is the confirmation. */
  async function handleRuleSaved() {
    setFocusRow(slipRow)
    setSlipRow(null)
    await readRows(true)
  }

  return (
    <>
      <hgroup>
        <h1>Import</h1>
        <p>Read a CSV export from the bank, and remember how that account's export is laid out.</p>
      </hgroup>

      {/* The only way to the rules page, so it is on the page at every step — and before there is an
          account to import into — rather than buried in one step's notes. */}
      <p className="memo">
        What an import does to your rows — which to classify, and which to skip — is set on the{' '}
        <Link to="/import/rules">Import rules</Link> page.
      </p>

      <ol className="stations" aria-label="Import steps">
        <Station state={step === 'upload' ? 'current' : 'done'}>Upload</Station>
        <Station state={stationState(step, 'map')}>Map columns</Station>
        {/* Once the file is written there is no station left to be at: all three are settled. */}
        <Station state={result !== null ? 'done' : stationState(step, 'preview')}>Preview &amp; confirm</Station>
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
          <StepHeading ref={stepHeading}>Upload</StepHeading>
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

          <p className="memo">
            Rows you never want — autopay confirmations, the transfer to savings — are struck out in the
            preview by an ignore rule. You can also write one from the row that provoked it, once the
            preview is on screen.
          </p>
        </form>
      ) : !preview || !draft ? null : step === 'map' ? (
        <form onSubmit={handleSave}>
          <StepHeading ref={stepHeading}>Map columns</StepHeading>
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
                  {row.error ? <p className="reading-error">{row.error}</p> : <ReadingLine row={row} />}
                </div>
              ))
            )}
          </section>

          {savedAt && (
            <p className="note" role="status">
              Mapping saved for <strong>{selectedAccount?.name ?? 'this account'}</strong>. The next export from
              this account arrives already mapped.
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
            <button type="button" className="secondary" onClick={startOver}>
              Choose another file
            </button>
          </div>
        </form>
      ) : (
        <>
        <StepHeading ref={stepHeading}>Preview &amp; confirm</StepHeading>
        <PreviewStation
          accounts={accounts}
          accountId={preview.accountId}
          fileName={preview.fileName}
          rows={rows}
          rowsError={rowsError}
          justStruck={justStruck}
          slipRow={slipRow}
          importing={importing}
          importError={importError}
          result={result}
          waiting={waiting}
          onOpenSlip={setSlipRow}
          onRuleSaved={handleRuleSaved}
          onImport={handleImport}
          focusRow={focusRow}
          onFocused={() => setFocusRow(null)}
          onBack={() => setStep('map')}
          onStartOver={startOver}
        />
        </>
      )}
    </>
  )
}

/** A station on the band. The current one says so to assistive technology, not only in ink. */
function Station({ state, children }: { state: 'done' | 'current' | 'ahead'; children: ReactNode }) {
  return (
    <li className="station" data-state={state} aria-current={state === 'current' ? 'step' : undefined}>
      {children}
    </li>
  )
}

/**
 * The step's name as a heading, for the document outline and as the place focus lands. Hidden
 * because the station band already says it in ink, one rule above.
 */
function StepHeading({ ref, children }: { ref: Ref<HTMLHeadingElement>; children: ReactNode }) {
  return (
    <h2 className="visually-hidden" tabIndex={-1} ref={ref}>
      {children}
    </h2>
  )
}

/** Which mark the station band carries: done behind you, current where you are, ahead of you. */
function stationState(step: Step, station: Step): 'done' | 'current' | 'ahead' {
  const order: Step[] = ['upload', 'map', 'preview']
  const here = order.indexOf(step)
  const there = order.indexOf(station)
  return here === there ? 'current' : here > there ? 'done' : 'ahead'
}

/**
 * One row as the app would store it: date, description, amount. The same three figures at both
 * stations — the Map step's rehearsal and station 3's performance — so a row cannot read one way
 * while it is being mapped and another way when it is being confirmed.
 */
function ReadingLine({ row }: { row: { date: string | null; description: string | null; amount: number | null } }) {
  return (
    <>
      <span className="reading-date">
        {row.date ? readableDate(row.date) : <span className="reading-missing">no date</span>}
      </span>
      <span className="reading-description">
        {row.description ?? <span className="reading-missing">no description</span>}
      </span>
      <span className="reading-amount">
        {row.amount === null ? <span className="reading-missing">no amount</span> : row.amount.toFixed(2)}
      </span>
    </>
  )
}

/** Rows that were not struck the last time the file was read, and are now. */
function newlyStruck(before: PreviewRow[], after: PreviewRow[]): ReadonlySet<number> {
  const struck = new Set<number>()
  after.forEach((row, index) => {
    if (row.skippedReason !== null && before[index]?.skippedReason == null) {
      struck.add(index)
    }
  })
  return struck
}

/**
 * Station 3: the whole file, read as the app would store it. The same block the Map step prints
 * under its double rule, carrying every row — and, under the rule, the counts that are the point of
 * the screen, closed by the one action that writes them. After the import the same close prints
 * what was written.
 */
function PreviewStation({
  accounts,
  accountId,
  fileName,
  rows,
  rowsError,
  justStruck,
  slipRow,
  importing,
  importError,
  result,
  waiting,
  focusRow,
  onFocused,
  onOpenSlip,
  onRuleSaved,
  onImport,
  onBack,
  onStartOver,
}: {
  accounts: Account[]
  accountId: number
  fileName: string
  rows: FullPreview | null
  rowsError: string | null
  justStruck: ReadonlySet<number>
  slipRow: number | null
  importing: boolean
  importError: string | null
  result: ImportResult | null
  waiting: number
  focusRow: number | null
  onFocused: () => void
  onOpenSlip: (index: number | null) => void
  onRuleSaved: () => void
  onImport: () => void
  onBack: () => void
  onStartOver: () => void
}) {
  const readingRef = useRef<HTMLElement>(null)

  // After a rule is written the slip closes under the cursor; focus goes back to the row it was
  // written from, which now carries its strike and the rule's name.
  useEffect(() => {
    if (focusRow === null || rows === null) {
      return
    }
    readingRef.current?.querySelector<HTMLElement>(`[data-entry="${focusRow}"]`)?.focus()
    onFocused()
  }, [focusRow, rows, onFocused])

  if (rowsError) {
    return (
      <>
        <p role="alert">{rowsError}</p>
        <div className="step-actions">
          <button type="button" onClick={onStartOver}>
            Choose another file
          </button>
        </div>
      </>
    )
  }

  if (rows === null) {
    return <p aria-busy="true">Reading the whole file…</p>
  }

  const reasons = [...new Set(rows.rows.map((row) => row.skippedReason).filter((r) => r !== null))]
  // Once the file is written it is no longer held, so there is nothing left to write a rule against.
  const imported = result !== null

  return (
    <>
      <section className="reading" aria-label="Preview" ref={readingRef}>
        <p className="reading-head">
          {imported ? 'Every row, as it was read' : 'Every row, as it will be read'}
          <span className="reading-file">{fileName}</span>
        </p>

        {rows.rows.map((row, index) => (
          <Fragment key={index}>
            <div
              className="preview-entry"
              data-entry={index}
              tabIndex={-1}
              data-skipped={row.skippedReason !== null}
              data-duplicate={row.isDuplicate}
              data-strike={justStruck.has(index) ? 'draw' : undefined}
            >
              <div className="preview-line">
                <ReadingLine row={row} />
                {/* Every row still going in carries this, including one that failed to parse: its
                    description is perfectly readable, and it is exactly the kind of row a rule
                    gets written from. A duplicate is leaving already, so a rule would change nothing. */}
                {!imported && row.skippedReason === null && !row.isDuplicate && (
                  <button
                    type="button"
                    className="preview-rule-open"
                    aria-expanded={slipRow === index}
                    // Every row carries the same words; the row's own description tells them apart.
                    aria-label={`Ignore rows like this: ${row.description ?? 'this row'}`}
                    onClick={() => onOpenSlip(slipRow === index ? null : index)}
                  >
                    Ignore rows like this
                  </button>
                )}
              </div>

              {/* Real text, not an ARIA label: the reason is read by everybody, and it names the
                  rule that caught the row so an over-broad one is diagnosable at a glance. */}
              {row.skippedReason !== null && <p className="preview-skip">Skipped · {row.skippedReason}</p>}
              {/* Not struck: the pen stroke means a rule caught the row, and no rule did. The row is
                  already in the ledger, and says so in the same register. */}
              {row.isDuplicate && <p className="preview-skip">Already imported</p>}
              {row.error !== null && <p className="preview-problem">{row.error}</p>}
            </div>

            {!imported && slipRow === index && (
              <div className="preview-slip">
                <div className="correction-slip">
                  <div className="correction-head">
                    <h3>Ignore rows like this</h3>
                    <p>{row.description ?? 'this row'}</p>
                  </div>
                  <IgnoreRuleForm
                    accounts={accounts}
                    // Pre-filled from the description the row is showing: matching against anything
                    // else would strike a row for text the household never saw.
                    initialMatchText={row.description ?? ''}
                    initialAccountId={accountId}
                    submitLabel="Add rule"
                    onSaved={onRuleSaved}
                    onCancel={() => onOpenSlip(null)}
                  />
                </div>
              </div>
            )}
          </Fragment>
        ))}
      </section>

      {result !== null ? (
        <ImportedClose
          result={result}
          waiting={waiting}
          accountId={accountId}
          accountName={accounts.find((a) => a.id === accountId)?.name ?? 'this account'}
          onStartOver={onStartOver}
        />
      ) : (
        <>
          {/* Entries, the way a ledger closes a figure. Every row that goes in arrives with no
              category, so all of them are for review until assignment rules can categorize a row. */}
          <div className="exceptions">
            <div className="exception">
              <span className="exception-label">For review</span>
              <span className="exception-count num">{rows.willImportCount}</span>
            </div>
            <div className="exception">
              <span className="exception-label">Skipped by a rule</span>
              <span className="exception-count num">{rows.skippedCount}</span>
            </div>
            <div className="exception">
              <span className="exception-label">Already imported</span>
              <span className="exception-count num">{rows.duplicateCount}</span>
            </div>
          </div>

          {/* The guess, made in the open: nothing has filed these rows, so they arrive with no
              category and counted as a Want until a person says otherwise (ADR-0001). */}
          {rows.willImportCount > 0 && (
            <p className="close-note">
              {rows.willImportCount === 1 ? 'It lands' : 'Each lands'} with no category, as a Want, until someone files{' '}
              {rows.willImportCount === 1 ? 'it' : 'them'}.
            </p>
          )}

          {/* One sentence for assistive technology, rather than three figures each announced alone
              as a rule re-counts them. */}
          <p className="visually-hidden" aria-live="polite">
            {rows.willImportCount} for review, {rows.skippedCount} skipped by a rule, {rows.duplicateCount} already
            imported.
          </p>

          {/* Unreadable rows are named in the margin rather than given a count of their own: they
              are a problem to fix at the mapping, not a figure the household is being asked to trust. */}
          {rows.errorCount > 0 && (
            <p className="note" data-signal="true">
              <strong>
                {rows.errorCount} {rows.errorCount === 1 ? 'row' : 'rows'}
              </strong>{' '}
              could not be read, so nothing can be imported yet. Go back to the mapping and check the date
              format and the column roles, or skip {rows.errorCount === 1 ? 'it' : 'them'} with a rule.
            </p>
          )}

          {rows.willImportCount === 0 && rows.skippedCount > 0 && rows.duplicateCount === 0 && (
            <p className="note" data-signal="true" role="alert">
              Nothing is left to import. A rule is almost certainly catching too much —{' '}
              <strong>{reasons.join(', ')}</strong>. Loosen or delete it on the{' '}
              <Link to="/import/rules">Import rules</Link> page.
            </p>
          )}

          {rows.willImportCount === 0 && rows.duplicateCount > 0 && rows.errorCount === 0 && (
            <p className="note">
              Everything in this file is already in the ledger or skipped by a rule. There is nothing new
              to import.
            </p>
          )}

          {importError && (
            <p className="field-error" role="alert">
              {importError}
            </p>
          )}

          <div className="step-actions">
            <button
              type="button"
              onClick={onImport}
              disabled={importing || rows.errorCount > 0 || rows.willImportCount === 0}
              aria-busy={importing || undefined}
            >
              {importing
                ? 'Importing…'
                : `Import ${rows.willImportCount} ${rows.willImportCount === 1 ? 'row' : 'rows'}`}
            </button>
            <button type="button" className="secondary" onClick={onBack}>
              Back to the mapping
            </button>
            <button type="button" className="secondary" onClick={onStartOver}>
              Choose another file
            </button>
          </div>
        </>
      )}
    </>
  )
}

/**
 * The same close, after the import: what was written and what was left out, then the work it made.
 * The review queue is the handoff from import to the monthly ritual, so it is the primary action
 * here — and when nothing is waiting, it does not print at all.
 */
function ImportedClose({
  result,
  waiting,
  accountId,
  accountName,
  onStartOver,
}: {
  result: ImportResult
  waiting: number
  accountId: number
  accountName: string
  onStartOver: () => void
}) {
  // The result replaces the button that was just pressed, so focus moves to what it says.
  const resultRef = useRef<HTMLParagraphElement>(null)
  useEffect(() => {
    resultRef.current?.focus()
  }, [])

  return (
    <>
      <p className="exceptions import-result" role="status" tabIndex={-1} ref={resultRef}>
        Imported <span className="num">{result.imported}</span> · skipped{' '}
        <span className="num">{result.skippedIgnored}</span> by a rule · skipped{' '}
        <span className="num">{result.skippedDuplicate}</span> as duplicates
      </p>

      {/* The account's whole queue, not just this file's rows: it is the list the action opens, and
          rows an earlier import left unfiled are waiting in it too. */}
      {waiting > 0 && (
        <p className="memo">
          <strong>{waiting}</strong> {waiting === 1 ? 'transaction' : 'transactions'} on {accountName}{' '}
          {waiting === 1 ? 'is' : 'are'} waiting for a category.
        </p>
      )}

      <div className="step-actions">
        {waiting > 0 && (
          <Link className="button" to={`/transactions?uncategorized=true&accountId=${accountId}`}>
            Review {waiting} {waiting === 1 ? 'transaction' : 'transactions'}
          </Link>
        )}
        <button type="button" className={waiting > 0 ? 'secondary' : undefined} onClick={onStartOver}>
          Import another file
        </button>
      </div>
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

function sameMapping(a: MappingDraft, b: MappingDraft): boolean {
  return (
    a.delimiter === b.delimiter &&
    a.hasHeaderRow === b.hasHeaderRow &&
    a.dateFormat === b.dateFormat &&
    ROLE_KEYS.every((key) => a[key] === b[key])
  )
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
