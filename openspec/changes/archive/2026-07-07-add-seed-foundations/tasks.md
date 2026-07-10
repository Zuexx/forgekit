## 1. Implementation
- [x] Implement SeedFoundations (extended PocDataSeeder) to insert at least 5 records for each entity in AppDbContext.
- [x] Make the seeder idempotent: use existence checks (regions count guard) to avoid duplication.
- [x] Register seeder in DI and invoke via ModuleExtensions.StartSeed when enabled.
- [x] Add configuration option (AppSettingKeys.SeedEnabled) to guard seeding in non-dev environments.
- [x] Ensure relationships are properly wired (foreign keys reference seeded parent records).
- [x] Scaffolded better-auth tables into Api\Entities\Auth and Api\Data\Auth\BetterAuthDbContext (database-first).
- [x] Confirm scaffolded entities are read-only (do not include them in system migrations by default).

## 2. Validation
- [x] Run seeder against a fresh local database and verify every DbSet contains >= 5 records. -- Verified against local_scaffold_seed (each target table had 5 records during validation).
- [x] Run seeder twice and verify no duplicates are created and operation is idempotent. -- Verified by re-running during testing; logic guards on regions count.
- [x] Run application with seeding disabled and verify no sample data is created. -- Verified by running without SeedEnabled and in non-Development environment.
- [x] Validate scaffolding of better-auth tables produces accurate EF entities and does not overwrite existing data.

## 3. Documentation
- [x] Update README or openspec/project.md with instructions to enable seeding for local development and CI.
- [x] Document how to reset seeded data and how to disable seeding in production. See sql/rollback.sql for rollback script and instructions.
- [x] Document scaffold command and generated file locations (docs/scaffold-better-auth.md).

## Progress
- Current completion: ~90% (implementation, gating, validation, rollback, docs, and scaffolding done; drift-detection + migration generation pending)
- Last update: 2026-02-03T05:12:56.484Z
