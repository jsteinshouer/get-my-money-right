import { useEffect, useRef } from 'react'
import { Link } from 'react-router-dom'
import type { FullPreview, ImportResult } from '../../api/import'

/**
 * The reading's close, before the import: the counts that are the point of the screen, closed by
 * the one action that writes them.
 */
export function PreviewClose({
  rows,
  stale,
  importing,
  importError,
  onImport,
  onBack,
  onStartOver,
}: {
  rows: FullPreview
  stale: boolean
  importing: boolean
  importError: string | null
  onImport: () => void
  onBack: () => void
  onStartOver: () => void
}) {
  const reasons = [...new Set(rows.rows.map((row) => row.skippedReason).filter((r) => r !== null))]

  return (
    <section aria-labelledby="import-close-heading">
      {/* A heading to jump to: on a long file the close is hundreds of rows below the head. */}
      <h3 id="import-close-heading" className="visually-hidden">
        Before you import
      </h3>
      {/* Entries, the way a ledger closes a figure: what the rules filed, what they left for a
          person, and what is not going in at all. */}
      <div className="exceptions">
        <div className="exception">
          <span className="exception-label">Categorized by a rule</span>
          <span className="exception-count num">{rows.categorizedCount}</span>
        </div>
        <div className="exception">
          <span className="exception-label">For review</span>
          <span className="exception-count num">{rows.forReviewCount}</span>
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

      {/* What the count means for the monthly session. Each row already says its own Need/Want,
          so the note does not repeat the guess (ADR-0001). */}
      {stale && (
        <p className="close-note">These counts are as the file read last time. Read it again before importing.</p>
      )}

      {rows.forReviewCount > 0 && (
        <p className="close-note">
          {rows.forReviewCount === 1
            ? 'It lands with no category and joins the review queue.'
            : `These ${rows.forReviewCount} rows land with no category and join the review queue.`}
        </p>
      )}

      {/* One sentence for assistive technology, rather than three figures each announced alone
          as a rule re-counts them. */}
      <p className="visually-hidden" aria-live="polite">
        {rows.categorizedCount} categorized by a rule, {rows.forReviewCount} for review, {rows.skippedCount} skipped
        by a rule, {rows.duplicateCount} already imported.
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

      {/* With nothing to write there is no import to offer — "Import 0 rows" is a button that
          can only refuse — so the next file becomes the action. */}
      <div className="step-actions">
        {rows.willImportCount === 0 && (
          <button type="button" onClick={onStartOver}>
            Choose another file
          </button>
        )}
        {rows.willImportCount > 0 && (
          <button
            type="button"
            onClick={onImport}
            disabled={importing || rows.errorCount > 0 || stale}
            aria-busy={importing || undefined}
          >
            {importing ? 'Importing…' : `Import ${rows.willImportCount} ${rows.willImportCount === 1 ? 'row' : 'rows'}`}
          </button>
        )}
        <button type="button" className="secondary" onClick={onBack}>
          Back to the mapping
        </button>
        {rows.willImportCount > 0 && (
          <button type="button" className="secondary" onClick={onStartOver}>
            Choose another file
          </button>
        )}
      </div>
    </section>
  )
}

/**
 * The same close, after the import: what was written and what was left out, then the work it made.
 * The review queue is the handoff from import to the monthly ritual, so it is the primary action
 * here — and when nothing is waiting, it does not print at all.
 */
export function ImportedClose({
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
