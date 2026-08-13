import { NextRequest } from "next/server"
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

// The unit under test is this project's composition, not next-intl's behaviour.
// Returning undefined lets a request fall through to the final action step.
const intlMiddleware = vi.fn<(request: NextRequest) => unknown>(() => undefined)
vi.mock("next-intl/middleware", () => ({ default: () => intlMiddleware }))

const { createProxy } = await import("./create-proxy")
const { evaluatePolicy } = await import("./evaluate-policy")

const config = {
    routing: { locales: ["en", "zh-TW"], defaultLocale: "en" },
    apiPrefix: "/api",
    authRoutes: ["/sign-in"],
    publicRoutes: ["/"],
} as never

function signIn() {
    cookieJar.set(`${AUTH_COOKIE}.session_token`, "a-token")
}

async function run(url: string) {
    return createProxy(config)(new NextRequest(url))
}

describe("createProxy", () => {
    beforeEach(() => {
        cookieJar.clear()
        intlMiddleware.mockClear()
        intlMiddleware.mockReturnValue(undefined)
    })

    describe("API bypass", () => {
        it("passes API requests through without consulting the policy", async () => {
            const res = await run("http://localhost:3000/api/todos")

            expect(res.status).toBe(200)
            expect(res.headers.get("location")).toBeNull()
            expect(intlMiddleware).not.toHaveBeenCalled()
        })

        it("does not bypass a page whose path merely starts with the prefix", async () => {
            // "/apidocs" begins with "/api" without being under it. A prefix match that
            // ignores the boundary hands such a page an unconditional bypass, so policy
            // never runs on it.
            const res = await run("http://localhost:3000/apidocs")

            expect(res.status).toBe(307)
            expect(new URL(res.headers.get("location")!).pathname).toBe("/sign-in")
        })
    })

    describe("redirects", () => {
        it("sends an anonymous visitor to sign-in without a prefix on the default locale", async () => {
            const res = await run("http://localhost:3000/en/dashboard")

            expect(res.status).toBe(307)
            expect(new URL(res.headers.get("location")!).pathname).toBe("/sign-in")
        })

        it("prefixes the redirect with a non-default locale", async () => {
            const res = await run("http://localhost:3000/zh-TW/dashboard")

            expect(new URL(res.headers.get("location")!).pathname).toBe("/zh-TW/sign-in")
        })

        it("redirects before next-intl runs, so the redirect is not rewritten", async () => {
            await run("http://localhost:3000/zh-TW/dashboard")

            expect(intlMiddleware).not.toHaveBeenCalled()
        })

        it("sends a signed-in user away from an auth route", async () => {
            signIn()

            const res = await run("http://localhost:3000/en/sign-in")

            expect(res.status).toBe(307)
            expect(new URL(res.headers.get("location")!).pathname).toBe("/")
        })
    })

    describe("allowed requests", () => {
        it("lets a signed-in user reach a protected route without tagging the response", async () => {
            signIn()

            const res = await run("http://localhost:3000/en/dashboard")

            expect(res.status).toBe(200)
            expect(res.headers.get("location")).toBeNull()
            // The allow branch once attached the resolved path as x-current-path, which
            // nothing read. Asserted alongside the status so this cannot pass by the
            // request having been redirected instead.
            expect(res.headers.get("x-current-path")).toBeNull()
        })

        it("returns next-intl's response when it produces one", async () => {
            signIn()
            const intlResponse = new Response(null, { status: 204 })
            intlMiddleware.mockReturnValue(intlResponse)

            const res = await run("http://localhost:3000/en/dashboard")

            expect(res).toBe(intlResponse)
        })
    })

    describe("deny", () => {
        it("refuses the request instead of falling through to allow", async () => {
            // evaluatePolicy does not return deny today, but the Effect type permits it
            // and the proxy has to fail closed if it ever does.
            const { performAction } = await import("./actions")
            const res = performAction(
                { effect: "deny" },
                {
                    subject: { isAuthenticated: false },
                    resource: { path: "/secret", locale: "en", isPublic: false, isAuthRoute: false },
                    environment: { method: "GET" },
                },
                new NextRequest("http://localhost:3000/en/secret"),
                true,
            )

            expect(res.status).toBe(403)
        })
    })

    describe("policy wiring", () => {
        it("acts on the decision the policy actually returns", async () => {
            // Guards against the composition drifting from the policy: if createProxy
            // stopped calling evaluatePolicy, the redirect assertions above would still
            // pass for the wrong reason.
            const anonymous = evaluatePolicy({
                subject: { isAuthenticated: false },
                resource: { path: "/dashboard", locale: "en", isPublic: false, isAuthRoute: false },
                environment: { method: "GET" },
            })
            expect(anonymous).toEqual({ effect: "redirect", to: "/sign-in" })

            const res = await run("http://localhost:3000/en/dashboard")
            expect(new URL(res.headers.get("location")!).pathname).toBe(anonymous.to)
        })
    })
})
