import { describe, expect, it } from "vitest"

import { evaluatePolicy } from "./evaluate-policy"
import type { AbacContext } from "./types"

function context(overrides: {
    isAuthenticated?: boolean
    isPublic?: boolean
    isAuthRoute?: boolean
    path?: string
}): AbacContext {
    return {
        subject: { isAuthenticated: overrides.isAuthenticated ?? false },
        resource: {
            path: overrides.path ?? "/dashboard",
            locale: "en",
            isPublic: overrides.isPublic ?? false,
            isAuthRoute: overrides.isAuthRoute ?? false,
        },
        environment: { method: "GET" },
    }
}

describe("evaluatePolicy", () => {
    it("sends an anonymous visitor on a protected route to sign-in", () => {
        expect(evaluatePolicy(context({}))).toEqual({
            effect: "redirect",
            to: "/sign-in",
        })
    })

    it("lets an authenticated user through to a protected route", () => {
        expect(evaluatePolicy(context({ isAuthenticated: true }))).toEqual({
            effect: "allow",
        })
    })

    it("lets an anonymous visitor reach a public route", () => {
        expect(evaluatePolicy(context({ isPublic: true }))).toEqual({
            effect: "allow",
        })
    })

    it("lets an anonymous visitor reach an auth route", () => {
        expect(evaluatePolicy(context({ isAuthRoute: true }))).toEqual({
            effect: "allow",
        })
    })

    it("sends an already-authenticated user away from an auth route", () => {
        expect(
            evaluatePolicy(context({ isAuthenticated: true, isAuthRoute: true })),
        ).toEqual({ effect: "redirect", to: "/" })
    })

    it("treats the auth-route branch as taking precedence over public", () => {
        // An auth route marked public must still bounce a signed-in user, otherwise
        // /sign-in stays reachable while signed in.
        expect(
            evaluatePolicy(
                context({ isAuthenticated: true, isAuthRoute: true, isPublic: true }),
            ),
        ).toEqual({ effect: "redirect", to: "/" })
    })

    it("never denies outright, so the caller only has to handle allow and redirect", () => {
        const combinations = [true, false].flatMap((isAuthenticated) =>
            [true, false].flatMap((isPublic) =>
                [true, false].map((isAuthRoute) =>
                    context({ isAuthenticated, isPublic, isAuthRoute }),
                ),
            ),
        )

        for (const ctx of combinations) {
            expect(evaluatePolicy(ctx).effect).not.toBe("deny")
        }
    })
})
