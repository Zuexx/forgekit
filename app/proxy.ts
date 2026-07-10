import { NextRequest } from "next/server"

import { apiPrefix, authRoutes, publicRoutes } from "@/constants"
import { routingConfig } from "@/i18n"
import { createProxy } from "@/proxies"

export const proxyHandler = createProxy({
    routing: routingConfig,
    apiPrefix,
    authRoutes,
    publicRoutes,
})

export default function proxy(request: NextRequest) {
    return proxyHandler(request)
}

export const config = {
    matcher: [
        "/((?!.+\\.[\\w]+$|_next).*)",
        "/",
        "/(api|trpc)(.*)",
    ],
}