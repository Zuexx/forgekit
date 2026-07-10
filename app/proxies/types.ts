import createIntlMiddleware from "next-intl/middleware"

type Effect = "allow" | "redirect" | "deny"

interface PolicyDecision {
    effect: Effect
    to?: string
}

interface Subject {
    isAuthenticated: boolean
    roles?: string[]
    userId?: string
}

interface Resource {
    path: string
    locale: string
    isPublic: boolean
    isAuthRoute: boolean
}

interface Environment {
    method: string
}

interface AbacContext {
    subject: Subject
    resource: Resource
    environment: Environment
}

interface ProxyConfig {
    routing: Parameters<typeof createIntlMiddleware>[0],
    apiPrefix: string
    authRoutes: string[]
    publicRoutes: string[]
}

export type { AbacContext, Effect, Environment, PolicyDecision, ProxyConfig,Resource, Subject }