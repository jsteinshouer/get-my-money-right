import { ApiError } from '../../api/client'
import { UPLOAD_NOT_HELD } from '../../api/import'

/**
 * Why a reading of the held file failed. Only `gone` sends the household back to choose the file;
 * otherwise a Try again sits beside the message, so the message does not say it too.
 */
export interface ReadFailure {
  message: string
  gone: boolean
}

/**
 * Names the failure rather than blaming every one on an expired upload: a dropped connection or a
 * server fault leaves the file held, and the fix is to try again, not to start over.
 */
export function readFailure(error: unknown): ReadFailure {
  if (error instanceof ApiError && error.status === 404) {
    return { message: UPLOAD_NOT_HELD, gone: true }
  }
  if (error instanceof ApiError) {
    return { message: 'The file could not be read just now.', gone: false }
  }
  return { message: 'The app could not be reached. Check the connection.', gone: false }
}

/** Why an import wrote nothing, said so the household knows whether trying again is the fix. */
export function importFailure(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'The import could not reach the server, so nothing was written. Try again.'
  }
  if (error.status === 409) {
    return 'The ledger was busy saving something else, so nothing was written. Try again.'
  }
  // The server's own sentence when it refused the file — "2 rows could not be read…" — or the upload
  // being gone.
  if (error.status === 400 || error.status === 404) {
    return error.message
  }
  return 'The import failed on the server, so nothing was written. Try again.'
}
