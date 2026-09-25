import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  use: { baseURL: 'http://localhost:5181', channel: 'msedge', headless: true },
  webServer: { command: 'npm run dev -- --host localhost --port 5181 --strictPort', url: 'http://localhost:5181', reuseExistingServer: false },
})
