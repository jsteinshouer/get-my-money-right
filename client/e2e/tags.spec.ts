import { test, expect } from '@playwright/test'

async function logIn(page: import('@playwright/test').Page) {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()
}

async function goToSection(page: import('@playwright/test').Page, name: string) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name }).click()
  // The route swap has to land before anything is typed: several sections have a "Name" field,
  // and filling the outgoing page's one leaves this page's form empty and silently unsubmittable.
  await expect(page.getByRole('heading', { name, level: 1 })).toBeVisible()
}

async function addAccount(page: import('@playwright/test').Page, name: string) {
  await goToSection(page, 'Accounts')
  await page.getByLabel('Name').fill(name)
  await page.getByLabel('Type').selectOption('Checking')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: name })).toBeVisible()
}

async function addCategory(page: import('@playwright/test').Page, name: string) {
  await goToSection(page, 'Categories')
  await page.getByLabel('Name').fill(name)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: name })).toBeVisible()
}

async function addTransaction(page: import('@playwright/test').Page, opts: {
  account: string
  category: string
  description: string
  amount: string
}) {
  const addForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Add a transaction' }) })
  await addForm.getByLabel('Account').selectOption({ label: opts.account })
  await addForm.getByLabel('Category').selectOption({ label: opts.category })
  await addForm.getByLabel('Need/Want').selectOption('Want')
  await addForm.getByLabel('Amount').fill(opts.amount)
  await addForm.getByLabel('Description').fill(opts.description)
  await page.getByRole('button', { name: 'Add transaction' }).click()
  await expect(page.getByRole('row').filter({ hasText: opts.description })).toBeVisible()
}

test('a tag written onto a transaction is created in place and filters the list', async ({ page }) => {
  await logIn(page)

  const stamp = Date.now()
  const tagName = `E2E Tag ${stamp}`
  const accountName = `E2E Tag Account ${stamp}`
  const categoryName = `E2E Tag Category ${stamp}`
  const taggedDescription = `E2E Tagged Txn ${stamp}`
  const untaggedDescription = `E2E Untagged Txn ${stamp}`

  await addAccount(page, accountName)
  await addCategory(page, categoryName)
  await goToSection(page, 'Transactions')
  await addTransaction(page, { account: accountName, category: categoryName, description: taggedDescription, amount: '-31.00' })
  await addTransaction(page, { account: accountName, category: categoryName, description: untaggedDescription, amount: '-11.00' })

  // The tag does not exist yet: it is invented at the moment the transaction needs it,
  // without leaving the page for the tag manager.
  await page.getByRole('row').filter({ hasText: taggedDescription }).getByRole('button', { name: 'Correct' }).click()
  const slip = page.getByRole('row').filter({ has: page.getByRole('heading', { name: 'Correcting this entry' }) })
  await expect(slip.getByLabel('Description')).toHaveValue(taggedDescription)

  const tagInput = slip.getByRole('combobox', { name: 'Add a tag' })
  await tagInput.fill(tagName)
  await expect(slip.getByRole('option', { name: `New tag “${tagName}”` })).toBeVisible()
  await tagInput.press('Enter')
  // The remove control only exists for a tag that is actually applied, so this waits for the
  // create round-trip rather than matching the "New tag" suggestion still on screen.
  await expect(slip.getByRole('button', { name: `Remove tag ${tagName}` })).toBeVisible()
  await slip.getByRole('button', { name: 'Save correction' }).click()

  const taggedRow = page.getByRole('row').filter({ hasText: taggedDescription })
  await expect(taggedRow.getByText(tagName)).toBeVisible()

  await page.getByLabel('Filter by tag').selectOption({ label: tagName })
  await expect(page.getByRole('row').filter({ hasText: taggedDescription })).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: untaggedDescription })).toHaveCount(0)

  await page.reload()
  await page.getByLabel('Filter by tag').selectOption({ label: tagName })
  await expect(page.getByRole('row').filter({ hasText: taggedDescription })).toBeVisible()
})

test('a selection of transactions is tagged in one pass and reports what it changed', async ({ page }) => {
  await logIn(page)

  const stamp = Date.now()
  const tagName = `E2E Bulk ${stamp}`
  const accountName = `E2E Bulk Account ${stamp}`
  const categoryName = `E2E Bulk Category ${stamp}`
  const first = `E2E Bulk First ${stamp}`
  const second = `E2E Bulk Second ${stamp}`

  await addAccount(page, accountName)
  await addCategory(page, categoryName)
  await goToSection(page, 'Transactions')
  await addTransaction(page, { account: accountName, category: categoryName, description: first, amount: '-40.00' })
  await addTransaction(page, { account: accountName, category: categoryName, description: second, amount: '-60.00' })

  // Narrow to this investigation's account, so the closing figure is a real answer.
  await page.getByLabel('Filter by account').selectOption({ label: accountName })
  await expect(page.getByRole('row').filter({ hasText: first })).toBeVisible()

  await page.getByRole('checkbox', { name: 'Select all transactions' }).check()
  await expect(page.getByText('2 selected')).toBeVisible()

  const bulkInput = page.getByRole('combobox', { name: 'Tag the selected transactions' })
  await bulkInput.fill(tagName)
  await bulkInput.press('Enter')

  await expect(page.getByRole('status')).toContainText(`Tagged 2 transactions “${tagName}”`)
  await expect(page.getByRole('row').filter({ hasText: first }).getByText(tagName)).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: second }).getByText(tagName)).toBeVisible()

  // The question the tagging was for: what did this add up to?
  await page.getByLabel('Filter by tag').selectOption({ label: tagName })
  await expect(page.getByText('2 entries shown')).toBeVisible()
  await expect(page.locator('tfoot td.money')).toHaveText('-100.00')
})

test('deleting a tag says how many transactions it will be removed from', async ({ page }) => {
  await logIn(page)

  const stamp = Date.now()
  const tagName = `E2E Delete ${stamp}`
  const accountName = `E2E Delete Account ${stamp}`
  const categoryName = `E2E Delete Category ${stamp}`
  const description = `E2E Delete Txn ${stamp}`

  await addAccount(page, accountName)
  await addCategory(page, categoryName)
  await goToSection(page, 'Transactions')
  await addTransaction(page, { account: accountName, category: categoryName, description, amount: '-15.00' })

  await page.getByLabel('Filter by account').selectOption({ label: accountName })
  await page.getByRole('checkbox', { name: 'Select all transactions' }).check()
  const bulkInput = page.getByRole('combobox', { name: 'Tag the selected transactions' })
  await bulkInput.fill(tagName)
  await bulkInput.press('Enter')
  await expect(page.getByRole('status')).toContainText(`Tagged 1 transaction “${tagName}”`)

  await goToSection(page, 'Tags')
  const tagRow = page.getByRole('row').filter({ hasText: tagName })
  await expect(tagRow).toBeVisible()
  await tagRow.getByRole('button', { name: 'Delete' }).click()

  // Nothing disappears silently: the count is stated before the delete, and after it.
  const confirm = page.getByRole('alertdialog')
  await expect(confirm).toContainText('removed from')
  await expect(confirm).toContainText('1 transaction')
  await confirm.getByRole('button', { name: 'Delete tag' }).click()

  await expect(page.getByRole('status')).toContainText(`removed it from 1 transaction`)
  await expect(page.getByRole('row').filter({ hasText: tagName })).toHaveCount(0)
})
