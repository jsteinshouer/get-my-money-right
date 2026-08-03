import { test, expect } from '@playwright/test'

test('setting a category budget shows it in the current month list after reload', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: /^Welcome,/ })).toBeVisible()

  const stamp = Date.now()
  const categoryName = `E2E Budget Category ${stamp}`

  await page.getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()
  await page.getByLabel('Name').fill(categoryName)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()

  await page.getByRole('link', { name: 'Budgets' }).click()
  await expect(page.getByRole('heading', { name: 'Budgets' })).toBeVisible()

  await page.getByLabel('Category').selectOption({ label: categoryName })
  await page.getByLabel('Monthly limit').fill('325.00')
  await page.getByRole('button', { name: 'Save budget' }).click()

  const row = page.getByRole('row').filter({ hasText: categoryName })
  await expect(row).toBeVisible()
  await expect(row.getByText('325.00')).toBeVisible()

  await page.reload()

  const reloadedRow = page.getByRole('row').filter({ hasText: categoryName })
  await expect(reloadedRow).toBeVisible()
  await expect(reloadedRow.getByText('325.00')).toBeVisible()
})
