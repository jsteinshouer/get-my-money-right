import { test, expect, type Page } from '@playwright/test'

const SIGNED_AMOUNT_CSV = [
  'Posted Date,Reference,Payee,Amount',
  '08/14/2026,4471,KROGER #442,-84.19',
  '08/15/2026,4472,SHELL OIL 5578,-41.02',
  '08/16/2026,4473,BLUE BOTTLE COFFEE,-6.75',
  '08/17/2026,4474,AUTOPAY THANK YOU,312.00',
  '08/18/2026,4475,CITY UTILITIES,-143.88',
].join('\r\n')

/** The same account, after the bank changed its export to a debit/credit pair. */
const DEBIT_CREDIT_CSV = [
  'Date,Description,Withdrawal,Deposit',
  '08/14/2026,GROCERY OUTLET,84.19,',
  '08/15/2026,PAYROLL DEPOSIT,,2410.55',
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

async function uploadCsv(page: Page, accountName: string, fileName: string, csv: string) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await expect(page.getByRole('heading', { name: 'Import' })).toBeVisible()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page
    .getByLabel('CSV file')
    .setInputFiles({ name: fileName, mimeType: 'text/csv', buffer: Buffer.from(csv, 'utf-8') })
  await page.getByRole('button', { name: 'Read the file' }).click()
}

test('a column mapping is saved and remembered on the next upload for the same account', async ({ page }) => {
  const accountName = `E2E Import ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)
  await uploadCsv(page, accountName, 'sapphire-august.csv', SIGNED_AMOUNT_CSV)

  // The file's own columns are what you map onto, so its header text is on screen.
  await expect(page.getByLabel('Role for column Posted Date')).toBeVisible()
  await expect(page.getByLabel('Role for column Reference')).toBeVisible()

  await page.getByLabel('Role for column Posted Date').selectOption('date')
  await page.getByLabel('Role for column Payee').selectOption('description')
  await page.getByLabel('Role for column Amount').selectOption('amount')

  // The proof: the file's first rows re-read as the app would store them.
  const reading = page.getByRole('region', { name: 'Reads as' })
  await expect(reading.getByText('Aug 14, 2026')).toBeVisible()
  await expect(reading.getByText('KROGER #442')).toBeVisible()
  await expect(reading.getByText('-84.19')).toBeVisible()

  await page.getByRole('button', { name: 'Save mapping' }).click()
  await expect(page.getByText('Mapping saved for')).toBeVisible()

  // Upload the same shape again: the mapping should arrive already made.
  await page.getByRole('button', { name: 'Choose another file' }).click()
  await uploadCsv(page, accountName, 'sapphire-september.csv', SIGNED_AMOUNT_CSV)

  await expect(page.getByText('Remembered from the last import for')).toBeVisible()
  await expect(page.getByLabel('Role for column Posted Date')).toHaveValue('date')
  await expect(page.getByLabel('Role for column Payee')).toHaveValue('description')
  await expect(page.getByLabel('Role for column Amount')).toHaveValue('amount')
  await expect(page.getByLabel('Role for column Reference')).toHaveValue('')
})

test('an export that no longer has the remembered columns names what went missing', async ({ page }) => {
  const accountName = `E2E Drift ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)
  await uploadCsv(page, accountName, 'first-export.csv', SIGNED_AMOUNT_CSV)

  await page.getByLabel('Role for column Posted Date').selectOption('date')
  await page.getByLabel('Role for column Payee').selectOption('description')
  await page.getByLabel('Role for column Amount').selectOption('amount')
  await page.getByRole('button', { name: 'Save mapping' }).click()
  await expect(page.getByText('Mapping saved for')).toBeVisible()

  // The bank switches to separate withdrawal/deposit columns: every remembered column is gone.
  await page.getByRole('button', { name: 'Choose another file' }).click()
  await uploadCsv(page, accountName, 'second-export.csv', DEBIT_CREDIT_CSV)

  await expect(page.getByText('This export no longer has')).toBeVisible()
  await expect(page.getByRole('alert')).toContainText('"Posted Date"')
  await expect(page.getByLabel('Role for column Date')).toHaveValue('')

  // Mapped as a debit/credit pair, a withdrawal reads as money out.
  await page.getByLabel('Role for column Date').selectOption('date')
  await page.getByLabel('Role for column Description').selectOption('description')
  await page.getByLabel('Role for column Withdrawal').selectOption('debit')
  await page.getByLabel('Role for column Deposit').selectOption('credit')

  const reading = page.getByRole('region', { name: 'Reads as' })
  await expect(reading.getByText('-84.19')).toBeVisible()
  await expect(reading.getByText('2410.55')).toBeVisible()
})
