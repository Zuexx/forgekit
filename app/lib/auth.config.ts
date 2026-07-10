import { betterAuth } from "better-auth"
import { nextCookies } from "better-auth/next-js"
import { admin } from "better-auth/plugins"
import { customSession, jwt, openAPI } from "better-auth/plugins"

import { AUTH_COOKIE } from "@/constants/cookies"
import { db as postgresDb } from "@/lib/db/postgres"

let database: object = { db: postgresDb, type: 'postgresql' }

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

export const auth = betterAuth({
  database,
  baseURL: process.env.BETTER_AUTH_URL,
  secret: process.env.BETTER_AUTH_SECRET,
  emailAndPassword: {
    enabled: true
  },
  socialProviders: microsoftProvider,
  advanced: {
    cookiePrefix: AUTH_COOKIE,
    defaultCookieAttributes: {
      sameSite: "lax",
      secure: process.env.NODE_ENV === "production",
      httpOnly: true,
    }
  },
  plugins: [
    admin({
      adminUserIds: ["inFpaorByjQwfFMx7z2xqU0Daumz8fQV"]
    }),
    nextCookies(),
    openAPI(),
    jwt({
      jwt: {
        // definePayload: (user) => {
        //   return {
        //     id: user.id,
        //     email: user.email,
        //     role: user.role
        //   }
        // }
      },
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
