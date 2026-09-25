import { test, expect } from '@playwright/test'

test('station cards support selection, status filtering, export and narrow screens', async ({ page }) => {
  await page.addInitScript(() => sessionStorage.setItem('smart-solar.access-token', 'test-token'))
  const statuses: string[] = []
  const station = { id: 'station-1', stationCode: 'MAT-SOLAR-001', name: 'Matara Solar Hub', address: 'Matara, Sri Lanka', capacityKWh: 100, availableBatterySlots: 10, status: 'ACTIVE' }
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url())
    if (url.pathname.endsWith('/auth/me')) return route.fulfill({ json: { id: 'staff', fullName: 'Thyaga', role: 'BACKOFFICE', status: 'ACTIVE' } })
    statuses.push(url.searchParams.get('status') ?? '')
    await route.fulfill({ json: { items: [station, { ...station, id: 'station-2', name: 'Galle Solar Hub', stationCode: 'GAL-SOLAR-002' }], totalCount: 2 } })
  })
  await page.setViewportSize({ width: 1672, height: 941 })
  await page.goto('/stations')
  await expect(page.getByRole('heading', { name: 'Microgrid node management' })).toBeVisible()
  await expect(page.locator('.station-metrics')).toHaveCSS('display', 'grid')
  await expect(page.locator('.station-browser')).toHaveCSS('display', 'grid')
  await expect(page.locator('.app-frame')).toHaveClass(/accounts-theme/)
  expect(await page.locator('.station-metrics').evaluate(el => getComputedStyle(el).gridTemplateColumns.split(' ').length)).toBe(4)
  const overview = page.getByRole('complementary', { name: 'Station overview' })
  await expect(overview.getByRole('heading', { name: 'Matara Solar Hub' })).toBeVisible()
  await page.screenshot({ path: 'test-results/stations-desktop.png', fullPage: true })
  await page.getByRole('button', { name: 'View overview for Galle Solar Hub' }).click()
  await expect(overview.getByRole('heading', { name: 'Galle Solar Hub' })).toBeVisible()
  await page.getByLabel('Station status filter').selectOption('ACTIVE')
  await expect.poll(() => statuses.includes('ACTIVE')).toBe(true)
  const download = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Export this page' }).click()
  expect((await download).suggestedFilename()).toBe('stations-page-1.csv')
  await page.setViewportSize({ width: 390, height: 844 })
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.screenshot({ path: 'test-results/stations-mobile.png', fullPage: true })
})
