## Better Auth — EF Core Entities

Date: 2026-02-03

### Summary

The five tables managed by the front-end Better Auth system are defined as code-first EF Core entities under `Api/Entities/Auth/` with a dedicated `BetterAuthDbContext` at `Api/Data/Auth/BetterAuthDbContext.cs`.

### Entity files

- `Api/Entities/Auth/Account.cs`
- `Api/Entities/Auth/User.cs`
- `Api/Entities/Auth/Session.cs`
- `Api/Entities/Auth/Jwk.cs`
- `Api/Entities/Auth/Verification.cs`
- `Api/Data/Auth/BetterAuthDbContext.cs`

### Applying migrations

```bash
dotnet ef database update \
  --context BetterAuthDbContext \
  --project Api/Api.csproj \
  --startup-project Api/Api.csproj
```

### Notes

- These entities mirror the Better Auth schema. If Better Auth adds plugins or schema changes, update the entities and generate a new migration.
- Use `BetterAuthDbContext` only for auth-related tables; application domain tables live in `AppDbContext`.
- Generate application migrations normally: `dotnet ef migrations add <Name> --context AppDbContext`.
