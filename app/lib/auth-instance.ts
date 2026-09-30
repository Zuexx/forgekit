// No "server-only" guard here on purpose, and this is the file the better-auth CLI's
// --config flag points to (see package.json's auth.generate/auth.migration scripts and
// .github/workflows/ci.yml's "Create auth schema" step) -- the CLI loads this file with its
// own module loader, outside Next's webpack build, and cannot resolve a "server-only" import
// at all ("Please remove import 'server-only' from your auth config file temporarily" is the
// CLI's own error). lib/auth.config.ts (the guarded wrapper every app import should use
// instead) re-exports everything here behind the real guard. Never import this file directly
// from app code -- import "@/lib/auth.config" instead.
import { config } from "dotenv"
import { resolve } from "path"

// `sqlite-instance.ts` below also loads this file, and ES module evaluation order means its
// call runs before this one either way. This call exists so the guarantee does not quietly
// depend on that: `normalizeProvider` below reads `Database__Provider` on the assumption it is
// already loaded, and that has to hold even if an adapter stops loading it — dotenv's `config`
// does not override an already-set variable, so calling it again here is a safe no-op today.
config({ path: resolve(process.cwd(), "..", ".env") })

import { betterAuth } from "better-auth"
import { nextCookies } from "better-auth/next-js"
import { admin } from "better-auth/plugins"
import { customSession, jwt, openAPI } from "better-auth/plugins"

import { AUTH_COOKIE } from "@/constants/cookies"
import { db as mssqlDb } from "@/lib/db/mssql-instance"
import { db as postgresDb } from "@/lib/db/postgres-instance"
import { db as sqliteDb } from "@/lib/db/sqlite-instance"

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

/**
 * Normalises `Database__Provider` the same way the API's own
 * `DatabaseProviderExtensions.NormalizeProvider` does, so a value that resolves on one side
 * resolves identically on the other. Defaults to `"sqlite"`, matching the API's
 * `DefaultProvider`.
 */
export function normalizeProvider(
  value: string | undefined,
): "sqlite" | "postgres" | "sqlserver" {
  const normalized = (value ?? "").trim().toLowerCase()
  switch (normalized) {
    case "":
    case "sqlite":
      return "sqlite"
    case "postgres":
    case "postgresql":
    case "npgsql":
      return "postgres"
    case "sqlserver":
    case "sql-server":
    case "mssql":
      return "sqlserver"
    default:
      throw new Error(
        `Unsupported database provider '${value}'. Supported providers: sqlite, postgres, sqlserver.`,
      )
  }
}

/**
 * The three adapters this kit ships need different shapes, and getting it wrong fails at
 * runtime rather than at compile time — Better Auth does not type-check this option.
 *
 * - `postgres-instance.ts` exports a `pg.Pool`. Pass it directly; Better Auth builds the
 *   Kysely instance and detects the dialect itself.
 * - `mssql-instance.ts` and `sqlite-instance.ts` export a Kysely instance. Those need the
 *   wrapper: `{ db: kyselyInstance, type: "mssql" | "sqlite" as const }`.
 *
 * Wrapping the Pool instead hands the adapter a Pool where it expects a Kysely, and every
 * query throws "db.selectFrom is not a function".
 */
const selectedProvider = normalizeProvider(process.env.Database__Provider)

export const database =
  selectedProvider === "postgres"
    ? postgresDb
    : selectedProvider === "sqlserver"
      ? { db: mssqlDb, type: "mssql" as const }
      : { db: sqliteDb, type: "sqlite" as const }

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
    // Must stay last. It forwards Set-Cookie into Next's cookie store, so any plugin
    // whose after-hook sets a cookie has to run before it or that cookie is dropped.
    nextCookies(),
  ]
})
