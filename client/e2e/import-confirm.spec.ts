import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { test, expect, type Page } from '@playwright/test'

/** Real exports on disk, read the way a household member would pick them from Downloads. */
const FIXTURES = path.join(path.dirname(fileURLToPath(import.meta.url)), 'fixtures')

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

/** Upload a fixture and carry on to station 3, mapping the columns only the first time. */
async function previewFixture(page: Page, accountName: string, fileName: string, { map }: { map: boolean }) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await expect(page.getByRole('heading', { name: 'Import' })).toBeVisible()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page.getByLabel('CSV file').setInputFiles(path.join(FIXTURES, fileName))
  await page.getByRole('button', { name: 'Read the file' }).click()

  if (map) {
    await page.getByLabel('Role for column Posted Date').selectOption('date')
    await page.getByLabel('Role for column Payee').selectOption('description')
    await page.getByLabel('Role for column Amount').selectOption('amount')
  } else {
    // The import saved the mapping, so the next export from this account arrives already mapped.
    await expect(page.getByText('Remembered from the last import')).toBeVisible()
  }

  await page.getByRole('button', { name: 'Preview all rows' }).click()
  await expect(page.getByRole('region', { name: 'Preview' })).toBeVisible()
}

async function expectTally(page: Page, forReview: number, skipped: number, duplicates: number) {
  const tally = page.locator('.exceptions')
  await expect(tally.locator('.exception', { hasText: 'For review' })).toContainText(String(forReview))
  await expect(tally.locator('.exception', { hasText: 'Skipped by a rule' })).toContainText(String(skipped))
  await expect(tally.locator('.exception', { hasText: 'Already imported' })).toContainText(String(duplicates))
}

function entry(page: Page, description: string) {
  return page.locator('.preview-entry', { hasText: description })
}

test('importing a statement writes its rows uncategorized, and re-importing it skips what is already there', async ({
  page,
}) => {
  const accountName = `E2E Confirm ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)
  await previewFixture(page, accountName, 'card-august.csv', { map: true })

  // The autopay is noise: strike it with a rule written from its own row.
  await entry(page, 'AUTOPAY THANK YOU').getByRole('button', { name: 'Ignore rows like this' }).click()
  await page.getByLabel('Match').selectOption('Contains')
  await page.getByLabel('These words').fill('AUTOPAY')
  await page.getByRole('button', { name: 'Add rule' }).click()
  await expect(entry(page, 'AUTOPAY THANK YOU')).toContainText('Skipped · contains AUTOPAY')
  await expectTally(page, 3, 1, 0)
  // The guess is said before it is made: nothing files these rows, so they arrive as Wants.
  await expect(page.getByText('Each lands with no category, as a Want, until someone files them.')).toBeVisible()
  // Focus returns to the row the rule was written from, now carrying its strike.
  await expect(entry(page, 'AUTOPAY THANK YOU')).toBeFocused()

  // The button names what it will write, so there is nothing to reconcile and no dialog.
  await page.getByRole('button', { name: 'Import 3 rows' }).click()

  await expect(page.getByRole('status')).toHaveText('Imported 3 · skipped 1 by a rule · skipped 0 as duplicates')
  // Focus follows the import to what it says, rather than falling back to the top of the page.
  await expect(page.getByRole('status')).toBeFocused()
  await expect(page.getByRole('list', { name: 'Import steps' }).locator('[aria-current="step"]')).toHaveCount(0)
  await expect(page.getByText(`3 transactions on ${accountName} are waiting for a category.`)).toBeVisible()
  await page.getByRole('link', { name: 'Review 3 transactions' }).click()

  // The memo opens the review queue for this account: the three rows, filed nowhere, guessed Want.
  await expect(page.getByRole('heading', { name: 'Transactions' })).toBeVisible()
  await expect(page.getByLabel('Filter by category')).toHaveValue('none')
  const rows = page.locator('tbody tr')
  await expect(rows).toHaveCount(3)
  for (const description of ['KROGER #442', 'SHELL OIL 5578', 'BOOKS, ETC']) {
    const row = rows.filter({ hasText: description })
    await expect(row.getByLabel(`Category for ${description}`)).toHaveValue('')
    await expect(row.getByLabel(`Category for ${description}`)).toContainText('No category')
    await expect(row.getByLabel(`Need or Want for ${description}`)).toHaveValue('Want')
  }
  await expect(rows.filter({ hasText: 'SHELL OIL 5578' })).toContainText('-41.02')
  await expect(page.getByText('AUTOPAY THANK YOU')).toHaveCount(0)

  // A re-export overlapping the first: two rows are already in the ledger, one is new.
  await previewFixture(page, accountName, 'card-august-reexport.csv', { map: false })
  await expect(entry(page, 'SHELL OIL 5578')).toContainText('Already imported')
  await expect(entry(page, 'BOOKS, ETC')).toContainText('Already imported')
  await expect(entry(page, 'BLUE BOTTLE COFFEE')).not.toContainText('Already imported')
  await expectTally(page, 1, 0, 2)

  await page.getByRole('button', { name: 'Import 1 row' }).click()
  await expect(page.getByRole('status')).toHaveText('Imported 1 · skipped 0 by a rule · skipped 2 as duplicates')
  // The memo counts the account's whole queue — the list it opens — not just this file's rows.
  await expect(page.getByText(`4 transactions on ${accountName} are waiting for a category.`)).toBeVisible()
  await page.getByRole('link', { name: 'Review 4 transactions' }).click()

  // Four in the queue now, not six: the overlap was not written twice.
  await expect(page.locator('tbody tr')).toHaveCount(4)
  await expect(page.locator('tbody tr', { hasText: 'BLUE BOTTLE COFFEE' })).toBeVisible()
})

test('a file with a row that cannot be read cannot be imported until the row is dealt with', async ({ page }) => {
  const accountName = `E2E Unreadable ${Date.now()}`
  const csv = [
    'Posted Date,Reference,Payee,Amount',
    '08/14/2026,4471,KROGER #442,-84.19',
    '08/15/2026,4472,STATEMENT BALANCE,',
  ].join('\r\n')

  await logIn(page)
  await addAccount(page, accountName)
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page
    .getByLabel('CSV file')
    .setInputFiles({ name: 'balance-line.csv', mimeType: 'text/csv', buffer: Buffer.from(csv, 'utf-8') })
  await page.getByRole('button', { name: 'Read the file' }).click()
  await page.getByLabel('Role for column Posted Date').selectOption('date')
  await page.getByLabel('Role for column Payee').selectOption('description')
  await page.getByLabel('Role for column Amount').selectOption('amount')
  await page.getByRole('button', { name: 'Preview all rows' }).click()

  await expect(page.getByText('could not be read, so nothing can be imported yet')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Import 1 row' })).toBeDisabled()

  // A rule that skips the balance line is how a household deals with it; then the import opens.
  await entry(page, 'STATEMENT BALANCE').getByRole('button', { name: 'Ignore rows like this' }).click()
  await page.getByLabel('Match').selectOption('Equals')
  await page.getByLabel('These words').fill('STATEMENT BALANCE')
  await page.getByRole('button', { name: 'Add rule' }).click()

  await expect(page.getByRole('button', { name: 'Import 1 row' })).toBeEnabled()
  await page.getByRole('button', { name: 'Import 1 row' }).click()
  await expect(page.getByRole('status')).toHaveText('Imported 1 · skipped 1 by a rule · skipped 0 as duplicates')
})

test('the ledger spread counts imported rows as awaiting a category, and links to them', async ({ page }) => {
  const accountName = `E2E Spread ${Date.now()}`
  // Dated this month, so they land on the spread the home page opens on.
  const today = new Date()
  const day = (d: number) =>
    `${String(today.getMonth() + 1).padStart(2, '0')}/${String(d).padStart(2, '0')}/${today.getFullYear()}`
  const csv = [
    'Posted Date,Reference,Payee,Amount',
    `${day(1)},9001,FARMERS MARKET ${Date.now()},-18.00`,
    `${day(1)},9002,HARDWARE STORE ${Date.now()},-27.35`,
  ].join('\r\n')

  await logIn(page)
  // The spread only opens once the household has a category to spend against.
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Categories' }).click()
  await page.getByLabel('Name').fill(`E2E Spread Category ${Date.now()}`)
  await page.getByRole('button', { name: 'Add category' }).click()
  await page.getByRole('link', { name: 'MoneyRight' }).click()
  const awaiting = page.locator('.exception', { hasText: 'Awaiting a category' }).locator('.exception-count')
  await expect(awaiting).toBeVisible()
  const before = Number(await awaiting.textContent())

  await addAccount(page, accountName)
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page
    .getByLabel('CSV file')
    .setInputFiles({ name: 'this-month.csv', mimeType: 'text/csv', buffer: Buffer.from(csv, 'utf-8') })
  await page.getByRole('button', { name: 'Read the file' }).click()
  await page.getByLabel('Role for column Posted Date').selectOption('date')
  await page.getByLabel('Role for column Payee').selectOption('description')
  await page.getByLabel('Role for column Amount').selectOption('amount')
  await page.getByRole('button', { name: 'Preview all rows' }).click()
  await page.getByRole('button', { name: 'Import 2 rows' }).click()
  await expect(page.getByRole('status')).toHaveText('Imported 2 · skipped 0 by a rule · skipped 0 as duplicates')

  await page.getByRole('link', { name: 'MoneyRight' }).click()
  await expect(awaiting).toHaveText(String(before + 2))

  // The entry opens the month's review queue, not the month's whole ledger.
  await page.locator('.exception', { hasText: 'Awaiting a category' }).click()
  await expect(page.getByLabel('Filter by category')).toHaveValue('none')
  await expect(page.locator('tbody tr')).toHaveCount(before + 2)
})
