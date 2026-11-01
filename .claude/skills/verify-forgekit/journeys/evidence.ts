import { mkdirSync, writeFileSync } from "node:fs"
import { join } from "node:path"

import type { Page } from "@playwright/test"

/** Writes a named artifact under $VF_EVIDENCE/<feature>/ and returns its path. */
function target(feature: string, name: string) {
    const dir = join(process.env.VF_EVIDENCE!, feature)
    mkdirSync(dir, { recursive: true })
    return join(dir, name)
}

export async function shot(page: Page, feature: string, name: string) {
    await page.screenshot({ path: target(feature, `${name}.png`), fullPage: true })
}

export function record(feature: string, name: string, data: unknown) {
    writeFileSync(target(feature, `${name}.json`), JSON.stringify(data, null, 2))
}
