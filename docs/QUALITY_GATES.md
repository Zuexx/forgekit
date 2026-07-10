# Quality Gates

Run the same categories of checks locally before pushing a starter-kit change or before treating a fork as ready.

## One Command

```bash
./scripts/verify.sh
```

The script runs:

- API restore, Release build, and tests
- App install, TypeScript check, ESLint, and production build
- strict OpenSpec validation
- Gitleaks working-tree and full-history scans

## Required Tools

- .NET 10 SDK
- Node.js 24
- pnpm 11
- OpenSpec CLI
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
BETTER_AUTH_SECRET="$(openssl rand -base64 32)" BETTER_AUTH_URL="http://localhost:3000" pnpm build
```

```bash
openspec validate --all --strict --no-interactive
gitleaks dir --redact .
gitleaks git --redact --log-opts=--all
```

## CI

GitHub Actions runs API, App, OpenSpec, and full-history secret scanning jobs on `main` and pull requests.
