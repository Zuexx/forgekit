import { expect, test } from "@playwright/test"

import { shot } from "./evidence"

// Feature map: features/locale.md.
const F = "locale"

test("locale-prefix: /zh-TW serves Traditional Chinese, the default locale is unprefixed", async ({ page }) => {
    await page.goto("/zh-TW/sign-in")
    await expect(page.getByRole("button", { name: "登入", exact: true })).toBeVisible()
    await shot(page, F, "01-zh-TW-sign-in")

    await page.goto("/en/sign-in")
    await expect(page).toHaveURL(/\/sign-in$/)
    expect(new URL(page.url()).pathname).toBe("/sign-in")
    await expect(page.getByRole("button", { name: "Login", exact: true })).toBeVisible()
})

test("locale-switch: the language menu switches and the choice sticks", async ({ page }) => {
    await page.goto("/sign-in")
    await page.getByRole("button", { name: "Change language" }).click()
    await page.getByText("中", { exact: true }).click()
    await expect(page).toHaveURL(/\/zh-TW\/sign-in$/)
    await expect(page.getByRole("button", { name: "登入", exact: true })).toBeVisible()
    await shot(page, F, "02-switched-to-zh-TW")

    // NEXT_LOCALE is set, so the unprefixed URL now resolves to the chosen locale.
    await page.goto("/sign-in")
    await expect(page).toHaveURL(/\/zh-TW\/sign-in$/)
    await shot(page, F, "03-choice-persisted")
})
