import { test, expect } from '@playwright/test'

test('a tag created in the UI can be applied to a transaction and filtered on', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()

  const stamp = Date.now()
  const tagName = `E2E Tag ${stamp}`
  const accountName = `E2E Tag Account ${stamp}`
  const categoryName = `E2E Tag Category ${stamp}`
  const taggedDescription = `E2E Tagged Txn ${stamp}`
  const untaggedDescription = `E2E Untagged Txn ${stamp}`

  const nav = page.getByRole('navigation', { name: 'Sections' })

  await nav.getByRole('link', { name: 'Tags' }).click()
  await expect(page.getByRole('heading', { name: 'Tags' })).toBeVisible()
  await page.getByLabel('Name').fill(tagName)
  await page.getByRole('button', { name: 'Add tag' }).click()
  await expect(page.getByRole('row').filter({ hasText: tagName })).toBeVisible()

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

  await nav.getByRole('link', { name: 'Transactions' }).click()
  await expect(page.getByRole('heading', { name: 'Transactions' })).toBeVisible()

  const addForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Add a transaction' }) })
  for (const description of [taggedDescription, untaggedDescription]) {
    await addForm.getByLabel('Account').selectOption({ label: accountName })
    await addForm.getByLabel('Category').selectOption({ label: categoryName })
    await addForm.getByLabel('Need/Want').selectOption('Want')
    await addForm.getByLabel('Amount').fill('-31.00')
    await addForm.getByLabel('Description').fill(description)
    await page.getByRole('button', { name: 'Add transaction' }).click()
    await expect(page.getByRole('row').filter({ hasText: description })).toBeVisible()
  }

  // Apply the tag through the transaction's edit form. Once editing starts the description
  // moves into a textbox, so the row has to be found by that form rather than by its text.
  await page.getByRole('row').filter({ hasText: taggedDescription }).getByRole('button', { name: 'Edit' }).click()
  const editingRow = page.getByRole('row').filter({ has: page.getByRole('textbox', { name: 'Description' }) })
  await expect(editingRow.getByRole('textbox', { name: 'Description' })).toHaveValue(taggedDescription)
  await editingRow.getByRole('checkbox', { name: tagName }).check()
  await editingRow.getByRole('button', { name: 'Save' }).click()

  const taggedRow = page.getByRole('row').filter({ hasText: taggedDescription })
  await expect(taggedRow.getByText(tagName)).toBeVisible()

  await page.getByLabel('Filter by tag').selectOption({ label: tagName })
  await expect(page.getByRole('row').filter({ hasText: taggedDescription })).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: untaggedDescription })).toHaveCount(0)

  await page.reload()
  await page.getByLabel('Filter by tag').selectOption({ label: tagName })
  await expect(page.getByRole('row').filter({ hasText: taggedDescription })).toBeVisible()
})
