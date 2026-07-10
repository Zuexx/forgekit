import { NextRequest, NextResponse } from "next/server";

import type { AbacContext, PolicyDecision } from "@/proxies";

export function performAction(
    decision: PolicyDecision,
    ctx: AbacContext,
    request: NextRequest,
    isDefaultLocale: boolean
): NextResponse {
    if (decision.effect === "allow") {
        const res = NextResponse.next();
        res.headers.set("x-current-path", ctx.resource.path);
        return res;
    }

    if (decision.effect === "redirect" && decision.to) {
        // Only add locale prefix if it's not the default locale (as-needed)
        const localePath = isDefaultLocale
            ? decision.to
            : `/${ctx.resource.locale}${decision.to}`
        
        const url = new URL(localePath, request.url);
        return NextResponse.redirect(url);
    }

    return NextResponse.next();
}
