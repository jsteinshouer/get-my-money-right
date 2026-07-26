import { test, expect } from '@playwright/test'

test('adding an account through the UI persists after a page reload', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: /^Welcome,/ })).toBeVisible()

  await page.getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible()

  const accountName = `E2E Checking ${Date.now()}`
  await page.getByLabel('Name').fill(accountName)
  await page.getByLabel('Type').selectOption('Checking')
  await page.getByRole('button', { name: 'Add account' }).click()

  const row = page.getByRole('row').filter({ hasText: accountName })
  await expect(row).toBeVisible()
  await expect(row.getByText('Checking', { exact: true })).toBeVisible()
  await expect(row.getByText('Active', { exact: true })).toBeVisible()

  await page.reload()

  await expect(page.getByRole('row').filter({ hasText: accountName })).toBeVisible()
})
