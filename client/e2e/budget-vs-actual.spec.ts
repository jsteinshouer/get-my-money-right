import { test, expect, type Page } from '@playwright/test'

// Pinned month + transaction date so the run never straddles a month boundary.
// March 2026 is a closed month, so the spread shows Limit and Actual with no pace column.
const MONTH = '2026-03'
const MONTH_LABEL = 'March 2026'
const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]
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

test('the pace column belongs to the current month only', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()

  // CI starts from an empty database, so this test creates every row it asserts on
  // rather than relying on seeded demo data or on another spec having run first.
  const stamp = Date.now()
  const accountName = `E2E Pace Account ${stamp}`
  const categoryName = `E2E Pace Category ${stamp}`
  const nav = page.getByRole('navigation', { name: 'Sections' })

  await nav.getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible()
  await page.getByLabel('Name').fill(accountName)
  await page.getByLabel('Type').selectOption('Checking')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: accountName })).toBeVisible()

  await nav.getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()
  await page.getByLabel('Name').fill(categoryName)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()

  const now = new Date()
  const currentMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
  const currentMonthLabel = `${MONTH_NAMES[now.getMonth()]} ${now.getFullYear()}`

  await setBudget(page, { month: currentMonth, categoryName, amount: '200.00' })
  await setBudget(page, { month: MONTH, categoryName, amount: '200.00' })

  // The month in progress carries the pace column, and every entry's rule gets a tick.
  await openSpread(page, currentMonthLabel)
  await expect(page.locator('.spread-elapsed').first()).toContainText('Day ')
  await expect(page.locator('.entry-columns').first()).toContainText('Expected to date')
  await expect(entryFor(page, categoryName).locator('.gauge-tick')).toHaveCount(1)

  // A closed month has no "where you should be by now", so both go away.
  await openSpread(page, MONTH_LABEL)
  await expect(page.locator('.spread-elapsed').first()).toHaveText('Month closed')
  await expect(page.locator('.entry-columns').first()).not.toContainText('Expected to date')
  await expect(page.locator('.gauge-tick')).toHaveCount(0)
})

/** Sets a limit for a category in a given month through the budgets page. */
async function setBudget(
  page: Page,
  { month, categoryName, amount }: { month: string; categoryName: string; amount: string },
) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Budgets' }).click()
  await expect(page.getByRole('heading', { name: 'Budgets' })).toBeVisible()
  await page.getByLabel('Month', { exact: true }).fill(month)
  const form = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Set a category budget' }) })
  await form.getByLabel('Category').selectOption({ label: categoryName })
  await form.getByLabel('Monthly limit').fill(amount)
  await page.getByRole('button', { name: 'Save budget' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()
}

function entryFor(page: Page, categoryName: string) {
  return page.locator('.entry').filter({ hasText: categoryName })
}

/** Pages the spread back from the current month until it reaches the pinned one. */
async function openSpread(page: Page, monthLabel: string = MONTH_LABEL) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'This month' }).click()
  await expect(page.locator('.spread-month')).toBeVisible()

  for (let step = 0; step < 24; step++) {
    if ((await page.locator('.spread-month').innerText()) === monthLabel.toUpperCase()) return
    await page.locator('.spread-nav button').first().click()
  }
  throw new Error(`Could not reach ${monthLabel} by paging the spread`)
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
