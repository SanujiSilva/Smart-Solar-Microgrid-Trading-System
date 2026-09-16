import { test, expect, type Page } from '@playwright/test'

const user = { id: 'user-1', nic: '200012345678', fullName: 'Test Prosumer', email: 'test@example.invalid', phone: '0771234567', role: 'PROSUMER', status: 'PENDING' }
const station = { id: 'station-1', stationCode: 'NODE-1', name: 'Colombo Station', address: 'Colombo', latitude: 6.9, longitude: 79.8, capacityKWh: 100, availableBatterySlots: 5, status: 'ACTIVE', operatingSchedule: { timeZoneId: 'Asia/Colombo', weeklyPeriods: [{ day: 'Monday', openMinuteOfDay: 480, closeMinuteOfDay: 1020 }] } }
const slot = { id: 'slot-1', startTime: '2030-01-01T08:00:00Z', endTime: '2030-01-01T09:00:00Z', capacity: 100, availableCapacity: 60, status: 'OPEN' }
const reservation = { id: 'reservation-1', reservationCode: 'RSV-1', prosumerNIC: user.nic, stationId: station.id, slotId: slot.id, energyAmount: 40, reservationDateTime: slot.startTime, status: 'PENDING', createdAt: '2029-12-30T08:00:00Z' }

async function setup(page: Page, role = 'BACKOFFICE') {
  const calls: { path: string; method: string; body: any }[] = []
  const account = structuredClone(user), node = structuredClone(station), booking = structuredClone(reservation)
  await page.addInitScript(() => sessionStorage.setItem('smart-solar.access-token', 'test-token'))
  page.on('dialog', dialog => dialog.accept())
  await page.route('**/api/**', async route => {
    const request = route.request(), path = new URL(request.url()).pathname.replace('/api', '')
    const method = request.method(), body = request.postDataJSON()
    calls.push({ path, method, body })
    let data: unknown = {}, status = 200
    if (path === '/auth/me') data = { ...user, role, status: 'ACTIVE' }
    else if (path === '/prosumers' || path === '/users') data = { items: [account], totalCount: 1 }
    else if (path === '/users/user-1/status') { account.status = body.status; data = account }
    else if (path === '/prosumers/200012345678') data = account
    else if (path === '/users/user-1') { if (method === 'PUT') Object.assign(account, body); data = account }
    else if (path === '/stations' && method === 'GET') data = { items: [node], totalCount: 1 }
    else if (path === '/stations' && method === 'POST') { Object.assign(node, body); data = node }
    else if (path === '/stations/station-1') { if (method === 'PUT') Object.assign(node, body); data = node }
    else if (path === '/stations/station-1/availability') { Object.assign(node, body); data = node }
    else if (path === '/stations/station-1/slots') data = method === 'GET' ? { items: [slot] } : { ...slot, ...body }
    else if (path === '/slots/slot-1/availability') data = { ...slot, ...body }
    else if (path === '/reservations/search') data = { items: [booking], totalCount: 1 }
    else if (path === '/reservations/reservation-1') { if (method === 'DELETE') booking.status = 'CANCELLED'; data = booking }
    else if (path === '/reservations/reservation-1/approve') { booking.status = 'APPROVED'; data = booking }
    else if (path === '/reservations/reservation-1/reject') { booking.status = 'REJECTED'; data = booking }
    else { status = 500; data = { title: 'Unexpected mock route ' + path } }
    await route.fulfill({ status, json: data, headers: { 'access-control-allow-origin': '*' } })
  })
  return calls
}

test('Backoffice approves a pending prosumer and can inspect details', async ({ page }) => {
  const calls = await setup(page)
  await page.goto('/prosumers')
  await page.getByRole('button', { name: 'Approve account' }).click()
  await expect(page.getByRole('status')).toHaveText('Account status updated.')
  expect(calls.find(c => c.path === '/users/user-1/status')?.body).toEqual({ status: 'ACTIVE' })
  await page.getByRole('button', { name: 'Details', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Account details' })).toBeVisible()
  await expect(page.getByLabel('Full name')).toBeDisabled()
})

test('Backoffice station editing preserves the server schedule and allows slot creation', async ({ page }) => {
  const calls = await setup(page)
  await page.goto('/stations')
  await page.getByRole('button', { name: 'Details / Manage' }).click()
  await page.getByLabel('Name', { exact: true }).fill('Updated station')
  await page.getByRole('button', { name: 'Save station', exact: true }).click()
  await expect(page.getByRole('status')).toHaveText('Station saved.')
  const update = calls.find(c => c.method === 'PUT' && c.path === '/stations/station-1')!
  expect(update.body.name).toBe('Updated station')
  expect(update.body.operatingSchedule).toEqual(station.operatingSchedule)
  expect(update.body).not.toHaveProperty('stationCode')
  await page.getByRole('button', { name: 'Add slot', exact: true }).click()
  await page.getByLabel('Start (local time)', { exact: true }).fill('2030-01-07T09:00')
  await page.getByLabel('End (local time)', { exact: true }).fill('2030-01-07T10:00')
  await page.getByRole('button', { name: 'Save slot', exact: true }).click()
  await expect(page.getByText('Slot saved.', { exact: true })).toBeVisible()
  expect(calls.some(c => c.method === 'POST' && c.path.endsWith('/slots'))).toBeTruthy()
})

test('operator changes availability without administrative fields', async ({ page }) => {
  const calls = await setup(page, 'GRID_OPERATOR')
  await page.goto('/stations')
  await page.getByRole('button', { name: 'Details / Manage' }).click()
  await expect(page.getByRole('button', { name: 'Add station', exact: true })).toHaveCount(0)
  await expect(page.getByLabel('Name', { exact: true })).toBeDisabled()
  await page.getByLabel('Available battery slots', { exact: true }).fill('3')
  await page.getByRole('button', { name: 'Save availability', exact: true }).click()
  await expect(page.getByText('Station saved.', { exact: true })).toBeVisible()
  expect(calls.find(c => c.path.endsWith('/station-1/availability'))?.body).toEqual({ availableBatterySlots: 3 })
  await page.getByRole('button', { name: 'Edit availability', exact: true }).click()
  await page.getByLabel('Available capacity (kWh)', { exact: true }).fill('50')
  await page.getByRole('button', { name: 'Save slot', exact: true }).click()
  await expect(page.getByText('Slot saved.', { exact: true })).toBeVisible()
  expect(calls.find(c => c.path === '/slots/slot-1/availability')?.body).toEqual({ availableCapacity: 50, status: 'OPEN' })
})

test('reservation detail supports approval and confirmed cancellation', async ({ page }) => {
  const calls = await setup(page)
  await page.goto('/reservations')
  await page.getByRole('button', { name: 'Details', exact: true }).click()
  await page.getByRole('button', { name: 'Approve', exact: true }).click()
  await expect(page.getByText('Reservation updated.', { exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Approve', exact: true })).toHaveCount(0)
  await page.getByRole('button', { name: 'Cancel reservation', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Cancel reservation', exact: true })).toHaveCount(0)
  expect(calls.some(c => c.path.endsWith('/approve') && c.method === 'PATCH')).toBeTruthy()
  expect(calls.some(c => c.path === '/reservations/reservation-1' && c.method === 'DELETE')).toBeTruthy()
})

test('expired API sessions return to login', async ({ page }) => {
  await setup(page)
  await page.goto('/prosumers')
  await expect(page.getByRole('button', { name: 'Approve account' })).toBeVisible()
  await page.route('**/api/prosumers?**', route => route.fulfill({ status: 401, json: { title: 'Expired' } }))
  await page.getByRole('button', { name: 'Refresh', exact: true }).click()
  await expect(page).toHaveURL(/\/login/)
})
