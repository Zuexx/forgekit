import { defineConfig, devices } from "@playwright/test"

const baseURL = process.env.BETTER_AUTH_URL ?? "http://localhost:3000"

export default defineConfig({
    testDir: "./e2e",
    // These tests share one database, so they run in sequence rather than racing to
    // create the same accounts.
    fullyParallel: false,
    workers: 1,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 1 : 0,
    reporter: process.env.CI ? "list" : "html",
    use: {
        baseURL,
        trace: "on-first-retry",
    },
    projects: [
        { name: "chromium", use: { ...devices["Desktop Chrome"] } },
    ],
    webServer: {
        // Runs the production build, so what is exercised is what would be deployed.
        command: "pnpm start",
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
    },
})
