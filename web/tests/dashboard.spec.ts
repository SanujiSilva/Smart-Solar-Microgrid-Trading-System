import { test, expect } from '@playwright/test'

test('dashboard fits desktop and mobile and opens the pending queue', async ({ page }) => {
  test.setTimeout(60_000)
  let databaseHealthy = true
  await page.addInitScript(() => sessionStorage.setItem('smart-solar.access-token', 'test-token'))
  const requestedStatuses: string[] = []
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url())
    let data: unknown
    if (url.pathname.endsWith('/auth/me')) data = { id: 'staff', fullName: 'Thyaga', role: 'BACKOFFICE', status: 'ACTIVE' }
    else if (url.pathname.endsWith('/health/ready')) return route.fulfill({ status: databaseHealthy ? 200 : 503, json: { status: databaseHealthy ? 'Healthy' : 'Unhealthy' } })
    else if (url.pathname.endsWith('/health')) data = { status: 'Healthy' }
    else if (url.pathname.endsWith('/reservations/dashboard')) data = { pendingReservations: 2, activeStations: 1, openSlots: 1, completedTransfers: 1, availableSlotCapacity: 40, recentReservations: [{ id: 'r1', reservationCode: 'RSV-DEMO', prosumerNIC: '200222787078', reservationDateTime: '2030-09-21T10:00:00Z', energyAmount: 10, status: 'COMPLETED' }] }
    else {
      if (url.pathname.endsWith('/reservations/search')) requestedStatuses.push(url.searchParams.get('status') ?? '')
      data = { items: [], totalCount: url.pathname.endsWith('/users') ? 3 : 0 }
    }
    await route.fulfill({ json: data })
  })
  await page.setViewportSize({ width: 1672, height: 941 })
  await page.goto('/dashboard')
  await expect(page.getByRole('heading', { name: 'Control room' })).toBeVisible()
  await expect(page.locator('.metric-card')).toHaveCount(6)
  await expect(page.getByText('Database connected', { exact: true })).toBeVisible()
  await expect(page.getByText('API online', { exact: true })).toBeVisible()
  const download = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Export report' }).click()
  expect((await download).suggestedFilename()).toBe('microgrid-overview.csv')
  await page.getByLabel('Search recent reservations').fill('no-match')
  await expect(page.getByText('No recent reservations match these filters.')).toBeVisible()
  await page.getByLabel('Search recent reservations').clear()
  await page.getByLabel('Recent reservation period').selectOption('today')
  await expect(page.getByText('No recent reservations match these filters.')).toBeVisible()
  await page.getByLabel('Recent reservation period').selectOption('recent')
  await page.screenshot({ path: 'test-results/dashboard-desktop.png', fullPage: true })
  await page.setViewportSize({ width: 390, height: 844 })
  await expect(page.getByRole('link', { name: 'Review pending' })).toBeVisible()
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true)
  await page.screenshot({ path: 'test-results/dashboard-mobile.png', fullPage: true })
  databaseHealthy = false
  await page.getByRole('button', { name: 'Refresh overview' }).click()
  await expect(page.getByText('Database unavailable', { exact: true })).toBeVisible()
  await page.getByRole('link', { name: 'Review pending' }).click()
  await expect.poll(() => requestedStatuses.includes('PENDING')).toBe(true)
})
