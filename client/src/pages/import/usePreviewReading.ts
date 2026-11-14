import { useCallback, useEffect, useRef, useState } from 'react'
import { importApi, type FullPreview, type MappingDraft, type PreviewRow } from '../../api/import'
import { readFailure, type ReadFailure } from './failures'

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
 * Station 3's reading of the whole file, and what is true about it. The file is read on arrival and
 * again after every rule written from a row, so a rule written or deleted elsewhere is applied rather
 * than remembered.
 *
 * A failed re-read keeps the last reading on screen unless the upload itself is gone — losing every
 * row to a dropped connection, just after a rule was saved from one of them, is the worst moment to
 * make the household start over — but marks it `stale`: its counts are no longer a promise the
 * import can keep.
 */
export function usePreviewReading(token: string, draft: MappingDraft) {
  const [rows, setRows] = useState<FullPreview | null>(null)
  const [failure, setFailure] = useState<ReadFailure | null>(null)
  const [rereading, setRereading] = useState(false)
  /** Rows a rule has just caught, which draw their stroke rather than arriving already struck. */
  const [justStruck, setJustStruck] = useState<ReadonlySet<number>>(new Set())
  /** The row focus goes back to once the file has re-read: the one a rule was written from. */
  const [focusRow, setFocusRow] = useState<number | null>(null)
  /** The row a rule was written from when the re-read after it failed: the failure is said there too. */
  const [rereadRow, setRereadRow] = useState<number | null>(null)
  /** What the file read as last time, so a row that has just been struck can draw its stroke. */
  const lastRead = useRef<PreviewRow[] | null>(null)

  const read = useCallback(
    async (markNewStrikes = false): Promise<boolean> => {
      setRereading(true)
      try {
        const result = await importApi.readAllRows(token, draft)
        const before = lastRead.current
        setJustStruck(markNewStrikes && before ? newlyStruck(before, result.rows) : new Set())
        lastRead.current = result.rows
        setRows(result)
        setFailure(null)
        setRereadRow(null)
        return true
      } catch (error) {
        const next = readFailure(error)
        if (next.gone) {
          lastRead.current = null
          setRows(null)
        }
        setFailure(next)
        return false
      } finally {
        setRereading(false)
      }
    },
    [token, draft],
  )

  useEffect(() => {
    void read()
  }, [read])

  /** A rule was just written from `row`: read the file through it, and keep the row if that fails. */
  const ruleSaved = useCallback(
    async (row: number | null) => {
      setFocusRow(row)
      if (!(await read(true))) {
        setRereadRow(row)
      }
    },
    [read],
  )

  /**
   * Try again. The control disappears with the failure it answered, so focus goes where the work is:
   * the row the rule was written from, or `elsewhere` when the failure was not about a rule.
   */
  const retry = useCallback(
    async (elsewhere: () => void) => {
      const row = rereadRow
      if (await read(true)) {
        if (row !== null) {
          setFocusRow(row)
        } else {
          elsewhere()
        }
      }
    },
    [read, rereadRow],
  )

  const focused = useCallback(() => setFocusRow(null), [])

  return {
    rows,
    failure,
    /** The last reading is on screen, but the file has not read since. */
    stale: failure !== null && !failure.gone && rows !== null,
    rereading,
    justStruck,
    focusRow,
    rereadRow,
    ruleSaved,
    retry,
    focused,
  }
}
