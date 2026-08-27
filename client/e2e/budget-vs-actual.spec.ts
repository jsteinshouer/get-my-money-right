import { test, expect, type Page } from '@playwright/test'

// Pinned month + transaction date so the run never straddles a month boundary.
// March 2026 is a closed month, so the spread shows Limit and Actual with no pace column.
const MONTH = '2026-03'
const MONTH_LABEL = 'March 2026'
const TRANSACTION_DATE = '2026-03-10'

test('the ledger spread reflects transactions in that category and month', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()

  const stamp = Date.now()
  const accountName = `E2E Actual Account ${stamp}`
  const categoryName = `E2E Actual Category ${stamp}`

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible()
  await page.getByLabel('Name').fill(accountName)
  await page.getByLabel('Type').selectOption('Checking')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: accountName })).toBeVisible()

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()
  await page.getByLabel('Name').fill(categoryName)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Budgets' }).click()
  await expect(page.getByRole('heading', { name: 'Budgets' })).toBeVisible()
  await page.getByLabel('Month', { exact: true }).fill(MONTH)
  const budgetForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Set a category budget' }) })
  await budgetForm.getByLabel('Category').selectOption({ label: categoryName })
  await budgetForm.getByLabel('Monthly limit').fill('200.00')
  await page.getByRole('button', { name: 'Save budget' }).click()

  // A budget with nothing spent against it still has an entry, reading the full limit as left.
  await openSpread(page)
  await expectEntry(page, categoryName, { limit: '200.00', actual: '0.00', relation: '200.00 left' })

  await addTransaction(page, { accountName, categoryName, amount: '-75.25', description: `E2E Actual A ${stamp}` })
  await openSpread(page)
  await expectEntry(page, categoryName, { limit: '200.00', actual: '75.25', relation: '124.75 left' })

  await addTransaction(page, { accountName, categoryName, amount: '-25.00', description: `E2E Actual B ${stamp}` })
  await openSpread(page)
  await expectEntry(page, categoryName, { limit: '200.00', actual: '100.25', relation: '99.75 left' })

  // Spending past the limit flips the relation to an over-budget reading, and the
  // entry's rule-gauge switches to its over state.
  await addTransaction(page, { accountName, categoryName, amount: '-150.00', description: `E2E Actual C ${stamp}` })
  await openSpread(page)
  await expectEntry(page, categoryName, { limit: '200.00', actual: '250.25', relation: '50.25 over' })
  await expect(entryFor(page, categoryName).locator('.gauge')).toHaveAttribute('data-over', 'true')
})

test('a closed month shows no expected-to-date column', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()

  // The current month is in progress, so it carries the pace column and a tick.
  await expect(page.locator('.entry-columns').first()).toContainText('Expected to date')

  await openSpread(page)
  await expect(page.locator('.spread-elapsed').first()).toHaveText('Month closed')
  await expect(page.locator('.entry-columns').first()).not.toContainText('Expected to date')
  await expect(page.locator('.gauge-tick')).toHaveCount(0)
})

function entryFor(page: Page, categoryName: string) {
  return page.locator('.entry').filter({ hasText: categoryName })
}

/** Pages the spread back from the current month until it reaches the pinned one. */
async function openSpread(page: Page) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'This month' }).click()
  await expect(page.locator('.spread-month')).toBeVisible()

  for (let step = 0; step < 24; step++) {
    if ((await page.locator('.spread-month').innerText()) === MONTH_LABEL.toUpperCase()) return
    await page.locator('.spread-nav button').first().click()
  }
  throw new Error(`Could not reach ${MONTH_LABEL} by paging the spread`)
}

async function expectEntry(
  page: Page,
  categoryName: string,
  { limit, actual, relation }: { limit: string; actual: string; relation: string },
) {
  const entry = entryFor(page, categoryName)
  await expect(entry).toBeVisible()
  await expect(entry.locator('.entry-figure').first()).toHaveText(limit)
  await expect(entry.locator('.entry-figure.is-actual')).toHaveText(actual)
  await expect(entry.locator('.entry-remaining')).toHaveText(relation)
}

async function addTransaction(
  page: Page,
  { accountName, categoryName, amount, description }: { accountName: string; categoryName: string; amount: string; description: string },
) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Transactions' }).click()
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
