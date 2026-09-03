import { test, expect } from '@playwright/test'

test('manually adding a transaction shows it in the filtered list', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()

  const stamp = Date.now()
  const accountName = `E2E Txn Account ${stamp}`
  const categoryName = `E2E Txn Category ${stamp}`
  const description = `E2E Txn ${stamp}`

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

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Transactions' }).click()
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
  // Need/Want is reclassified in the ledger itself now, so it reads as the select's value.
  await expect(row.getByLabel(`Need or Want for ${description}`)).toHaveValue('Want')

  await page.reload()
  await page.getByLabel('Filter by category').selectOption({ label: categoryName })
  await expect(page.getByRole('row').filter({ hasText: description })).toBeVisible()
})

test('the queue reclassifies an entry in place, and a correction slip guards the amount', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()

  const stamp = Date.now()
  const accountName = `E2E Queue Account ${stamp}`
  const fromCategory = `E2E Queue From ${stamp}`
  const toCategory = `E2E Queue To ${stamp}`
  const description = `E2E Queue Txn ${stamp}`
  const nav = page.getByRole('navigation', { name: 'Sections' })

  await nav.getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts', level: 1 })).toBeVisible()
  await page.getByLabel('Name').fill(accountName)
  await page.getByLabel('Type').selectOption('Checking')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: accountName })).toBeVisible()

  for (const name of [fromCategory, toCategory]) {
    await nav.getByRole('link', { name: 'Categories' }).click()
    await expect(page.getByRole('heading', { name: 'Categories', level: 1 })).toBeVisible()
    await page.getByLabel('Name').fill(name)
    await page.getByRole('button', { name: 'Add category' }).click()
    await expect(page.getByRole('row').filter({ hasText: name })).toBeVisible()
  }

  await nav.getByRole('link', { name: 'Transactions' }).click()
  await expect(page.getByRole('heading', { name: 'Transactions', level: 1 })).toBeVisible()
  const addForm = page.locator('article').filter({ has: page.getByRole('heading', { name: 'Add a transaction' }) })
  await addForm.getByLabel('Account').selectOption({ label: accountName })
  await addForm.getByLabel('Category').selectOption({ label: fromCategory })
  await addForm.getByLabel('Need/Want').selectOption('Want')
  await addForm.getByLabel('Amount').fill('-42.50')
  await addForm.getByLabel('Description').fill(description)
  await page.getByRole('button', { name: 'Add transaction' }).click()
  await expect(page.getByRole('row').filter({ hasText: description })).toBeVisible()

  // The queue job: two fields change in the ledger itself, with no mode to enter or leave.
  const row = page.getByRole('row').filter({ hasText: description })
  await row.getByLabel(`Category for ${description}`).selectOption({ label: toCategory })
  await expect(row.getByLabel(`Category for ${description}`)).toHaveValue(/\d+/)
  await row.getByLabel(`Need or Want for ${description}`).selectOption('Need')

  await page.reload()
  const reloaded = page.getByRole('row').filter({ hasText: description })
  await expect(reloaded.getByLabel(`Need or Want for ${description}`)).toHaveValue('Need')

  // A blank amount used to reach the ledger as 0.00; the slip refuses it and says why.
  await reloaded.getByRole('button', { name: 'Correct' }).click()
  const slip = page.getByRole('row').filter({ has: page.getByRole('heading', { name: 'Correcting this entry' }) })
  await slip.getByLabel('Amount').fill('')
  await slip.getByRole('button', { name: 'Save correction' }).click()
  await expect(slip.getByText('Enter an amount')).toBeVisible()

  await slip.getByLabel('Amount').fill('-43.75')
  await slip.getByRole('button', { name: 'Save correction' }).click()
  await expect(page.getByRole('row').filter({ hasText: description }).getByText('-43.75')).toBeVisible()

  // Escape leaves a correction unmade wherever focus sits on the slip.
  await page.getByRole('row').filter({ hasText: description }).getByRole('button', { name: 'Correct' }).click()
  const reopened = page.getByRole('row').filter({ has: page.getByRole('heading', { name: 'Correcting this entry' }) })
  await expect(reopened).toBeVisible()
  await reopened.getByLabel('Description').press('Escape')
  await expect(page.getByRole('heading', { name: 'Correcting this entry' })).toHaveCount(0)

  // Deleting an entry states what it is destroying first.
  await page.getByRole('row').filter({ hasText: description }).getByRole('button', { name: 'Delete' }).click()
  const confirm = page.getByRole('alertdialog')
  await expect(confirm).toContainText(description)
  await expect(confirm).toContainText('-43.75')
  await confirm.getByRole('button', { name: 'Delete entry' }).click()
  await expect(page.getByRole('row').filter({ hasText: description })).toHaveCount(0)
})
