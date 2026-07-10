Seed Foundations

How to enable seeding for local development and CI

- By default seeding will run automatically when ASPNETCORE_ENVIRONMENT is Development.
- To enable seeding in other environments set the environment variable or appsetting "SeedEnabled": true.

Rollback instructions

- A rollback SQL script is provided at openspec/changes/add-seed-foundations/sql/rollback.sql.
- To rollback seeded data run the script against the development database (use caution).

Notes

- Seeder is idempotent and avoids running when there are already >=5 regions.
- Production should not enable seeding; ensure SeedEnabled is false or absent in production configuration.