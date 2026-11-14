import { Fragment, useEffect, useRef, useState } from 'react'
import type { Account } from '../../api/accounts'
import { filedAs, importApi, type ImportResult, type MappingDraft, type SecondLine, type UploadPreview } from '../../api/import'
import { transactionsApi } from '../../api/transactions'
import { IgnoreRuleForm } from '../../components/IgnoreRuleForm'
import { importFailure } from './failures'
import { ImportedClose, PreviewClose } from './PreviewClose'
import { ReadingLine } from './ReadingLine'
import { usePreviewReading } from './usePreviewReading'

/**
 * Station 3: the whole file, read as the app would store it. The same block the Map step prints
 * under its double rule, carrying every row — and, under the rule, the counts that are the point of
 * the screen, closed by the one action that writes them. After the import the same close prints
 * what was written.
 */
export function PreviewStation({
  accounts,
  preview,
  draft,
  onFocusHeading,
  onImported,
  onBack,
  onStartOver,
}: {
  accounts: Account[]
  preview: UploadPreview
  draft: MappingDraft
  /** Where focus goes when a Try again answered a failure that was not about a rule. */
  onFocusHeading: () => void
  onImported: () => void
  onBack: () => void
  onStartOver: () => void
}) {
  const { accountId, fileName } = preview
  const { rows, failure, stale, rereading, justStruck, focusRow, rereadRow, ruleSaved, retry, focused } =
    usePreviewReading(preview.token, draft)

  const [slipRow, setSlipRow] = useState<number | null>(null)
  const [importing, setImporting] = useState(false)
  const [importError, setImportError] = useState<string | null>(null)
  const [result, setResult] = useState<ImportResult | null>(null)
  /** The account's review queue after the import: the count the memo's link actually opens. */
  const [waiting, setWaiting] = useState(0)

  const readingRef = useRef<HTMLElement>(null)

  // After a rule is written the slip closes under the cursor; focus goes back to the row it was
  // written from, which now carries its strike and the rule's name.
  useEffect(() => {
    if (focusRow === null || rows === null) {
      return
    }
    readingRef.current?.querySelector<HTMLElement>(`[data-entry="${focusRow}"]`)?.focus()
    focused()
  }, [focusRow, rows, focused])

  /** No confirmation line: the row striking itself and the tally re-counting is the confirmation. */
  function handleRuleSaved() {
    const row = slipRow
    setSlipRow(null)
    void ruleSaved(row)
  }

  const onRetry = () => void retry(onFocusHeading)

  /**
   * The import reads the file through the account's saved mapping, so the mapping on screen is
   * saved first: what the household previewed is exactly what is written, and the next export from
   * this bank arrives already mapped. No confirmation dialog — the button names what it will do.
   */
  async function handleImport() {
    setImportError(null)
    setImporting(true)
    try {
      try {
        await importApi.saveMapping(accountId, draft)
      } catch {
        setImportError('The mapping could not be saved, so nothing was imported. Try again.')
        return
      }
      const outcome = await importApi.confirm(preview.token)
      // Counted after the import rather than taken from it, so rows left unfiled by an earlier
      // import of this account are in the figure too — they are in the list the memo opens.
      const queue = await transactionsApi.fetchAll({ accountId, uncategorized: true }).catch(() => null)
      // Failing that, this file's own rows for review: the classified ones are not waiting.
      setWaiting(queue?.length ?? rows?.forReviewCount ?? outcome.imported)
      setResult(outcome)
      setSlipRow(null)
      onImported()
    } catch (error) {
      setImportError(importFailure(error))
    } finally {
      setImporting(false)
    }
  }

  // With nothing to show — the first reading failed, or the upload is gone — the failure is the
  // whole station. Otherwise it is a note over the last reading, which stays where it was.
  if (failure && !stale) {
    return (
      <>
        <p role="alert">{failure.message}</p>
        <div className="step-actions">
          {!failure.gone && (
            <button type="button" onClick={onRetry} disabled={rereading} aria-busy={rereading || undefined}>
              Try again
            </button>
          )}
          <button type="button" className={failure.gone ? undefined : 'secondary'} onClick={onStartOver}>
            Choose another file
          </button>
        </div>
      </>
    )
  }

  if (rows === null) {
    return <p aria-busy="true">Reading the whole file…</p>
  }

  // Once the file is written it is no longer held, so there is nothing left to write a rule against.
  const imported = result !== null

  const tryAgain = (
    <button type="button" className="linklike" onClick={onRetry} disabled={rereading} aria-busy={rereading || undefined}>
      Try again
    </button>
  )

  return (
    <>
      {stale && (
        <p className="note" data-signal="true" role="alert">
          {failure?.message} The rows and counts below are as the file read last time. {tryAgain}
        </p>
      )}

      <section className="reading" aria-label="Preview" ref={readingRef}>
        <p className="reading-head">
          {imported ? 'Every row, as it was read' : 'Every row, as it will be read'}
          <span className="reading-file">{fileName}</span>
          {/* The tally, said once where the reading starts: a long file's counts are otherwise
              hundreds of rows below, and a re-import should need no scrolling to trust. */}
          {!imported && (
            <span className="reading-count">
              {stale && 'As last read: '}
              {rows.categorizedCount} categorized by a rule · {rows.forReviewCount} for review · {rows.skippedCount}{' '}
              skipped by a rule · {rows.duplicateCount} already imported
            </span>
          )}
        </p>

        {rows.rows.map((row, index) => {
          const filed = filedAs(row)
          return (
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
                      onClick={() => setSlipRow(slipRow === index ? null : index)}
                    >
                      Ignore rows like this
                    </button>
                  )}
                </div>

                {/* Real text, not an ARIA label: the reason is read by everybody, and it names the
                    rule that caught the row so an over-broad one is diagnosable at a glance. */}
                {row.skippedReason !== null && (
                  <SecondLineText line={{ marks: ['Skipped'], notes: [row.skippedReason] }} />
                )}
                {/* How a row going in will be filed, and the rule that decided it — in the strike
                    reason's register, real text for everybody. A row nobody has taught the app about
                    says so rather than staying bare: its Want is a guess, made in the open. */}
                {filed !== null && <SecondLineText line={filed} filed />}
                {/* Not struck: the pen stroke means a rule caught the row, and no rule did. The row is
                    already in the ledger, and says so in the same register. */}
                {row.isDuplicate && <SecondLineText line={{ marks: ['Already imported'], notes: [] }} />}
                {row.error !== null && <p className="preview-problem">{row.error}</p>}
                {/* Said where the rule was written from, as well as over the reading: focus comes
                    back to this row, and on a long file the note above is far out of sight. */}
                {stale && rereadRow === index && (
                  <p className="preview-problem">
                    The rule is saved, but the file has not been read through it yet. {failure?.message} {tryAgain}
                  </p>
                )}
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
                      onSaved={handleRuleSaved}
                      onCancel={() => setSlipRow(null)}
                    />
                  </div>
                </div>
              )}
            </Fragment>
          )
        })}
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
        <PreviewClose
          rows={rows}
          stale={stale}
          importing={importing}
          importError={importError}
          onImport={() => void handleImport()}
          onBack={onBack}
          onStartOver={onStartOver}
        />
      )}
    </>
  )
}

/**
 * A row's second line: the marks in tracked caps, then the notes as written. Each part after the
 * first carries its separator, glued with a non-breaking space, so a wrapped line never ends on a
 * dangling "·" with its reason orphaned below.
 */
function SecondLineText({ line, filed = false }: { line: SecondLine; filed?: boolean }) {
  const parts = [
    ...line.marks.map((text) => ({ text, note: false })),
    ...line.notes.map((text) => ({ text, note: true })),
  ]
  return (
    <p className={filed ? 'preview-skip preview-filed' : 'preview-skip'}>
      {parts.map((part, index) => (
        <Fragment key={index}>
          {index > 0 && ' '}
          <span className={part.note ? 'preview-note' : undefined}>
            {index > 0 ? `· ${part.text}` : part.text}
          </span>
        </Fragment>
      ))}
    </p>
  )
}
