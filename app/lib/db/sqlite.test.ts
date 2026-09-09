import { existsSync, rmSync } from "fs"
import { resolve } from "path"
import { afterEach, beforeEach, describe, expect, it } from "vitest"

const testDbPath = resolve(process.cwd(), ".sqlite-adapter-test.db")

describe("sqlite adapter", () => {
    beforeEach(() => {
        process.env.SQLITE_DATABASE_PATH = testDbPath
    })

    afterEach(() => {
        delete process.env.SQLITE_DATABASE_PATH
        for (const suffix of ["", "-wal", "-shm"]) {
            const f = testDbPath + suffix
            if (existsSync(f)) rmSync(f)
        }
    })

    it("runs a real query, not just a type-checked shape", async () => {
        // Regression guard for the same class of bug auth.config.test.ts already guards against:
        // Better Auth's `database` option is untyped, and a Pool passed where a Kysely instance is
        // expected (or vice versa) compiles fine and fails on the first query with
        // "db.selectFrom is not a function". Importing after setting the env var, not before, so
        // the lazy dialect factory opens the test file rather than the real default.
        const { db } = await import("./sqlite")
        const result = await db
            .selectNoFrom((eb) => eb.lit(1).as("x"))
            .executeTakeFirst()
        expect(result).toEqual({ x: 1 })
    })
})
