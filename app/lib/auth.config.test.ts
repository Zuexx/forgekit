import { afterEach, describe, expect, it, vi } from "vitest"

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

describe("database option", () => {
    afterEach(() => {
        delete process.env.Database__Provider
        vi.resetModules()
    })

    it("is passed in the shape Better Auth expects for a pg Pool when Postgres is selected", async () => {
        // Regression guard. Better Auth does not type-check this option, so the wrong
        // shape compiles and then fails on the first query:
        //
        //   { db: pool, type: "postgres" }  ->  the adapter uses `db` as the Kysely
        //                                       instance, and every call throws
        //                                       "db.selectFrom is not a function"
        //
        // A pg.Pool must be passed directly so the adapter builds Kysely itself. Only a
        // real Kysely instance — what lib/db/mssql.ts and lib/db/sqlite.ts export — takes
        // the wrapper. sqlite is the default now, so this test selects postgres explicitly
        // to keep guarding the shape that trips this class of bug.
        process.env.Database__Provider = "Postgres"
        vi.resetModules()
        const { database } = await import("./auth.config")

        expect(database).not.toHaveProperty("type")
        expect(database).toHaveProperty("connect")
    })

    it("selects the sqlite adapter by default", async () => {
        delete process.env.Database__Provider
        vi.resetModules()
        const { database } = await import("./auth.config")
        expect(database).toHaveProperty("type", "sqlite")
    })
})

describe("normalizeProvider", () => {
    it("defaults an unset value to sqlite", async () => {
        const { normalizeProvider } = await import("./auth.config")
        expect(normalizeProvider(undefined)).toBe("sqlite")
    })

    it("is case-insensitive and accepts the API's aliases", async () => {
        const { normalizeProvider } = await import("./auth.config")
        expect(normalizeProvider("SQLite")).toBe("sqlite")
        expect(normalizeProvider("Postgres")).toBe("postgres")
        expect(normalizeProvider("PostgreSQL")).toBe("postgres")
        expect(normalizeProvider("npgsql")).toBe("postgres")
        expect(normalizeProvider("SqlServer")).toBe("sqlserver")
        expect(normalizeProvider("mssql")).toBe("sqlserver")
    })

    it("rejects an unsupported value", async () => {
        const { normalizeProvider } = await import("./auth.config")
        expect(() => normalizeProvider("Oracle")).toThrow("Unsupported database provider")
    })
})
