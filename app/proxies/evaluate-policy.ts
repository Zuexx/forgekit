import type { AbacContext, PolicyDecision } from "./types";

export function evaluatePolicy(ctx: AbacContext): PolicyDecision {
    const { subject, resource } = ctx;

    // Auth routes
    if (resource.isAuthRoute) {
        if (subject.isAuthenticated) {
            return { effect: "redirect", to: "/" };
        }
        return { effect: "allow" };
    }

    // Protected routes
    if (!subject.isAuthenticated && !resource.isPublic) {
        return { effect: "redirect", to: "/sign-in" };
    }

    return { effect: "allow" };
}
