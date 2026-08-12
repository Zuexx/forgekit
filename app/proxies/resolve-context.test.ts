import type { NextRequest } from "next/server"
import { beforeEach, describe, expect, it, vi } from "vitest"

import { AUTH_COOKIE } from "@/constants/cookies"

const cookieJar = new Map<string, string>()

vi.mock("next/headers", () => ({
    cookies: async () => ({
        get: (name: string) => {
            const value = cookieJar.get(name)
            return value === undefined ? undefined : { name, value }
        },
    }),
}))

const { resolveContext } = await import("./resolve-context")

const config = {
    routing: { locales: ["en", "zh-TW", "ko-KR"], defaultLocale: "en" },
    apiPrefix: "/api",
    authRoutes: ["/sign-in", "/sign-up"],
    publicRoutes: ["/", "/about"],
} as unknown as Parameters<typeof resolveContext>[0]["config"]

function request(pathname: string, method = "GET") {
    return {
        nextUrl: { pathname },
        method,
    } as unknown as NextRequest
}

describe("resolveContext", () => {
    beforeEach(() => cookieJar.clear())

    describe("locale handling", () => {
        it("takes the locale from the first path segment", async () => {
            const ctx = await resolveContext({ request: request("/zh-TW/about"), config })

            expect(ctx.resource.locale).toBe("zh-TW")
            expect(ctx.resource.path).toBe("/about")
        })

        it("falls back to the default locale when the path has none", async () => {
            const ctx = await resolveContext({ request: request("/about"), config })

            expect(ctx.resource.locale).toBe("en")
            expect(ctx.resource.path).toBe("/about")
        })

        it("maps a bare locale path to the root", async () => {
            const ctx = await resolveContext({ request: request("/en"), config })

            expect(ctx.resource.path).toBe("/")
        })

        it("leaves a path alone when its first segment only looks like a locale", async () => {
            const ctx = await resolveContext({ request: request("/english/page"), config })

            expect(ctx.resource.locale).toBe("en")
            expect(ctx.resource.path).toBe("/english/page")
        })

        it("strips only the leading locale, not a later match", async () => {
            const ctx = await resolveContext({ request: request("/en/enroll"), config })

            expect(ctx.resource.path).toBe("/enroll")
        })
    })

    describe("session detection", () => {
        it("reads the session cookie named after AUTH_COOKIE", async () => {
            cookieJar.set(`${AUTH_COOKIE}.session_token`, "a-token")

            const ctx = await resolveContext({ request: request("/dashboard"), config })

            expect(ctx.subject.isAuthenticated).toBe(true)
        })

        it("reads the __Secure- prefixed cookie set over HTTPS", async () => {
            cookieJar.set(`__Secure-${AUTH_COOKIE}.session_token`, "a-token")

            const ctx = await resolveContext({ request: request("/dashboard"), config })

            expect(ctx.subject.isAuthenticated).toBe(true)
        })

        it("treats a missing cookie as anonymous", async () => {
            const ctx = await resolveContext({ request: request("/dashboard"), config })

            expect(ctx.subject.isAuthenticated).toBe(false)
        })

        it("treats an empty cookie value as anonymous", async () => {
            cookieJar.set(`${AUTH_COOKIE}.session_token`, "")

            const ctx = await resolveContext({ request: request("/dashboard"), config })

            expect(ctx.subject.isAuthenticated).toBe(false)
        })
    })

    describe("route classification", () => {
        it("marks a configured public route", async () => {
            const ctx = await resolveContext({ request: request("/en/about"), config })

            expect(ctx.resource.isPublic).toBe(true)
            expect(ctx.resource.isAuthRoute).toBe(false)
        })

        it("marks a configured auth route", async () => {
            const ctx = await resolveContext({ request: request("/en/sign-in"), config })

            expect(ctx.resource.isAuthRoute).toBe(true)
        })

        it("classifies against the locale-stripped path", async () => {
            // Route lists hold "/about", so classification has to run after the locale
            // prefix is removed or every localised URL falls through as protected.
            const ctx = await resolveContext({ request: request("/ko-KR/about"), config })

            expect(ctx.resource.isPublic).toBe(true)
        })

        it("carries the request method through", async () => {
            const ctx = await resolveContext({ request: request("/", "POST"), config })

            expect(ctx.environment.method).toBe("POST")
        })
    })
})
