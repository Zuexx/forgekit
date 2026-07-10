# Change: add-seed-foundations

## Why
A comprehensive seeding foundation is required to bootstrap a realistic dataset for development, QA and CI smoke tests. Current lightweight POC seeding covers a subset of entities; this change ensures every entity has representative data to enable integration testing and developer productivity.

## What Changes
- Implement a robust seeder (SeedFoundations / extended PocDataSeeder) that creates at least 5 records for every entity defined in AppDbContext.
- Ensure seeding is idempotent and safe to run multiple times.
- Register the seeder in DI and expose a safe startup invocation via ModuleExtensions.StartSeed(), gated to Development or when SeedEnabled=true.
- Provide configuration guidance to enable/disable seeding per-environment.
- Scaffold and reflect the five auth-related tables that are created by the front-end better-auth system into EF (database-first scaffolding) against the local_scaffold database, keeping their existing data intact.
- Detect schema drift from better-auth (plugins/settings) and generate EF migrations that update the rest of the system's tables to remain compatible with the current DB schema (apply to DB after review). See docs/scaffold-better-auth.md for scaffold command and generated file locations.

## Impact
- Large initial data insertion into the configured SQL Server database used for development/CI.
- Improved test coverage for repository and handler integration tests.
- Must be guarded or disabled in production environments to avoid injecting sample data.

## Requirements (Deltas)
## ADDED Requirements
### Requirement: Seed Foundations
The system SHALL provide a seeder that creates at least five (5) records for each entity declared in AppDbContext.

#### Scenario: Idempotent seed
- **WHEN** the seeder is executed multiple times
- **THEN** it MUST NOT duplicate existing records beyond the intended dataset and MUST complete without error.

#### Scenario: Environment gating
- **WHEN** running in production-like environments
- **THEN** seeding MUST be disabled unless explicitly enabled by configuration.

#### Scenario: Local dev/CI usage
- **WHEN** running in a developer or CI environment with seeding enabled
- **THEN** the database MUST contain at least 5 records per entity after seeding finishes.

## Date
2026-02-03T02:43:23.906Z

## Note
This proposal has been extended 2026-02-03T05:03:17.734Z to include database-first scaffolding of the five better-auth tables in local_scaffold, schema-drift detection for better-auth plugin changes, and generation of candidate migrations for system tables.

