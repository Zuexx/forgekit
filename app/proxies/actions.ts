import { NextRequest, NextResponse } from "next/server";

import type { AbacContext, PolicyDecision } from "@/proxies";

/**
 * Builds the URL a redirect decision points at, prefixing the locale only when it is
 * not the default one, to match the "as-needed" locale prefix strategy.
 */
export function buildRedirectUrl(
    to: string,
    ctx: AbacContext,
    request: NextRequest,
    isDefaultLocale: boolean
): URL {
    const localePath = isDefaultLocale ? to : `/${ctx.resource.locale}${to}`
    return new URL(localePath, request.url)
}

export function performAction(
    decision: PolicyDecision,
    ctx: AbacContext,
    request: NextRequest,
    isDefaultLocale: boolean
): NextResponse {
    if (decision.effect === "allow") {
        return NextResponse.next();
    }

    if (decision.effect === "redirect" && decision.to) {
        return NextResponse.redirect(
            buildRedirectUrl(decision.to, ctx, request, isDefaultLocale)
        );
    }

    // Anything else — a deny, or a redirect with no destination — fails closed.
    // Falling through to NextResponse.next() here would turn a deny into an allow.
    return new NextResponse(null, { status: 403 });
}
