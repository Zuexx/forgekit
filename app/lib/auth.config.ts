import { betterAuth } from "better-auth"
import { nextCookies } from "better-auth/next-js"
import { admin } from "better-auth/plugins"
import { customSession, jwt, openAPI } from "better-auth/plugins"

import { AUTH_COOKIE } from "@/constants/cookies"
import { db as postgresDb } from "@/lib/db/postgres"

/**
 * Reads a comma-separated environment variable into a list.
 *
 * Used for settings that are per-deployment and must not be baked into the starter
 * kit — admin user ids and trusted origins are both values a fork has to supply.
 */
export function parseEnvList(value: string | undefined): string[] {
  return (value ?? "")
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean)
}

const database = { db: postgresDb, type: "postgresql" as const }

const microsoftClientId = process.env.AZURE_AD_CLIENT_ID
const microsoftTenantId = process.env.AZURE_AD_TENANT_ID
const microsoftClientSecret = process.env.AZURE_AD_CLIENT_SECRET
const microsoftProvider =
  microsoftClientId && microsoftTenantId && microsoftClientSecret
    ? {
        microsoft: {
          enabled: true,
          clientId: microsoftClientId,
          tenantId: microsoftTenantId,
          clientSecret: microsoftClientSecret,
          scope: ["User.Read"],
        },
      }
    : {}

const isProduction = process.env.NODE_ENV === "production"

/**
 * Admin user ids come from the environment and default to none.
 *
 * A starter kit cannot ship a real id here: every fork would inherit it, and whoever
 * held that account in a fork's database would be an administrator of it.
 */
const adminUserIds = parseEnvList(process.env.BETTER_AUTH_ADMIN_USER_IDS)

/** Extra origins allowed to receive auth callbacks and redirects. baseURL is always trusted. */
const trustedOrigins = parseEnvList(process.env.BETTER_AUTH_TRUSTED_ORIGINS)

export const auth = betterAuth({
  database,
  baseURL: process.env.BETTER_AUTH_URL,
  secret: process.env.BETTER_AUTH_SECRET,
  trustedOrigins,
  emailAndPassword: {
    enabled: true
  },
  socialProviders: microsoftProvider,
  advanced: {
    cookiePrefix: AUTH_COOKIE,
    defaultCookieAttributes: {
      sameSite: "lax",
      secure: isProduction,
      httpOnly: true,
    }
  },
  plugins: [
    admin({
      adminUserIds
    }),
    nextCookies(),
    // The interactive reference page is served outside production only. Dropping the
    // plugin entirely in production would be stronger, but the plugins array has to stay
    // a fixed tuple: Better Auth infers the session user type from it, and a conditional
    // spread widens the type until the admin plugin's fields (role, banned) are lost.
    // The generate-schema endpoint remains either way; it describes Better Auth's own
    // documented endpoints, so it discloses little.
    openAPI({ disableDefaultReference: isProduction }),
    jwt({
      jwks: {
        disablePrivateKeyEncryption: false,
        keyPairConfig: {
          alg: "RS256"
        }
      }
    }),
    customSession(async ({ user, session }) => {
      return {
        user: {
          ...user,
        },
        session
      };
    }),
  ]
})
