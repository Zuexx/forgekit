# Forking Guide

Use this guide when turning ForgeKit into a new project. The goal is to make the fork feel native to the new product instead of carrying starter-kit naming, sample data, or local setup assumptions.

## 1. Rename Project Identity

### Preferred: generate from the `dotnet new` template

The repository root contains `.template.config/`, which registers ForgeKit as a `dotnet new` template. Use it instead of renaming by hand.

```bash
# once per machine, pointing at a local clone of ForgeKit
dotnet new install /path/to/forgekit

# then, from wherever the new project should live
dotnet new forgekit -n Acme.Portal
```

The template handles:

- Solution, project folder, and `.csproj` names → `Acme.Portal.Api`, `Acme.Portal.Api.Tests`, migration projects
- C# root namespaces and `using` directives
- Lowercase identifiers (`forgekit` → `acme.portal`): SQLite file name, `forgekit_db` connection strings, Compose project and container names, `DATABASE_URL`, `forgekit-app` package name, auth cookie prefix
- Fresh solution and project GUIDs on every instantiation
- Excluding `bin/`, `obj/`, `node_modules/`, `.next/`, nested `.git/` directories, `appsettings.Local.json`, `.env.local`, and local SQLite databases

Pass `--slug` when the lowercase form should differ from the lowercased project name — notably for dotted names, where the default produces `acme.portal`:

```bash
dotnet new forgekit -n Acme.Portal --slug acmeportal
```

To refresh the template after pulling ForgeKit changes, re-run `dotnet new install /path/to/forgekit --force`. To remove it, `dotnet new uninstall /path/to/forgekit`.

### Receiving base updates after generation

The API is split into two layers. `api/Anvil/` is the shared layer, with its tests in
`api/Anvil.Tests/`: the template renames neither, so their paths and contents are identical
in the base and in every generated project. `api/<Product>.Api/` is the product layer and
carries the product name.

That split makes shared-layer updates a one-line sync:

```bash
git remote add upstream https://github.com/Zuexx/forgekit.git   # once
git fetch upstream
git checkout upstream/main -- api/Anvil api/Anvil.Tests
```

Review, build, and commit.

Do **not** run a whole-repository `git merge upstream/main`. Git matches files by path, and
it does not know that your `api/<Product>.Api/` is the base's `api/ForgeKit.Api/` under a
new name. It therefore treats the base's product layer as new files and adds them, leaving
you with two copies of the same code side by side. Sync the shared layer by path instead.

#### Check first whether wiring is needed

Before syncing, diff the whole `api/` directory rather than just `api/Anvil/`. What the base
changed tells you whether the sync is self-contained:

```bash
git fetch upstream
git diff HEAD upstream/main -- api/
```

**Only `api/Anvil/` changed — nothing else to do.** Result types, exceptions, handlers,
extension methods, and middleware classes all work as soon as the files arrive. Service
registrations are included: the product's `RegisterApplicationServices` calls the shared
layer's `AddPlatformServices`, so anything the base registers there reaches you without
editing your composition root.

**The base's product layer also changed — you have manual wiring to do.** The most common
case is the middleware pipeline, where ordering is the product's decision and cannot be
injected from the shared layer:

```csharp
app.UseMiddleware<SomeNewMiddleware>();
```

Read what the base did in its own `Program.cs` and apply the equivalent to yours. Git cannot
do this for you, because the two files sit at different paths. It is usually one or two lines.

When adding a shared feature to the base, say in the commit message whether downstream
products need wiring. That turns this check into confirmation rather than investigation.

The frontend has no equivalent split, but it renames only 7 files with no directory
renames, so `git diff upstream/main -- app/` is usually readable enough to apply by hand.

Two notes on template output:

- `.claude/skills/*` are symlinks in this repository; the generated project receives real file copies instead.
- Avoid embedding the project name inside a longer identifier (for example a method named `..ForgeKitEntities()`). The template replaces the token in place, and a dotted project name would produce invalid C#.

### Manual rename

If you cloned instead of generating, rename consistently.

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
- `BETTER_AUTH_ADMIN_USER_IDS`: comma-separated ids granted the admin role, empty by
  default — set it after creating the account that should administer the fork
- `BETTER_AUTH_TRUSTED_ORIGINS`: extra origins allowed to receive auth callbacks, only
  needed when a separate frontend or preview deployment completes sign-in
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
- `openspec/config.yaml` (the `context:` section)
- ADRs if the fork changes architecture decisions

Keep OpenSpec current:

```bash
openspec list
openspec list --specs
openspec validate --all --strict --no-interactive
```

If the fork changes architecture, data model, security posture, or public behavior, create an OpenSpec change before implementing it.

## 6. Review Auth Defaults

The starter kit ships auth settings chosen to be safe for a fork rather than tuned for
any one deployment. Two are worth revisiting before production:

- **Rate limit storage.** Better Auth enables rate limiting in production but stores
  counters in memory by default, which resets on restart and is per-instance. That is
  ineffective on serverless or multi-instance hosting. Switching to
  `rateLimit: { storage: "database" }` needs a `rateLimit` table, which this kit's auth
  schema does not include — add the migration before enabling it.
- **The OpenAPI reference page** for auth endpoints is served outside production only.
  The schema endpoint behind it stays available in every environment — it describes
  Better Auth's own documented endpoints, so it discloses little, but block it at the
  edge if your threat model cares.

The admin plugin has no administrators until `BETTER_AUTH_ADMIN_USER_IDS` names one. This
is intentional: an id shipped in a starter kit would make its holder an administrator of
every project generated from it.

## 7. Verify Security Baseline

Before the first push from a fork:

```bash
gitleaks dir --redact --config .gitleaks.toml .
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

## 8. Run Quality Gates

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
pnpm test
BETTER_AUTH_SECRET="$(openssl rand -base64 32)" BETTER_AUTH_URL="http://localhost:3000" pnpm build

cd ..
openspec validate --all --strict --no-interactive
gitleaks git --redact --config .gitleaks.toml --log-opts=--all
```

`scripts/verify.sh` runs all of the above in one command.

End-to-end tests are not in that list because they need infrastructure. Run them
separately once a database is available:

```bash
podman compose up -d                      # or docker
cd app
export DATABASE_URL=postgresql://localhost:5432/<your-db> PGUSER=postgres
pnpm auth.migration                       # creates the auth schema
pnpm build && pnpm test:e2e
```

CI runs everything above, end-to-end tests included, on every pull request.

## 9. First Commit After Fork

Make the first product commit about identity only:

```bash
git checkout -b chore/rename-project
```

Keep behavior changes out of that commit. A clean rename commit makes future diffs and blame much easier to review.
