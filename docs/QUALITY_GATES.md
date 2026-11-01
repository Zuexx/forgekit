# Quality Gates

Run the same categories of checks locally before pushing a starter-kit change or before treating a fork as ready.

## One Command

```bash
pnpm verify
```

The script runs:

- API restore, Release build, and tests
- App install, TypeScript check, ESLint, unit tests, and production build
- Gitleaks working-tree and full-history scans

## Required Tools

- .NET 10 SDK
- Node.js 24
- pnpm 11
- Gitleaks

## Manual Commands

If you need to run checks independently:

```bash
cd api
dotnet restore ForgeKit.sln
dotnet build ForgeKit.sln --configuration Release --no-restore
dotnet test ForgeKit.sln --configuration Release --no-build
dotnet tool restore
```

```bash
cd app
pnpm install --frozen-lockfile
pnpm check
pnpm lint
pnpm test
BETTER_AUTH_SECRET="$(openssl rand -base64 32)" BETTER_AUTH_URL="http://localhost:3000" pnpm build
```

```bash
gitleaks dir --redact .
gitleaks git --redact --log-opts=--all
```

## CI

GitHub Actions runs the API, App, and Secret scan jobs on `main` and pull requests; the App job
also runs the Playwright end-to-end tests. All three are required checks in the branch ruleset
(`scripts/protect-branch.sh`).
