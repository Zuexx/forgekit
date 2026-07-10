# Security Policy

## Supported Branch

Security fixes target `main`.

## Reporting A Vulnerability

Open a private security advisory on GitHub when available. If private advisories are unavailable, contact the repository owner directly and avoid posting exploit details in public issues.

Include:

- affected commit or release
- impacted API, app, dependency, or configuration
- reproduction steps
- whether credentials, tokens, or customer data are involved
- suggested mitigation if known

## Secret Handling

Do not commit secrets, local app settings, database URLs with credentials, private keys, tokens, or generated local databases.

Local-only files are ignored:

- `app/.env.local`
- `api/ForgeKit.Api/appsettings.Local.json`
- SQLite database files
- generated logs and build outputs

Use examples as templates only:

- `app/.env.local.example`
- `api/ForgeKit.Api/appsettings.Local.json.example`

Generate per-environment secrets. Never reuse starter-kit or CI values in production.

## Incident Response

If a secret is pushed:

1. Revoke or rotate the secret first.
2. Remove the secret from the current tree.
3. Rewrite Git history if the repository was pushed.
4. Force-push only after confirming collaborators understand the history rewrite.
5. Clear local reflogs and unreachable objects.
6. Verify with Gitleaks and GitHub secret scanning.
7. Resolve the alert only after the exposed secret is invalid.

Detailed steps are in [docs/SECRET_INCIDENT_RESPONSE.md](docs/SECRET_INCIDENT_RESPONSE.md).

## Security Checks

Run before release or before making a fork public:

```bash
gitleaks dir --redact .
gitleaks git --redact --log-opts=--all
openspec validate --all --strict --no-interactive
```

GitHub Actions also runs a full-history Gitleaks scan on push and pull request.
