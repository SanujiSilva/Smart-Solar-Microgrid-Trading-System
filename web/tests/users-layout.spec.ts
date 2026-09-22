import { test, expect } from '@playwright/test'

test('user management supports filters, staff creation, export and mobile layout', async ({ page }) => {
  test.setTimeout(60_000)
  await page.addInitScript(() => sessionStorage.setItem('smart-solar.access-token', 'test-token'))
  const users = [{ id: 'staff-1', fullName: 'Grid operator', role: 'GRID_OPERATOR', email: 'operator@example.invalid', phone: '0771234567', status: 'ACTIVE', nic: null }]
  const roles: string[] = []
  let created: Record<string, string> | null = null
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url())
    if (url.pathname.endsWith('/auth/me')) return route.fulfill({ json: { ...users[0], fullName: 'Thyaga', role: 'BACKOFFICE' } })
    if (route.request().method() === 'POST') {
      created = route.request().postDataJSON()
      return route.fulfill({ status: 201, json: { ...created, id: 'new-staff' } })
    }
    roles.push(url.searchParams.get('role') ?? '')
    await route.fulfill({ json: { items: users, totalCount: url.searchParams.get('status') === 'DEACTIVATED' ? 0 : 1 } })
  })
  await page.setViewportSize({ width: 1672, height: 941 })
  await page.goto('/users')
  await expect(page.getByRole('heading', { name: 'User management' })).toBeVisible()
  await expect(page.getByRole('cell', { name: 'operator@example.invalid 0771234567' })).toBeVisible()
  await page.screenshot({ path: 'test-results/users-desktop.png', fullPage: true })
  await page.getByLabel('Account role').selectOption('GRID_OPERATOR')
  await expect.poll(() => roles.includes('GRID_OPERATOR')).toBe(true)
  const download = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Export this page' }).click()
  expect((await download).suggestedFilename()).toBe('users-page-1.csv')
  const form = page.locator('.staff-form')
  await form.getByLabel('Full name').fill('New operator')
  await form.getByLabel('Email', { exact: true }).fill('new@example.invalid')
  await form.getByLabel('Phone', { exact: true }).fill('0779876543')
  await form.getByLabel('Password', { exact: true }).fill('Example-test-only-123')
  await form.getByRole('button', { name: 'Create staff account', exact: true }).click()
  await expect(page.getByText('Staff account created.', { exact: true })).toBeVisible()
  expect(created).toMatchObject({ fullName: 'New operator', role: 'GRID_OPERATOR' })
  await page.setViewportSize({ width: 390, height: 844 })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.screenshot({ path: 'test-results/users-mobile.png', fullPage: true })
  await page.getByRole('button', { name: 'Close staff form' }).click()
  await expect(form).toHaveCount(0)
  await page.getByRole('button', { name: 'Add staff member' }).click()
  await expect(page.locator('.staff-form')).toBeVisible()
})
