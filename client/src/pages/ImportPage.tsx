import { useCallback, useEffect, useRef, useState, type ReactNode, type Ref } from 'react'
import { Link } from 'react-router-dom'
import { accountsApi, type Account } from '../api/accounts'
import type { MappingDraft, UploadPreview } from '../api/import'
import { MapStep } from './import/MapStep'
import { draftFrom } from './import/mapping'
import { PreviewStation } from './import/PreviewStation'
import { UploadStep } from './import/UploadStep'

type Step = 'upload' | 'map' | 'preview'

/**
 * The import, as three stations on one rule: upload the file, say how it is laid out, then read
 * every row and write them. The page holds only what crosses stations — the account, the held
 * upload and its mapping — and each station owns the rest of its own work.
 */
export function ImportPage() {
  const [accounts, setAccounts] = useState<Account[] | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [step, setStep] = useState<Step>('upload')
  const [accountId, setAccountId] = useState('')
  const [preview, setPreview] = useState<UploadPreview | null>(null)
  const [draft, setDraft] = useState<MappingDraft | null>(null)
  const [imported, setImported] = useState(false)

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

  const changeDraft = useCallback((update: (draft: MappingDraft) => MappingDraft) => {
    setDraft((current) => (current ? update(current) : current))
  }, [])

  function handleUploaded(result: UploadPreview) {
    setPreview(result)
    setDraft(draftFrom(result))
    setImported(false)
    setStep('map')
  }

  function startOver() {
    setImported(false)
    setStep('upload')
  }

  const accountName = accounts?.find((a) => String(a.id) === accountId)?.name ?? 'this account'

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
        <Station state={imported ? 'done' : stationState(step, 'preview')}>Preview &amp; confirm</Station>
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
        <UploadStep
          heading={<StepHeading ref={stepHeading}>Upload</StepHeading>}
          accounts={accounts}
          accountId={accountId}
          onAccountChange={setAccountId}
          onUploaded={handleUploaded}
        />
      ) : !preview || !draft ? null : step === 'map' ? (
        <MapStep
          heading={<StepHeading ref={stepHeading}>Map columns</StepHeading>}
          preview={preview}
          draft={draft}
          accountName={accountName}
          onDraftChange={changeDraft}
          onPreview={() => setStep('preview')}
          onStartOver={startOver}
        />
      ) : (
        <>
          <StepHeading ref={stepHeading}>Preview &amp; confirm</StepHeading>
          <PreviewStation
            accounts={accounts}
            preview={preview}
            draft={draft}
            onFocusHeading={() => stepHeading.current?.focus()}
            onImported={() => setImported(true)}
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
