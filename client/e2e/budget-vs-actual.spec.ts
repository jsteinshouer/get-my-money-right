import { test, expect, type Page } from '@playwright/test'

// Pinned month + transaction date so the run never straddles a month boundary.
const MONTH = '2026-03'
const TRANSACTION_DATE = '2026-03-10'

test('actual spend on the budgets page reflects transactions in that category and month', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: /^Welcome,/ })).toBeVisible()

  const stamp = Date.now()
  const accountName = `E2E Actual Account ${stamp}`
  const categoryName = `E2E Actual Category ${stamp}`

  await page.getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible()
  await page.getByLabel('Name').fill(accountName)
  await page.getByLabel('Type').selectOption('Checking')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: accountName })).toBeVisible()

  await page.getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()
  await page.getByLabel('Name').fill(categoryName)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()

  await page.getByRole('link', { name: 'Budgets' }).click()
  await expect(page.getByRole('heading', { name: 'Budgets' })).toBeVisible()
  await page.getByLabel('Month', { exact: true }).fill(MONTH)
  await page.getByLabel('Category').selectOption({ label: categoryName })
  await page.getByLabel('Monthly limit').fill('200.00')
  await page.getByRole('button', { name: 'Save budget' }).click()

  await expectActualCells(page, categoryName, { actual: '0.00', remaining: '200.00 left' })

  await addTransaction(page, { accountName, categoryName, amount: '-75.25', description: `E2E Actual A ${stamp}` })
  await expectActual(page, categoryName, { actual: '75.25', remaining: '124.75 left' })

  await addTransaction(page, { accountName, categoryName, amount: '-25.00', description: `E2E Actual B ${stamp}` })
  await expectActual(page, categoryName, { actual: '100.25', remaining: '99.75 left' })

  // Spending past the limit flips remaining to an over-budget reading.
  await addTransaction(page, { accountName, categoryName, amount: '-150.00', description: `E2E Actual C ${stamp}` })
  await expectActual(page, categoryName, { actual: '250.25', remaining: '50.25 over' })
})

async function addTransaction(
  page: Page,
  { accountName, categoryName, amount, description }: { accountName: string; categoryName: string; amount: string; description: string },
) {
  await page.getByRole('link', { name: 'Transactions' }).click()
  await expect(page.getByRole('heading', { name: 'Transactions' })).toBeVisible()

  const addForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Add a transaction' }) })
  await addForm.getByLabel('Account').selectOption({ label: accountName })
  await addForm.getByLabel('Category').selectOption({ label: categoryName })
  await addForm.getByLabel('Need/Want').selectOption('Need')
  await addForm.getByLabel('Date').fill(TRANSACTION_DATE)
  await addForm.getByLabel('Amount').fill(amount)
  await addForm.getByLabel('Description').fill(description)
  await page.getByRole('button', { name: 'Add transaction' }).click()
  await expect(page.getByRole('row').filter({ hasText: description })).toBeVisible()
}

/** Reloads the budgets page for the pinned month, then asserts the category's actual-vs-limit cells. */
async function expectActual(
  page: Page,
  categoryName: string,
  amounts: { actual: string; remaining: string },
) {
  await page.getByRole('link', { name: 'Budgets' }).click()
  await expect(page.getByRole('heading', { name: 'Budgets' })).toBeVisible()
  await page.getByLabel('Month', { exact: true }).fill(MONTH)
  await expectActualCells(page, categoryName, amounts)
}

async function expectActualCells(
  page: Page,
  categoryName: string,
  { actual, remaining }: { actual: string; remaining: string },
) {
  // Addressed by column position rather than by cell text: the progress bar's value duplicates
  // the actual figure in the accessibility tree, so a name-based cell lookup is ambiguous.
  // Columns: Category | Monthly limit | Actual | Remaining | Progress | Actions.
  const cells = page.getByRole('row').filter({ hasText: categoryName }).getByRole('cell')
  await expect(cells.nth(2)).toHaveText(actual)
  await expect(cells.nth(3)).toHaveText(remaining)
}
