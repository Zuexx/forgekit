import { randomBytes } from "node:crypto"

import { expect, test } from "@playwright/test"

import { record, shot } from "./evidence"

// Feature map: features/auth.md. The email is written to evidence so the database row can
// be checked afterwards with `vf.sh sql`.
const F = "auth"
const PASSWORD = randomBytes(16).toString("base64url")
const email = `verify-${Date.now()}@test.local`

test.describe.configure({ mode: "serial" })

test("auth-guard: an anonymous visitor is sent to sign-in", async ({ page }) => {
    await page.goto("/en/dashboard")
    await expect(page).toHaveURL(/\/sign-in$/)
    await shot(page, F, "01-guard-redirected-to-sign-in")
})

test("auth-sign-up: a visitor creates an account and is signed in", async ({ page }) => {
    await page.goto("/en/sign-up")
    await page.fill('input[name="name"]', "Verify User")
    await page.fill('input[name="email"]', email)
    await page.fill('input[name="password"]', PASSWORD)
    await page.fill('input[name="confirmPassword"]', PASSWORD)
    await shot(page, F, "02-sign-up-filled")
    await page.click('button[type="submit"]')
    await expect(page).not.toHaveURL(/\/sign-up$/, { timeout: 15_000 })
    await shot(page, F, "03-after-sign-up")

    const session = await page.evaluate(async () => (await fetch("/api/auth/get-session")).json())
    record(F, "03-session", { email, url: page.url(), session })
    expect(session?.user?.email).toBe(email)
})

test("auth-sign-in: the account signs in again and survives a reload", async ({ page }) => {
    await page.goto("/en/sign-in")
    await page.fill('input[name="email"]', email)
    await page.fill('input[name="password"]', PASSWORD)
    await page.click('button[type="submit"]')
    await expect(page).not.toHaveURL(/\/sign-in$/, { timeout: 15_000 })
    await page.reload()
    const session = await page.evaluate(async () => (await fetch("/api/auth/get-session")).json())
    record(F, "04-session-after-reload", { url: page.url(), session })
    await shot(page, F, "04-signed-in-after-reload")
    expect(session?.user?.email).toBe(email)
})

test("auth-sign-in-wrong-password: a wrong password is refused", async ({ page }) => {
    await page.goto("/en/sign-in")
    await page.fill('input[name="email"]', email)
    await page.fill('input[name="password"]', randomBytes(16).toString("base64url"))
    await page.click('button[type="submit"]')
    // The refusal is a toast; the URL alone would also pass if the submit never fired.
    await expect(page.getByText(/Sign in failed/)).toBeVisible({ timeout: 15_000 })
    await expect(page).toHaveURL(/\/sign-in/)
    await shot(page, F, "05-wrong-password")
    const session = await page.evaluate(async () => (await fetch("/api/auth/get-session")).json())
    expect(session?.user).toBeFalsy()
})
