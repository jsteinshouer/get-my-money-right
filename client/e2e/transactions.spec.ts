import { test, expect } from '@playwright/test'

test('manually adding a transaction shows it in the filtered list', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: /^Welcome,/ })).toBeVisible()

  const stamp = Date.now()
  const accountName = `E2E Txn Account ${stamp}`
  const categoryName = `E2E Txn Category ${stamp}`
  const description = `E2E Txn ${stamp}`

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

  await page.getByRole('link', { name: 'Transactions' }).click()
  await expect(page.getByRole('heading', { name: 'Transactions' })).toBeVisible()

  const addForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Add a transaction' }) })
  await addForm.getByLabel('Account').selectOption({ label: accountName })
  await addForm.getByLabel('Category').selectOption({ label: categoryName })
  await addForm.getByLabel('Need/Want').selectOption('Want')
  await addForm.getByLabel('Amount').fill('-25.50')
  await addForm.getByLabel('Description').fill(description)
  await page.getByRole('button', { name: 'Add transaction' }).click()

  await expect(page.getByRole('row').filter({ hasText: description })).toBeVisible()

  await page.getByLabel('Filter by category').selectOption({ label: categoryName })
  const row = page.getByRole('row').filter({ hasText: description })
  await expect(row).toBeVisible()
  await expect(row.getByText('Want', { exact: true })).toBeVisible()

  await page.reload()
  await page.getByLabel('Filter by category').selectOption({ label: categoryName })
  await expect(page.getByRole('row').filter({ hasText: description })).toBeVisible()
})
