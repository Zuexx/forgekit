import { describe, expect, it } from "vitest"

import { createSignUpSchema, signUpSchema } from "./sign-up-schema"

const t = ((key: string) => key) as never

const valid = {
    name: "A Person",
    email: "person@example.com",
    password: "abcd1234",
    confirmPassword: "abcd1234",
}

/**
 * The client factory once extended the already-refined server schema and added an
 * inverted check. Refinements stack rather than replace, so the two conditions could
 * never both hold and the form rejected every input — including correct ones. The pairs
 * below are asserted against both schemas so the two cannot drift apart again.
 */
describe.each([
    ["server", signUpSchema],
    ["client", createSignUpSchema(t)],
])("%s sign-up schema", (_name, schema) => {
    it("accepts a valid registration", () => {
        expect(schema.safeParse(valid).success).toBe(true)
    })

    it("rejects mismatched passwords", () => {
        const result = schema.safeParse({ ...valid, confirmPassword: "different1" })

        expect(result.success).toBe(false)
        expect(result.error?.issues.some((i) => i.path[0] === "confirmPassword")).toBe(true)
    })

    it("rejects a password under eight characters", () => {
        const short = "abc123"
        const result = schema.safeParse({ ...valid, password: short, confirmPassword: short })

        expect(result.success).toBe(false)
    })

    it("rejects a malformed email", () => {
        expect(schema.safeParse({ ...valid, email: "not-an-email" }).success).toBe(false)
    })

    it("rejects an empty name", () => {
        expect(schema.safeParse({ ...valid, name: "" }).success).toBe(false)
    })
})
