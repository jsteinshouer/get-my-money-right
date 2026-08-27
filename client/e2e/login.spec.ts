import { test, expect } from '@playwright/test'

const seededUsers = [
  { email: 'user1@household.local', password: 'ChangeMe123!', displayName: 'Household Member 1' },
  { email: 'user2@household.local', password: 'ChangeMe123!', displayName: 'Household Member 2' },
]

for (const user of seededUsers) {
  test(`logs in as ${user.displayName} and reaches the authenticated shell`, async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveURL(/\/login$/)

    await page.getByLabel('Email').fill(user.email)
    await page.getByLabel('Password').fill(user.password)
    await page.getByRole('button', { name: 'Log in' }).click()

    await expect(page).toHaveURL('http://localhost:5173/')
    await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()
    await expect(page.getByText(user.displayName)).toBeVisible()
    // The spread is the authenticated landing surface, not a welcome message.
    await expect(page.locator('.spread-month')).toBeVisible()
  })
}

test('unauthenticated visitor is redirected to login', async ({ page }) => {
  await page.goto('/')
  await expect(page).toHaveURL(/\/login$/)
})

test('logging out returns to the login page', async ({ page }) => {
  const user = seededUsers[0]
  await page.goto('/login')
  await page.getByLabel('Email').fill(user.email)
  await page.getByLabel('Password').fill(user.password)
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByText(user.displayName)).toBeVisible()

  await page.getByRole('button', { name: 'Log out' }).click()

  await expect(page).toHaveURL(/\/login$/)
})
