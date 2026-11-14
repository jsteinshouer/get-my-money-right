import { dateFormatLabel, dateFormats, delimiterLabel, delimiters, type MappingDraft } from '../../api/import'

/**
 * What the app worked out about the file's shape, in words rather than format strings — with the
 * controls to correct it, for the moment the reading below says the guess was wrong.
 */
export function FormatNote({
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
