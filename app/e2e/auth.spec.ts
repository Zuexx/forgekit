import { randomBytes } from "node:crypto"

import { expect, test } from "@playwright/test"

/**
 * Covers the path no unit test reaches: browser form to Hono route to Better Auth to
 * PostgreSQL. The database adapter was misconfigured for the life of this kit and every
 * auth query threw, while type checks, lint, unit tests and the production build all
 * stayed green — only a request that actually reaches the database catches that.
 */

/** A fresh address per run, so a rerun against the same database does not collide. */
function newEmail() {
    return `e2e-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@test.local`
}

/**
 * Both are generated per run and hold no literal at all — a secret scanner flags any
 * string assigned to a password, including a harmless prefix on a random value. Random
 * also means a rerun never shares an account password, and that the second value cannot
 * accidentally equal the first.
 */
const PASSWORD = randomBytes(16).toString("base64url")
const WRONG_PASSWORD = randomBytes(16).toString("base64url")

test.describe("authentication", () => {
    test("an anonymous visitor is sent to sign-in", async ({ page }) => {
        await page.goto("/en/dashboard")

        await expect(page).toHaveURL(/\/sign-in$/)
    })

    test("a visitor can create an account and lands signed in", async ({ page }) => {
        const email = newEmail()

        await page.goto("/en/sign-up")
        await page.fill('input[name="name"]', "E2E User")
        await page.fill('input[name="email"]', email)
        await page.fill('input[name="password"]', PASSWORD)
        await page.fill('input[name="confirmPassword"]', PASSWORD)
        await page.click('button[type="submit"]')

        // Signing up establishes a session, so the sign-up route itself becomes
        // off-limits and the proxy moves the visitor on.
        await expect(page).not.toHaveURL(/\/sign-up$/, { timeout: 15_000 })

        const session = await page.evaluate(async () => {
            const res = await fetch("/api/auth/get-session")
            return res.ok ? await res.json() : null
        })

        expect(session?.user?.email).toBe(email)
    })

    test("a registered visitor can sign in and the session survives a reload", async ({
        page,
    }) => {
        const email = newEmail()

        // Register through the API so this test fails for sign-in reasons only.
        const signUp = await page.request.post("/api/auth/sign-up/email", {
            data: { email, password: PASSWORD, name: "Returning User" },
        })
        expect(signUp.ok()).toBe(true)
        await page.context().clearCookies()

        await page.goto("/en/sign-in")
        await page.fill('input[name="email"]', email)
        await page.fill('input[name="password"]', PASSWORD)
        await page.click('button[type="submit"]')

        await expect(page).not.toHaveURL(/\/sign-in$/, { timeout: 15_000 })

        await page.reload()
        const session = await page.evaluate(async () => {
            const res = await fetch("/api/auth/get-session")
            return res.ok ? await res.json() : null
        })

        expect(session?.user?.email).toBe(email)
    })

    test("the wrong password does not sign anyone in", async ({ page }) => {
        const email = newEmail()
        await page.request.post("/api/auth/sign-up/email", {
            data: { email, password: PASSWORD, name: "Careful User" },
        })
        await page.context().clearCookies()

        await page.goto("/en/sign-in")
        await page.fill('input[name="email"]', email)
        await page.fill('input[name="password"]', WRONG_PASSWORD)
        await page.click('button[type="submit"]')

        await expect(page).toHaveURL(/\/sign-in/)

        const session = await page.evaluate(async () => {
            const res = await fetch("/api/auth/get-session")
            return res.ok ? await res.json() : null
        })

        expect(session?.user).toBeFalsy()
    })
})
