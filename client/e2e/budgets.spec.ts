import { test, expect } from '@playwright/test'

test('setting a category budget shows it in the current month list after reload', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()

  const stamp = Date.now()
  const categoryName = `E2E Budget Category ${stamp}`

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()
  await page.getByLabel('Name').fill(categoryName)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Budgets' }).click()
  await expect(page.getByRole('heading', { name: 'Budgets' })).toBeVisible()

  const budgetForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Set a category budget' }) })
  await budgetForm.getByLabel('Category').selectOption({ label: categoryName })
  await budgetForm.getByLabel('Monthly limit').fill('325.00')
  await page.getByRole('button', { name: 'Save budget' }).click()

  const row = page.getByRole('row').filter({ hasText: categoryName })
  await expect(row).toBeVisible()
  await expect(row.getByRole('cell', { name: '325.00', exact: true })).toBeVisible()

  await page.reload()

  const reloadedRow = page.getByRole('row').filter({ hasText: categoryName })
  await expect(reloadedRow).toBeVisible()
  await expect(reloadedRow.getByRole('cell', { name: '325.00', exact: true })).toBeVisible()
})

test('a slow load for the previous month cannot overwrite the month the user switched to', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()

  const stamp = Date.now()
  const categoryName = `E2E Race Category ${stamp}`
  const laterMonth = '2030-01'

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()
  await page.getByLabel('Name').fill(categoryName)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Budgets' }).click()
  await expect(page.getByRole('heading', { name: 'Budgets' })).toBeVisible()
  await page.getByLabel('Month', { exact: true }).fill(laterMonth)
  const budgetForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Set a category budget' }) })
  await budgetForm.getByLabel('Category').selectOption({ label: categoryName })
  await budgetForm.getByLabel('Monthly limit').fill('50.00')
  await page.getByRole('button', { name: 'Save budget' }).click()
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible()

  // Force the losing ordering: the page mounts on the current month, whose load is held back
  // long enough that the month the user switches to has already rendered when it finally lands.
  const now = new Date()
  const isCurrentMonthLoad = (url: string) => {
    const params = new URL(url).searchParams
    return params.get('year') === String(now.getFullYear()) && params.get('month') === String(now.getMonth() + 1)
  }
  await page.route('**/api/budgets?*', async (route) => {
    if (isCurrentMonthLoad(route.request().url())) {
      await new Promise((resolve) => setTimeout(resolve, 1500))
    }
    await route.continue()
  })

  await page.reload()
  await page.getByLabel('Month', { exact: true }).fill(laterMonth)

  const row = page.getByRole('row').filter({ hasText: categoryName })
  await expect(row).toBeVisible()

  await page.waitForResponse((response) => isCurrentMonthLoad(response.url()))
  await page.waitForTimeout(250) // let the stale response be handled (or, before the fix, applied)
  await expect(row).toBeVisible()
})
