# API health

The API reports whether its process is alive and whether it is ready to serve, the latter
including a database connectivity check.

## Sub-features

- `health-live` answers without touching dependencies.
- `health-ready` includes the `appdbcontext` database check.
- `health` reports the overall status with an entry per check.

## How to get to it (user POV)

- An operator or a load balancer requests `/health/live`, `/health/ready`, or `/health`.

## Driving it with curl

Preconditions:

- Baseline from `README.md`; `mkdir -p "$VF_EVIDENCE/api"`.

Steps:

- **Live.** `curl -s -w '\nHTTP %{http_code}\n' "$VF_API_URL/health/live" > "$VF_EVIDENCE/api/health-live.txt"`.
  Expect `HTTP 200` and `"status":"Healthy"`.
- **Ready.** The same for `/health/ready` into `health-ready.txt`. Expect `HTTP 200` with an
  `appdbcontext` entry that is `Healthy`.

## Gotchas

- `/health/live` stays 200 when the database is gone; only `/health/ready` and `/health`
  reflect it.
- The unhealthy path is not covered: `vf.sh` has no way to start the API against a broken
  database. Report it as unverified rather than deleting the database under a live process.
- `vf.sh doctor` already calls `/health`, so a doctor pass is not evidence for a health change.
