import { test, expect, type Page } from '@playwright/test'

async function logIn(page: Page) {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()
}

async function addCategory(page: Page, name: string) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Categories' }).click()
  await expect(page.getByRole('heading', { name: 'Categories' })).toBeVisible()
  await page.getByLabel('Name').fill(name)
  await page.getByRole('button', { name: 'Add category' }).click()
  await expect(page.getByRole('row').filter({ hasText: name })).toBeVisible()
}

/** Import has nothing to offer — the rules memo included — until there is an account to import into. */
async function addAccount(page: Page, name: string) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible()
  await page.getByLabel('Name').fill(name)
  await page.getByLabel('Type').selectOption('CreditCard')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: name })).toBeVisible()
}

/** The rules page is reached from Import, never from the masthead. */
async function openRulesPage(page: Page) {
  const sections = page.getByRole('navigation', { name: 'Sections' })
  await expect(sections.getByRole('link', { name: /rules/i })).toHaveCount(0)
  await sections.getByRole('link', { name: 'Import' }).click()
  await page.getByRole('link', { name: 'ignore rule' }).click()
  await expect(page.getByRole('heading', { name: 'Import rules' })).toBeVisible()
}

function classifySection(page: Page) {
  return page.getByRole('region', { name: 'Rules that classify a row' })
}

test('an assignment rule is written on the rules page, read back as a sentence, and deleted', async ({ page }) => {
  const stamp = Date.now()
  const categoryName = `E2E Groceries ${stamp}`
  const tagName = `E2E Pantry ${stamp}`
  const matchText = `KROGER ${stamp}`

  await logIn(page)
  await addAccount(page, `E2E Assign ${stamp}`)
  await addCategory(page, categoryName)
  await openRulesPage(page)

  // Classify first, skip second: the page opens on the kind of rule the household writes most.
  const headings = page.getByRole('heading', { level: 2 })
  await expect(headings.nth(0)).toHaveText('Rules that classify a row')
  await expect(headings.nth(1)).toHaveText('Rules that skip a row')

  const classify = classifySection(page)
  // Nothing written yet is a state, not a fault.
  await expect(classify.getByText('No rules yet. Write one from a row in an import preview.')).toBeVisible()
  await expect(classify.getByRole('alert')).toHaveCount(0)

  // A rule that sets nothing cannot be saved, and the reason is printed, not hidden on hover.
  await classify.getByLabel('These words').fill(matchText)
  const save = classify.getByRole('button', { name: 'Save rule' })
  await expect(save).toBeDisabled()
  await expect(classify.getByText('Set a category, Need or Want, or a tag to save this rule')).toBeVisible()

  await classify.getByLabel('Match').selectOption('Contains')
  await classify.getByLabel('Category').selectOption({ label: categoryName })
  await classify.getByLabel('Need or want').selectOption('Need')
  await classify.getByRole('combobox', { name: 'Add a tag' }).fill(tagName)
  await classify.getByRole('combobox', { name: 'Add a tag' }).press('Enter')
  await expect(classify.locator('.tag-mark', { hasText: tagName })).toBeVisible()
  await expect(classify.getByText('Set a category, Need or Want, or a tag to save this rule')).toHaveCount(0)
  await save.click()

  // Read back as a sentence: the match, what it assigns, and where it applies.
  const ruleRow = classify.getByRole('row').filter({ hasText: matchText })
  await expect(ruleRow).toContainText(`contains ${matchText}`)
  await expect(ruleRow).toContainText(`${categoryName} · Need · +${tagName}`)
  await expect(ruleRow).toContainText('All accounts')
  await expect(classify.getByText('No rules yet.')).toHaveCount(0)

  // A rule that sets only Need/Want prints only that — no dashes standing in for the rest.
  const needOnlyText = `SQ ${stamp}`
  await classify.getByLabel('Match').selectOption('StartsWith')
  await classify.getByLabel('These words').fill(needOnlyText)
  await classify.getByLabel('Need or want').selectOption('Want')
  await classify.getByRole('button', { name: 'Save rule' }).click()
  const needOnlyRow = classify.getByRole('row').filter({ hasText: needOnlyText })
  await expect(needOnlyRow).toContainText(`starts with ${needOnlyText}`)
  await expect(needOnlyRow.getByRole('cell').nth(1)).toHaveText('Want')

  // The same words, match and scope again is the same rule, refused at the field.
  await classify.getByLabel('Match').selectOption('Contains')
  await classify.getByLabel('These words').fill(matchText)
  await classify.getByLabel('Need or want').selectOption('Want')
  await classify.getByRole('button', { name: 'Save rule' }).click()
  await expect(classify.locator('.field-error')).toContainText('That rule is already written')

  // Delete is confirmed in the page, never in a browser dialog.
  await ruleRow.getByRole('button', { name: 'Delete' }).click()
  await page.getByRole('button', { name: 'Delete rule' }).click()
  await expect(ruleRow).toHaveCount(0)
  await expect(needOnlyRow).toBeVisible()
})

test('a category an assignment rule uses cannot be deleted, and the categories page says how many rules', async ({
  page,
}) => {
  const stamp = Date.now()
  const categoryName = `E2E Ruled ${stamp}`

  await logIn(page)
  await addAccount(page, `E2E Assign ${stamp}`)
  await addCategory(page, categoryName)
  await openRulesPage(page)

  const classify = classifySection(page)
  await classify.getByLabel('These words').fill(`RULED ${stamp}`)
  await classify.getByLabel('Category').selectOption({ label: categoryName })
  await classify.getByRole('button', { name: 'Save rule' }).click()
  await expect(classify.getByRole('row').filter({ hasText: `RULED ${stamp}` })).toBeVisible()

  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Categories' }).click()
  const categoryRow = page.getByRole('row').filter({ hasText: categoryName })
  await categoryRow.getByRole('button', { name: 'Delete' }).click()

  await expect(page.getByRole('alert')).toContainText('used by 1 assignment rule')
  await expect(categoryRow).toBeVisible()
})
