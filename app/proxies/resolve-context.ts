import { cookies } from "next/headers"
import type { NextRequest } from "next/server"

import { AUTH_COOKIE } from "@/constants/cookies"
import type { AbacContext, ProxyConfig } from "@/proxies"

interface Args {
    request: NextRequest
    config: ProxyConfig
}

export async function resolveContext({
    request,
    config,
}: Args): Promise<AbacContext> {
    // Extract locale from pathname
    const pathname = request.nextUrl.pathname
    const segments = pathname.split('/').filter(Boolean)
    
    // Check if first segment is a valid locale
    const firstSegment = segments[0]
    const isLocaleInPath = config.routing.locales.includes(firstSegment as any)
    
    const locale = isLocaleInPath 
        ? firstSegment 
        : config.routing.defaultLocale

    // Remove locale from path
    let path = pathname
    if (isLocaleInPath && path.startsWith(`/${locale}`)) {
        path = path.replace(`/${locale}`, "") || "/"
    }

    // Derived from AUTH_COOKIE rather than written out, because that constant is what
    // Better Auth uses as its cookiePrefix. Hardcoding the name here means renaming the
    // app silently stops session detection: every signed-in user reads as anonymous and
    // gets bounced to /sign-in.
    const cookieStore = await cookies()
    const session =
        cookieStore.get(`${AUTH_COOKIE}.session_token`) ??
        cookieStore.get(`__Secure-${AUTH_COOKIE}.session_token`)

    return {
        subject: {
            isAuthenticated: Boolean(session?.value),
        },
        resource: {
            path,
            locale,
            isPublic: config.publicRoutes.includes(path),
            isAuthRoute: config.authRoutes.includes(path),
        },
        environment: {
            method: request.method,
        },
    }
}
