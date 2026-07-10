# ForgeKit App

Next.js 16 frontend for the ForgeKit monorepo. The app uses the App Router, TypeScript, Tailwind CSS, shadcn/ui primitives, Better Auth, Hono RPC routes, TanStack Query, and next-intl.

## Prerequisites

- Node.js 24
- pnpm 11
- PostgreSQL access for Better Auth
- API running locally when exercising backend flows

The root `compose.yaml` provides a local PostgreSQL service for development:

```bash
docker compose up -d postgres
```

## Setup

Install dependencies:

```bash
pnpm install
```

Create `.env.local` with the values needed by your local auth and database setup:

```bash
DATABASE_URL=postgresql://localhost:5432/forgekit
BETTER_AUTH_URL=http://localhost:3000
BETTER_AUTH_SECRET=
AZURE_AD_CLIENT_ID=
AZURE_AD_TENANT_ID=
AZURE_AD_CLIENT_SECRET=
```

Generate a local secret with `openssl rand -base64 32`. Do not commit `.env.local`.

Start the development server:

```bash
pnpm dev
```

The app runs at `http://localhost:3000`.

## Scripts

```bash
pnpm dev          # Start Next.js dev server
pnpm build        # Build production app
pnpm start        # Start production server
pnpm check        # TypeScript check
pnpm lint         # ESLint
pnpm lint:fix     # ESLint autofix
pnpm auth.generate
pnpm auth.migration
pnpm auth.codegen
```

## Structure

- `app/` contains App Router pages and API route handlers.
- `features/authenticate/` contains auth UI, schemas, hooks, and Hono route definitions.
- `lib/auth.config.ts` configures Better Auth with PostgreSQL.
- `lib/db/postgres.ts` provides the PostgreSQL pool used by Better Auth.
- `lib/db/mssql.ts` is an optional Kysely MSSQL helper and is not the default app database.
- `lib/rpc/` contains the Hono typed client and session middleware.
- `providers/` contains app-level React providers.
- `messages/` contains `en`, `ko-KR`, and `zh-TW` translations.

## Local API

The backend API runs from `../api`:

```bash
cd ../api
dotnet run --project ForgeKit.Api
```

Default launch URLs are `http://localhost:5000` and `https://localhost:7288`.
