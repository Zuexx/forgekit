import { NextRequest, NextResponse } from "next/server"
import createIntlMiddleware from "next-intl/middleware"

import { type AbacContext, evaluatePolicy, performAction, type ProxyConfig,resolveContext } from "@/proxies"

export function createProxy(config: ProxyConfig) {
    const intlMiddleware = createIntlMiddleware(config.routing)

    return async function proxy(request: NextRequest) {
        const { nextUrl } = request

        // 1️⃣ API bypass
        if (nextUrl.pathname.startsWith(config.apiPrefix)) {
            return NextResponse.next()
        }

        // 2️⃣ Context
        const context: AbacContext = await resolveContext({
            request,
            config,
        })

        // console.log("Context:", JSON.stringify(context, null, 2))

        // 3️⃣ Policy
        const decision = evaluatePolicy(context)

        // 4️⃣ Handle redirect decisions first (before intl)
        if (decision.effect === "redirect" && decision.to) {
            // console.log("Redirect URL parts:", {
            //     locale: context.resource.locale,
            //     to: decision.to,
            //     requestUrl: request.url,
            //     isDefaultLocale: context.resource.locale === config.routing.defaultLocale
            // })

            // Only add locale prefix if it's not the default locale (as-needed)
            const localePath = context.resource.locale === config.routing.defaultLocale
                ? decision.to
                : `/${context.resource.locale}${decision.to}`

            const url = new URL(localePath, request.url)
            // console.log("Final redirect URL:", url.toString()
            return NextResponse.redirect(url)
        }

        // 5️⃣ next-intl locale handling (only if allowed)
        const intlResponse = intlMiddleware(request)
        if (intlResponse) { return intlResponse }

        // 6️⃣ Action
        const isDefaultLocale = context.resource.locale === config.routing.defaultLocale
        return performAction(decision, context, request, isDefaultLocale)
    }
}
