import { test, expect, type Locator, type Page } from '@playwright/test'

/**
 * One month's card export: a grocer, an airline, a coffee bar a rule judges but does not file, and a
 * payee nobody has taught the app about.
 */
const CARD_CSV = [
  'Posted Date,Reference,Payee,Amount',
  '08/14/2026,5501,KROGER #442,-84.19',
  '08/15/2026,5502,DELTA AIR LINES 0062,-412.80',
  "08/16/2026,5503,TRADER JOE'S #221,-63.40",
  '08/16/2026,5504,SQ *BLUE BOTTLE,-6.75',
].join('\r\n')

/** Upload a CSV to the account and give its columns their roles, arriving at station 3. */
async function previewCsv(page: Page, accountName: string, csv: string, { map }: { map: boolean }) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await expect(page.getByRole('heading', { name: 'Import', exact: true })).toBeVisible()
  await page.getByLabel('Account').selectOption({ label: accountName })
  await page
    .getByLabel('CSV file')
    .setInputFiles({ name: 'august.csv', mimeType: 'text/csv', buffer: Buffer.from(csv, 'utf-8') })
  await page.getByRole('button', { name: 'Read the file' }).click()
  if (map) {
    await page.getByLabel('Role for column Posted Date').selectOption('date')
    await page.getByLabel('Role for column Payee').selectOption('description')
    await page.getByLabel('Role for column Amount').selectOption('amount')
  }
  await page.getByRole('button', { name: 'Preview all rows' }).click()
  await expect(page.getByRole('region', { name: 'Preview' })).toBeVisible()
}

async function openRulesPage(page: Page) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Import' }).click()
  await page.getByRole('link', { name: 'Import rules' }).click()
  await expect(page.getByRole('heading', { name: 'Import rules' })).toBeVisible()
  return page.getByRole('region', { name: 'Rules that classify a row' })
}

async function expectTally(page: Page, classified: number, forReview: number) {
  const tally = page.locator('.exceptions')
  await expect(tally.locator('.exception', { hasText: 'Classified' })).toContainText(String(classified))
  await expect(tally.locator('.exception', { hasText: 'For review' })).toContainText(String(forReview))
}

async function logIn(page: Page) {
  await page.goto('/login')
  await page.getByLabel('Email').fill('user1@household.local')
  await page.getByLabel('Password').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Log in' }).click()
  await expect(page.getByRole('link', { name: 'MoneyRight' })).toBeVisible()
}

async function addAccount(page: Page, name: string) {
  await page.getByRole('navigation', { name: 'Sections' }).getByRole('link', { name: 'Accounts' }).click()
  await expect(page.getByRole('heading', { name: 'Accounts' })).toBeVisible()
  await page.getByLabel('Name').fill(name)
  await page.getByLabel('Type').selectOption('CreditCard')
  await page.getByRole('button', { name: 'Add account' }).click()
  await expect(page.getByRole('row').filter({ hasText: name })).toBeVisible()
}

/** Write one classify rule on the rules page, scoped to the account under test. */
async function writeRule(
  classify: Locator,
  rule: { match: 'Contains' | 'StartsWith' | 'Equals'; words: string; account: string; category?: string; needWant?: string; tag?: string },
) {
  await classify.getByLabel('Match').selectOption(rule.match)
  await classify.getByLabel('These words').fill(rule.words)
  await classify.getByLabel('For').selectOption({ label: rule.account })
  if (rule.category) {
    const category = classify.getByRole('combobox', { name: 'Category' })
    await category.fill(rule.category)
    await category.press('Enter')
    await expect(category).toHaveValue(rule.category)
  }
  if (rule.needWant) await classify.getByLabel('Need or want').selectOption(rule.needWant)
  if (rule.tag) {
    const tag = classify.getByRole('combobox', { name: 'Add a tag' })
    await tag.fill(rule.tag)
    await tag.press('Enter')
    await expect(classify.locator('.tag-mark', { hasText: rule.tag })).toBeVisible()
  }
  await classify.getByRole('button', { name: 'Save rule' }).click()
  await expect(classify.getByRole('row').filter({ hasText: rule.words }).filter({ hasText: rule.account })).toBeVisible()
}

function entry(page: Page, description: string) {
  return page.locator('.preview-entry', { hasText: description })
}

test('rules classify the preview, and the import writes what the preview said', async ({ page }) => {
  const stamp = Date.now()
  const accountName = `E2E Classify ${stamp}`
  const groceries = `E2E Groceries ${stamp}`
  const travel = `E2E Travel ${stamp}`
  const vacation = `E2E Vacation ${stamp}`

  await logIn(page)
  await addAccount(page, accountName)

  const classify = await openRulesPage(page)
  await writeRule(classify, { match: 'Contains', words: 'KROGER', account: accountName, category: groceries, needWant: 'Need' })
  // Two rules about one airline row: one files it, the other tags it.
  await writeRule(classify, { match: 'StartsWith', words: 'DELTA', account: accountName, category: travel })
  await writeRule(classify, { match: 'Contains', words: 'AIR LINES', account: accountName, tag: vacation })
  // Judges the coffee without filing it.
  await writeRule(classify, { match: 'StartsWith', words: 'SQ *', account: accountName, needWant: 'Need' })

  await previewCsv(page, accountName, CARD_CSV, { map: true })

  // Every row going in says how it will be filed, and cites what decided it — one rule by name,
  // several by count — and the row nobody taught the app about says so plainly.
  await expect(entry(page, 'KROGER #442').locator('.preview-filed')).toHaveText(`${groceries} · Need · contains KROGER`)
  await expect(entry(page, 'DELTA AIR LINES 0062').locator('.preview-filed')).toHaveText(
    `${travel} · Want · +${vacation} · 2 rules`,
  )
  await expect(entry(page, "TRADER JOE'S #221").locator('.preview-filed')).toHaveText('No category · Want · for review')
  // Judged but not filed: it cites its rule, and is still for review until it has a category.
  await expect(entry(page, 'SQ *BLUE BOTTLE').locator('.preview-filed')).toHaveText(
    'No category · Need · starts with SQ * · for review',
  )

  await expectTally(page, 2, 2)
  await expect(page.getByText('Each lands with no category, as a Want unless a rule said otherwise')).toBeVisible()

  await page.getByRole('button', { name: 'Import 4 rows' }).click()
  await expect(page.getByRole('status')).toHaveText('Imported 4 · skipped 0 by a rule · skipped 0 as duplicates')

  // Only the rows no rule filed are left for a person: the queue is two long, not four.
  await expect(page.getByText(`2 transactions on ${accountName} are waiting for a category.`)).toBeVisible()
  await page.getByRole('link', { name: 'Review 2 transactions' }).click()
  await expect(page.getByRole('heading', { name: 'Transactions' })).toBeVisible()
  const rows = page.locator('tbody tr')
  await expect(rows).toHaveCount(2)
  await expect(rows.getByLabel("Category for TRADER JOE'S #221")).toHaveValue('')
  await expect(rows.getByLabel("Need or Want for TRADER JOE'S #221")).toHaveValue('Want')
  await expect(rows.getByLabel('Need or Want for SQ *BLUE BOTTLE')).toHaveValue('Need')

  // The whole account: the classified rows carry what the rules gave them.
  await page.getByLabel('Filter by category').selectOption('')
  await expect(rows).toHaveCount(4)
  const kroger = rows.filter({ hasText: 'KROGER #442' })
  await expect(kroger.getByLabel('Category for KROGER #442').locator('option:checked')).toHaveText(groceries)
  await expect(kroger.getByLabel('Need or Want for KROGER #442')).toHaveValue('Need')
  const delta = rows.filter({ hasText: 'DELTA AIR LINES 0062' })
  await expect(delta.getByLabel('Category for DELTA AIR LINES 0062').locator('option:checked')).toHaveText(travel)
  await expect(delta.getByLabel('Need or Want for DELTA AIR LINES 0062')).toHaveValue('Want')
  await expect(delta.locator('.tag-mark')).toHaveText([vacation])

  // The same export again, with an autopay the household never wants. Rows that are leaving —
  // already in the ledger, or struck by a rule — carry no filing at all: their own mark wins.
  await previewCsv(page, accountName, `${CARD_CSV}\r\n08/17/2026,5505,AUTOPAY THANK YOU,312.00`, { map: false })

  await expect(entry(page, 'KROGER #442')).toContainText('Already imported')
  await expect(entry(page, 'KROGER #442').locator('.preview-filed')).toHaveCount(0)

  const autopay = entry(page, 'AUTOPAY THANK YOU')
  await expect(autopay.locator('.preview-filed')).toHaveText('No category · Want · for review')
  await autopay.getByRole('button', { name: 'Ignore rows like this' }).click()
  await page.getByLabel('These words').fill('AUTOPAY')
  await page.getByRole('button', { name: 'Add rule' }).click()
  await expect(autopay).toContainText('Skipped · contains AUTOPAY')
  await expect(autopay.locator('.preview-filed')).toHaveCount(0)
  await expectTally(page, 0, 0)
})

test('with no rules every row is for review, and once every row is classified nothing is left waiting', async ({
  page,
}) => {
  const stamp = Date.now()
  const accountName = `E2E All Filed ${stamp}`
  const groceries = `E2E Filed Groceries ${stamp}`
  const travel = `E2E Filed Travel ${stamp}`
  const csv = ['Posted Date,Reference,Payee,Amount', '08/14/2026,6601,KROGER #442,-84.19', '08/15/2026,6602,DELTA AIR LINES 0062,-412.80'].join('\r\n')

  await logIn(page)
  await addAccount(page, accountName)

  // Day one: nothing has been taught, so nothing is filed — said plainly, not as a fault.
  await previewCsv(page, accountName, csv, { map: true })
  await expect(entry(page, 'KROGER #442').locator('.preview-filed')).toHaveText('No category · Want · for review')
  await expect(entry(page, 'DELTA AIR LINES 0062').locator('.preview-filed')).toHaveText('No category · Want · for review')
  await expectTally(page, 0, 2)
  await expect(page.getByRole('alert')).toHaveCount(0)

  const classify = await openRulesPage(page)
  await writeRule(classify, { match: 'Contains', words: 'KROGER', account: accountName, category: groceries })
  await writeRule(classify, { match: 'Contains', words: 'DELTA', account: accountName, category: travel })

  // Nothing was imported yet, so the mapping is not remembered: map the columns again.
  await previewCsv(page, accountName, csv, { map: true })
  await expectTally(page, 2, 0)
  await expect(page.locator('.close-note')).toHaveCount(0)

  await page.getByRole('button', { name: 'Import 2 rows' }).click()
  await expect(page.getByRole('status')).toHaveText('Imported 2 · skipped 0 by a rule · skipped 0 as duplicates')
  // Nothing is waiting, so the handoff to the review queue does not print at all.
  await expect(page.getByText('waiting for a category')).toHaveCount(0)
  await expect(page.getByRole('link', { name: /^Review/ })).toHaveCount(0)
})
