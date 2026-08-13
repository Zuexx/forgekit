import { describe, expect, it } from "vitest"

import { parseEnvList } from "./auth.config"

describe("parseEnvList", () => {
    it("treats an unset variable as no entries", () => {
        expect(parseEnvList(undefined)).toEqual([])
    })

    it("treats an empty variable as no entries", () => {
        // The distinction matters: a stray "" entry in adminUserIds would be compared
        // against user ids, and an empty trustedOrigins entry would widen what the
        // callback validator accepts.
        expect(parseEnvList("")).toEqual([])
    })

    it("ignores whitespace-only values", () => {
        expect(parseEnvList("   ")).toEqual([])
    })

    it("splits on commas", () => {
        expect(parseEnvList("a,b,c")).toEqual(["a", "b", "c"])
    })

    it("trims surrounding whitespace", () => {
        expect(parseEnvList(" a , b ")).toEqual(["a", "b"])
    })

    it("drops empty entries from trailing or doubled commas", () => {
        expect(parseEnvList("a,,b,")).toEqual(["a", "b"])
    })

    it("keeps a single entry", () => {
        expect(parseEnvList("https://app.example.com")).toEqual([
            "https://app.example.com",
        ])
    })
})
