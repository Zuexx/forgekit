# Forking Guide

Use this guide when turning ForgeKit into a new project. The goal is to make the fork feel native to the new product instead of carrying starter-kit naming, sample data, or local setup assumptions.

## 1. Rename Project Identity

Choose the product name first, then rename consistently.

Update these items together:

- Repository name and GitHub description
- Root `README.md` title and overview
- .NET solution name: `api/ForgeKit.sln`
- .NET project folders and `.csproj` names under `api/`
- C# root namespace: `ForgeKit.Api`
- Frontend package name in `app/package.json`
- Database names in examples and local configuration
- Serilog application name in API configuration

Recommended search before and after renaming:

```bash
rg -n "ForgeKit|forgekit|ForgeKitDb|forgekit_db|forgekit-app" .
```

If you keep the ForgeKit name intentionally, document that decision in the project README so contributors know it is not stale scaffold residue.

## 2. Reset Local Configuration

Never commit local runtime secrets. Start from examples:

```bash
cp api/ForgeKit.Api/appsettings.Local.json.example api/ForgeKit.Api/appsettings.Local.json
cp app/.env.local.example app/.env.local
```

Then fill only local values:

- `BETTER_AUTH_SECRET`: generate with `openssl rand -base64 32`
- `DATABASE_URL`: required by the frontend Better Auth PostgreSQL adapter
- `Database:Provider`: API provider, defaults to `Sqlite`
- `ConnectionStrings:<Provider>`: API connection string for the selected provider
- OAuth client secrets: only if the fork enables social login

The starter kit intentionally commits no real credentials and no fixed shared app secret.

## 3. Choose Database Posture

ForgeKit supports three API migration tracks:

- SQLite: default for local API development
- PostgreSQL: optional API provider
- SQL Server: optional API provider

The frontend currently uses Better Auth with PostgreSQL, so auth flows need PostgreSQL even if the API stays on SQLite.

For a new fork, decide early:

- Keep all three API providers if the fork is a reusable platform base.
- Keep only one provider if the fork is a product with known infrastructure.
- If removing providers, delete the unused migration projects and update `docs/api/CONFIGURATION_GUIDE.md`.

When the EF model changes and multiple providers remain supported, add migrations for every provider project before merging.

## 4. Decide What To Do With Samples

ForgeKit contains sample-oriented code to demonstrate conventions:

- `ForgeKit.Api/Samples/`
- `ForgeKit.Api/Modules/SampleResourceModule.cs`
- `ForgeKit.Api/Entities/Todos/`
- `ForgeKit.Api/Services/Todos/`
- matching tests under `ForgeKit.Api.Tests/`

For a fork base, keeping these is useful. For a product fork, either:

- keep them under a clearly named `Samples` area, or
- delete them after the product has its first real feature using the same conventions.

Do not leave sample endpoints mixed with production modules without naming them as samples.

## 5. Refresh Documentation

At minimum, update:

- `README.md`
- `docs/STRUCTURE.md`
- `api/README.md`
- `app/README.md`
- `openspec/project.md`
- ADRs if the fork changes architecture decisions

Keep OpenSpec current:

```bash
openspec list
openspec list --specs
openspec validate --all --strict --no-interactive
```

If the fork changes architecture, data model, security posture, or public behavior, create an OpenSpec change before implementing it.

## 6. Verify Security Baseline

Before the first push from a fork:

```bash
gitleaks dir --redact .
git status --short --ignored
```

Check that these remain untracked:

- `.env.local`
- `appsettings.Local.json`
- SQLite databases
- build output
- local IDE settings
- generated logs

If a secret is ever pushed, rotate it first, then rewrite history and force-push only after the replacement secret is no longer valid.

## 7. Run Quality Gates

Run these before treating the fork as ready:

```bash
cd api
dotnet restore
dotnet build
dotnet test
dotnet tool restore

cd ../app
pnpm install
pnpm check
pnpm lint
BETTER_AUTH_SECRET="$(openssl rand -base64 32)" BETTER_AUTH_URL="http://localhost:3000" pnpm build

cd ..
openspec validate --all --strict --no-interactive
gitleaks git --redact --log-opts=--all
```

The GitHub Actions workflow runs the same categories of checks on `main`.

## 8. First Commit After Fork

Make the first product commit about identity only:

```bash
git checkout -b chore/rename-project
```

Keep behavior changes out of that commit. A clean rename commit makes future diffs and blame much easier to review.
