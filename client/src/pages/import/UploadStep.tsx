import { useState, type FormEvent, type ReactNode } from 'react'
import type { Account } from '../../api/accounts'
import { importApi, type UploadPreview } from '../../api/import'

/**
 * Station 1: which account the export came from, and the file itself. Nothing is written here —
 * the file is read and held, and the household moves on to say how it is laid out.
 */
export function UploadStep({
  heading,
  accounts,
  accountId,
  onAccountChange,
  onUploaded,
}: {
  /** The step's own heading, which the page owns so focus can land on it when the step opens. */
  heading: ReactNode
  accounts: Account[]
  accountId: string
  onAccountChange: (accountId: string) => void
  onUploaded: (preview: UploadPreview) => void
}) {
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)

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
      onUploaded(await importApi.uploadPreview(Number(accountId), file))
    } catch (error) {
      setUploadError(error instanceof Error ? error.message : 'That file could not be read as a CSV.')
    } finally {
      setUploading(false)
    }
  }

  return (
    <form onSubmit={handleUpload}>
      {heading}
      <div className="well">
        <div className="well-fields">
          <label htmlFor="import-account">
            Account
            <select
              id="import-account"
              value={accountId}
              onChange={(event) => onAccountChange(event.target.value)}
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
  )
}
