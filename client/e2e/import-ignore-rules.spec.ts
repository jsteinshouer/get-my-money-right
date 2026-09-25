import { test, expect, type Page } from '@playwright/test'

/** One month's export with two rows the household never wants: an autopay and a transfer. */
const NOISY_CSV = [
  'Posted Date,Reference,Payee,Amount',
  '08/14/2026,4471,KROGER #442,-84.19',
  '08/15/2026,4472,AUTOPAY THANK YOU,312.00',
  '08/16/2026,4473,TRANSFER TO SAVINGS,-500.00',
  '08/17/2026,4474,SHELL OIL 5578,-41.02',
  "08/18/2026,4475,TRADER JOE'S #221,-63.40",
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

/** A statement that is nothing but noise, for the state where one rule catches everything. */
const ALL_NOISE_CSV = [
  'Posted Date,Reference,Payee,Amount',
  '08/14/2026,4471,POINTS REDEMPTION,12.00',
  '08/15/2026,4472,POINTS ADJUSTMENT,-3.00',
].join('\r\n')

/** Upload the file, give the columns their roles, and carry on to station 3. */
async function previewCsv(page: Page, accountName: string, csv: string = NOISY_CSV) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await expect(page.getByRole('heading', { name: 'Import' })).toBeVisible()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page
    .getByLabel('CSV file')
    .setInputFiles({ name: 'august.csv', mimeType: 'text/csv', buffer: Buffer.from(csv, 'utf-8') })
  await page.getByRole('button', { name: 'Read the file' }).click()

  await page.getByLabel('Role for column Posted Date').selectOption('date')
  await page.getByLabel('Role for column Payee').selectOption('description')
  await page.getByLabel('Role for column Amount').selectOption('amount')

  await page.getByRole('button', { name: 'Preview all rows' }).click()
  await expect(page.getByRole('region', { name: 'Preview' })).toBeVisible()
}

/** The counts under the double rule: what the whole screen exists to make trustworthy. */
async function expectTally(page: Page, willImport: number, skipped: number) {
  const tally = page.locator('.exceptions')
  await expect(tally.locator('.exception', { hasText: 'Rows will import' })).toContainText(String(willImport))
  await expect(tally.locator('.exception', { hasText: 'Skipped by a rule' })).toContainText(String(skipped))
}

function entry(page: Page, description: string) {
  return page.locator('.preview-entry', { hasText: description })
}

test('a rule written on the rules page strikes the matching row in the preview', async ({ page }) => {
  const accountName = `E2E Rules ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)

  // The rules page is reached from the Import page, not from the masthead.
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await page.getByRole('link', { name: 'ignore rule' }).click()
  await expect(page.getByRole('heading', { name: 'Ignore rules' })).toBeVisible()

  await page.getByLabel('Match').selectOption('Contains')
  await page.getByLabel('These words').fill('AUTOPAY')
  await page.getByLabel('For').selectOption({ label: accountName })
  await page.getByRole('button', { name: 'Add rule' }).click()

  const ruleRow = page.getByRole('row').filter({ hasText: 'AUTOPAY' }).filter({ hasText: accountName })
  await expect(ruleRow).toBeVisible()

  // Same words, same kind of match, same scope is the same rule — refused at the field, not in a
  // dialog, and the form keeps what was typed.
  await page.getByLabel('These words').fill('AUTOPAY')
  await page.getByLabel('For').selectOption({ label: accountName })
  await page.getByRole('button', { name: 'Add rule' }).click()
  await expect(page.locator('.field-error')).toContainText('That rule is already written')
  await expect(page.getByRole('row').filter({ hasText: 'AUTOPAY' }).filter({ hasText: accountName })).toHaveCount(1)

  await previewCsv(page, accountName)

  // The matching row is struck, and says which rule caught it.
  const autopay = entry(page, 'AUTOPAY THANK YOU')
  await expect(autopay).toHaveAttribute('data-skipped', 'true')
  await expect(autopay).toContainText('Skipped · contains AUTOPAY')

  // Nothing else is touched.
  await expect(entry(page, 'KROGER #442')).toHaveAttribute('data-skipped', 'false')
  await expectTally(page, 4, 1)

  // A struck row offers no per-row escape hatch: if the strike is wrong, the rule is wrong.
  await expect(autopay.getByRole('button', { name: 'Ignore rows like this' })).toHaveCount(0)
})

test('a rule written from a preview row strikes that row in place', async ({ page }) => {
  const accountName = `E2E From Row ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)
  await previewCsv(page, accountName)

  await expectTally(page, 5, 0)

  const transfer = entry(page, 'TRANSFER TO SAVINGS')
  await expect(transfer).toHaveAttribute('data-skipped', 'false')
  await transfer.getByRole('button', { name: 'Ignore rows like this' }).click()

  // The rule is born from the row: its words are the description the row is showing, and its
  // scope is the account being imported.
  await expect(page.getByLabel('These words')).toHaveValue('TRANSFER TO SAVINGS')
  await expect(page.getByLabel('For')).toHaveValue(/\d+/)
  await page.getByRole('button', { name: 'Add rule' }).click()

  // The payoff: the row strikes itself where it stands, with no navigation.
  await expect(transfer).toHaveAttribute('data-skipped', 'true')
  await expect(transfer).toContainText('Skipped · contains TRANSFER TO SAVINGS')
  await expectTally(page, 4, 1)

  // The inter-account transfer is dropped by a text rule, because this app has no Transfer entity.
  await expect(page.getByRole('link', { name: 'ignore rules' }).first()).toBeVisible()
})

test('deleting a rule un-strikes the rows it was catching', async ({ page }) => {
  const accountName = `E2E Undo Rule ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)
  await previewCsv(page, accountName)

  const shell = entry(page, 'SHELL OIL 5578')
  await shell.getByRole('button', { name: 'Ignore rows like this' }).click()
  await page.getByLabel('These words').fill('SHELL OIL')
  await page.getByRole('button', { name: 'Add rule' }).click()
  await expect(shell).toHaveAttribute('data-skipped', 'true')
  await expectTally(page, 4, 1)

  await page.getByRole('link', { name: 'ignore rules' }).first().click()
  await expect(page.getByRole('heading', { name: 'Ignore rules' })).toBeVisible()

  const ruleRow = page.getByRole('row').filter({ hasText: 'SHELL OIL' }).filter({ hasText: accountName })
  await ruleRow.getByRole('button', { name: 'Delete' }).click()
  // A confirmation printed in the page, never a browser dialog.
  await page.getByRole('button', { name: 'Delete rule' }).click()
  await expect(ruleRow).toHaveCount(0)

  // Returning to station 3 re-reads the file, so the row comes back.
  await previewCsv(page, accountName)
  await expect(entry(page, 'SHELL OIL 5578')).toHaveAttribute('data-skipped', 'false')
  await expectTally(page, 5, 0)
})

test('a rule that catches every row says so, and an empty rule is refused at the field', async ({ page }) => {
  const accountName = `E2E Too Broad ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)
  await previewCsv(page, accountName, ALL_NOISE_CSV)

  await entry(page, 'POINTS REDEMPTION').getByRole('button', { name: 'Ignore rows like this' }).click()

  // Whitespace alone is not a rule, and the refusal is named beside the field it belongs to.
  await page.getByLabel('These words').fill('   ')
  await page.getByRole('button', { name: 'Add rule' }).click()
  await expect(page.locator('.field-error')).toContainText('Type the words the rule should look for')

  // One word both rows carry, so the rule catches the whole file.
  await page.getByLabel('These words').fill('POINTS')
  await page.getByRole('button', { name: 'Add rule' }).click()

  await expectTally(page, 0, 2)
  // Nothing left to import is named as the diagnosis it is, and the over-broad rule is quoted.
  const warning = page.getByRole('alert').filter({ hasText: 'Nothing is left to import' })
  await expect(warning).toContainText('contains POINTS')
})

test('a row that cannot be read keeps its description and can still seed a rule', async ({ page }) => {
  const accountName = `E2E Unreadable ${Date.now()}`

  await logIn(page)
  await addAccount(page, accountName)

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page
    .getByLabel('CSV file')
    .setInputFiles({ name: 'august.csv', mimeType: 'text/csv', buffer: Buffer.from(NOISY_CSV, 'utf-8') })
  await page.getByRole('button', { name: 'Read the file' }).click()

  await page.getByLabel('Role for column Posted Date').selectOption('date')
  await page.getByLabel('Role for column Payee').selectOption('description')
  await page.getByLabel('Role for column Amount').selectOption('amount')

  // Tell the app the dates are written the other way round, so no row's date parses.
  await page.getByRole('button', { name: 'Change' }).click()
  await page.getByLabel('Date format').selectOption('yyyy-MM-dd')
  await page.getByRole('button', { name: 'Preview all rows' }).click()
  await expect(page.getByRole('region', { name: 'Preview' })).toBeVisible()

  // Unreadable rows are named in the margin rather than given a count of their own.
  await expect(page.getByText('could not be read and are not counted above')).toBeVisible()
  await expectTally(page, 0, 0)

  // The row still prints what it does know, and is still the place a rule gets written from —
  // a description the app can read perfectly well is exactly what a rule matches on.
  const autopay = entry(page, 'AUTOPAY THANK YOU')
  await expect(autopay).toContainText('is not a date')
  await autopay.getByRole('button', { name: 'Ignore rows like this' }).click()
  await expect(page.getByLabel('These words')).toHaveValue('AUTOPAY THANK YOU')
  await page.getByRole('button', { name: 'Add rule' }).click()

  // Struck, and the parse error is gone: the row is leaving either way, and two reasons would
  // read as two problems.
  await expect(autopay).toHaveAttribute('data-skipped', 'true')
  await expect(autopay).toContainText('Skipped · contains AUTOPAY THANK YOU')
  await expect(autopay).not.toContainText('is not a date')
  await expectTally(page, 0, 1)
})
