import { test, expect } from '@playwright/test'

for (const role of ['BACKOFFICE', 'GRID_OPERATOR', 'PROSUMER', 'UNKNOWN']) {
  const allowed = role === 'BACKOFFICE' || role === 'GRID_OPERATOR'
  for (const restore of [false, true]) {
    test(`${role} ${restore ? 'restored session' : 'login'} ${allowed ? 'opens workspace' : 'denies web access'}`, async ({ page }) => {
      if (restore) await page.addInitScript(() => sessionStorage.setItem('smart-solar.access-token', 'test-token'))
      await page.route('**/api/**', async route => {
        const path = new URL(route.request().url()).pathname
        const user = { id: 'test-user', fullName: 'Test User', email: 'staff@example.com', role, status: 'ACTIVE' }
        if (path.endsWith('/auth/login')) return route.fulfill({ json: { accessToken: 'test-token', user } })
        if (path.endsWith('/auth/me')) return route.fulfill({ json: user })
        return route.fulfill({ json: { items: [], totalCount: 0, recentReservations: [] } })
      })
      await page.goto(restore ? '/dashboard' : '/login')
      if (!restore) {
        await page.getByLabel('Staff email').fill('staff@example.com')
        await page.getByLabel('Password', { exact: true }).fill('test-password')
        await page.getByRole('button', { name: 'Show password', exact: true }).click()
        await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('type', 'text')
        await page.getByRole('button', { name: 'Hide password', exact: true }).click()
        await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('type', 'password')
        await page.getByRole('button', { name: 'Enter workspace' }).click()
      }
      if (allowed) {
        await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
        await expect(page.getByRole('link', { name: 'Users', exact: true })).toHaveCount(role === 'BACKOFFICE' ? 1 : 0)
      } else {
        await expect(page).toHaveURL(/\/login$/)
        await expect(page.getByRole('button', { name: 'Enter workspace' })).toBeVisible()
        if (!restore) await expect(page.getByRole('alert')).toContainText('Prosumers should use the Android app')
        expect(await page.evaluate(() => sessionStorage.getItem('smart-solar.access-token'))).toBeNull()
      }
    })
  }
}
