import { test, expect } from '@playwright/test'

test('adding a category through the UI persists after a page reload', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('heading', { name: /^Welcome,/ })).toBeVisible()

  await page.getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()

  const categoryName = `E2E Groceries ${Date.now()}`
  await page.getByLabel('Name').fill(categoryName)
  await page.getByRole('button', { name: 'Add category' }).click()

  const row = page.getByRole('row').filter({ hasText: categoryName })
  await expect(row).toBeVisible()

  await page.reload()

  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()
})
