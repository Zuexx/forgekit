# ForgeKit verification map

The maintained source for verifying ForgeKit's user-facing behavior. Read this index, then
use the matching feature file as the recipe. Launch, doctor, and cleanup are in `../SKILL.md`.

## Baseline preconditions

- A run started by `vf.sh launch` in this session, and `vf.sh doctor` reports `doctor: OK`.
- `eval "$(bash .claude/skills/verify-forgekit/scripts/vf.sh env)"` has set `VF_APP_URL`
  (default `http://localhost:3100`), `VF_API_URL` (`http://localhost:5050`), `VF_EVIDENCE`.
- The database is the run's fresh file: no users, the migrated API schema, no seed data.
- Never drive an instance this run did not start.

## Driving conventions

- Browser features: Playwright journeys in `../journeys/`, run from `app/` with
  `-c ../.claude/skills/verify-forgekit/playwright.config.ts`.
- Prefer roles and accessible names; the auth forms are reached by `input[name=...]`, which
  is what the existing e2e suite uses too.
- The default locale (`en`) is unprefixed: `/en/sign-in` redirects to `/sign-in`.
- API features: curl against `$VF_API_URL`, saving the body and the status code.
- Each journey creates its own data (a fresh email per run), so no restore step is needed;
  cleanup discards the database.

## Proof and skip reporting

- A screenshot of the action and of the resulting state, plus a read-only second view of any
  write (`vf.sh sql`, or `get-session` for auth).
- Record the feature id with every artifact (`evidence/<feature>/NN-<step>.*`).
- Report an unreachable path with the attempted command and the unmet precondition. Do not
  report a feature as verified through a different entry point than the one that changed.

## Feature entry contract

Each feature file has an H1, one paragraph of user-visible behavior, then exactly these H2s:
`Sub-features`, `How to get to it (user POV)`, `Driving it with Playwright` (or `with curl`),
`Gotchas`.

## Features

- [Authentication](./auth.md): route guard, sign-up, sign-in, session persistence, wrong password.
- [Locale](./locale.md): `en` (unprefixed) and `zh-TW`, the language menu, the remembered choice.
- [API health](./api-health.md): liveness and readiness, including the database check.
- [Sample resources and API reference](./api-resources.md): `/v1/resources`, validation errors, Scalar.
