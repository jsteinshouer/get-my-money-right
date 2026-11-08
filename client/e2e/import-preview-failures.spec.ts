import { test, expect, type Page } from '@playwright/test'

const CARD_CSV = [
  'Posted Date,Reference,Payee,Amount',
  '08/14/2026,7701,KROGER #442,-84.19',
  '08/15/2026,7702,SHELL OIL 5578,-41.02',
  '08/15/2026,7703,AUTOPAY THANK YOU,312.00',
].join('\r\n')

async function logIn(page: Page) {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()
}

async function addAccount(page: Page, name: string) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible()
  await page.getByLabel('Name').fill(name)
  await page.getByLabel('Type').selectOption('CreditCard')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: name })).toBeVisible()
}

/** Upload the export and arrive at the Map step. */
async function upload(page: Page, accountName: string) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await expect(page.getByRole('heading', { name: 'Import', exact: true })).toBeVisible()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page
    .getByLabel('CSV file')
    .setInputFiles({ name: 'august.csv', mimeType: 'text/csv', buffer: Buffer.from(CARD_CSV, 'utf-8') })
  await page.getByRole('button', { name: 'Read the file' }).click()
}

async function mapAndPreview(page: Page) {
  await page.getByLabel('Role for column Posted Date').selectOption('date')
  await page.getByLabel('Role for column Payee').selectOption('description')
  await page.getByLabel('Role for column Amount').selectOption('amount')
  await page.getByRole('button', { name: 'Preview all rows' }).click()
  await expect(page.getByRole('region', { name: 'Preview' })).toBeVisible()
}

function entry(page: Page, description: string) {
  return page.locator('.preview-entry', { hasText: description })
}

const READ_ALL_ROWS = '**/api/import/previews/*/rows'

test('a re-read that fails keeps the last reading on screen, and trying again reads the file', async ({ page }) => {
  await logIn(page)
  const accountName = `E2E Reread ${Date.now()}`
  await addAccount(page, accountName)
  await upload(page, accountName)
  await mapAndPreview(page)

  // The server faults on the re-read a new rule sets off — the file is still held.
  await page.route(READ_ALL_ROWS, (route) => route.fulfill({ status: 500 }))
  await entry(page, 'AUTOPAY THANK YOU').getByRole('button', { name: 'Ignore rows like this' }).click()
  await page.getByLabel('These words').fill('AUTOPAY')
  await page.getByRole('button', { name: 'Add rule' }).click()

  // Said for what it is, over the rows as they last read — nobody is sent back for the file.
  await expect(page.getByRole('alert')).toContainText(
    'The file could not be read just now. Try again. The rows below are as the file read last time.',
  )
  await expect(entry(page, 'KROGER #442')).toBeVisible()
  await expect(entry(page, 'AUTOPAY THANK YOU')).not.toContainText('Skipped')
  await expect(page.getByRole('button', { name: 'Choose another file' })).toBeVisible()

  await page.unroute(READ_ALL_ROWS)
  await page.getByRole('button', { name: 'Try again', exact: true }).click()

  // The rule saved before the fault applies once the file reads again.
  await expect(entry(page, 'AUTOPAY THANK YOU')).toContainText('Skipped · contains AUTOPAY')
  await expect(page.getByText('as the file read last time')).toHaveCount(0)
})

test('an upload that is no longer held says why, and offers only a new file', async ({ page }) => {
  await logIn(page)
  const accountName = `E2E Gone ${Date.now()}`
  await addAccount(page, accountName)
  await upload(page, accountName)
  await mapAndPreview(page)

  await page.route(READ_ALL_ROWS, (route) => route.fulfill({ status: 404 }))
  await page.getByRole('button', { name: 'Back to the mapping' }).click()
  await page.getByRole('button', { name: 'Preview all rows' }).click()

  await expect(page.getByRole('alert')).toHaveText(
    'This upload is no longer held — it expired, or you have uploaded a newer file for this account since. Choose the file again to carry on.',
  )
  await expect(page.getByRole('region', { name: 'Preview' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Try again' })).toHaveCount(0)
  await page.getByRole('button', { name: 'Choose another file' }).click()
  await expect(page.getByLabel('CSV file')).toBeVisible()
})

test('the Map step names an unreachable app, and trying again reads the sample', async ({ page }) => {
  await logIn(page)
  const accountName = `E2E Unreachable ${Date.now()}`
  await addAccount(page, accountName)

  const READING = '**/api/import/previews/*/reading'
  await page.route(READING, (route) => route.abort('connectionrefused'))
  await upload(page, accountName)

  const readsAs = page.getByRole('region', { name: 'Reads as' })
  await expect(readsAs.getByRole('alert')).toContainText(
    'The app could not be reached. Check the connection, then try again.',
  )

  await page.unroute(READING)
  await readsAs.getByRole('button', { name: 'Try again' }).click()
  await expect(readsAs.getByRole('alert')).toHaveCount(0)
  await expect(readsAs.locator('.reading-row')).toHaveCount(3)
})
