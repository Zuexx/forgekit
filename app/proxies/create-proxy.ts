import { NextRequest, NextResponse } from "next/server"
import createIntlMiddleware from "next-intl/middleware"

import { type AbacContext, buildRedirectUrl, evaluatePolicy, performAction, type ProxyConfig, resolveContext } from "@/proxies"

export function createProxy(config: ProxyConfig) {
    const intlMiddleware = createIntlMiddleware(config.routing)

    return async function proxy(request: NextRequest) {
        const { nextUrl } = request

        // 1️⃣ API bypass
        // Matched on the segment boundary: a bare startsWith also lets through pages
        // like "/apidocs", which would then never be seen by the policy.
        const { pathname } = nextUrl
        if (
            pathname === config.apiPrefix ||
            pathname.startsWith(`${config.apiPrefix}/`)
        ) {
            return NextResponse.next()
        }

        // 2️⃣ Context
        const context: AbacContext = await resolveContext({
            request,
            config,
        })

        // 3️⃣ Policy
        const decision = evaluatePolicy(context)

        const isDefaultLocale =
            context.resource.locale === config.routing.defaultLocale

        // 4️⃣ Handle redirect decisions before next-intl, so it cannot rewrite the
        // destination out from under us.
        if (decision.effect === "redirect" && decision.to) {
            return NextResponse.redirect(
                buildRedirectUrl(decision.to, context, request, isDefaultLocale)
            )
        }

        // 5️⃣ next-intl locale handling (only if allowed)
        const intlResponse = intlMiddleware(request)
        if (intlResponse) { return intlResponse }

        // 6️⃣ Action
        return performAction(decision, context, request, isDefaultLocale)
    }
}
