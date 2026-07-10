## 1. Migration Project Structure
- [x] 1.1 Create SQLite, PostgreSQL, and SQL Server migration projects
- [x] 1.2 Add all migration projects to `ForgeKit.sln`
- [x] 1.3 Add provider-specific design-time factories for both DbContexts

## 2. Migration Histories
- [x] 2.1 Move SQLite migrations without changing migration identifiers
- [x] 2.2 Generate initial PostgreSQL migrations for both DbContexts
- [x] 2.3 Generate initial SQL Server migrations for both DbContexts
- [x] 2.4 Remove the SQLite-only factories from `ForgeKit.Api`

## 3. Verification
- [x] 3.1 Add focused tests for migration project/provider mapping
- [x] 3.2 Verify migration discovery for all six provider/context combinations
- [x] 3.3 Apply both SQLite migration chains to a fresh local database
- [x] 3.4 Run API build and tests

## 4. Documentation And CI
- [x] 4.1 Update quickstart and configuration guides with explicit migration projects
- [x] 4.2 Add migration discovery checks to CI
- [x] 4.3 Run frontend checks and production build
- [x] 4.4 Run strict OpenSpec and Gitleaks validation
