# Secret Incident Response

Use this runbook when GitHub, GitGuardian, Gitleaks, or a reviewer reports an exposed secret.

## 1. Classify The Alert

Identify:

- secret type
- affected repository and branch
- commit SHA
- file path
- whether the secret is real, test-only, expired, or a false positive
- whether it reached a public remote

Do not paste the secret into issues, chat, commit messages, or logs.

## 2. Revoke Or Rotate First

If the secret could be valid, rotate it before rewriting history. History cleanup does not protect a credential that was already observed by a scanner or copied into an email notification.

Examples:

- rotate database passwords in the database provider
- revoke OAuth client secrets and create a new value
- revoke API tokens
- replace webhook signing secrets

## 3. Remove From The Current Tree

Replace committed secrets with:

- environment variable reads
- local ignored files
- deployment secret stores
- documentation placeholders that are not credential-shaped

Avoid examples like:

```text
database URL with an embedded username and password
connection string segment with an assigned password value
environment variable assignment with a token-like value
```

Prefer:

```text
DATABASE_URL=postgresql://localhost:5432/app
ConnectionStrings__Postgres=<read-from-secret-store>
```

## 4. Rewrite History When Needed

If the secret reached GitHub or another shared remote, rewrite history. For small starter-kit repositories, replacing the branch with a clean root commit is often simpler than filtering individual blobs.

After rewrite:

```bash
git push --force-with-lease origin main
git reflog expire --expire=now --all
git gc --prune=now --aggressive
```

Coordinate with collaborators before force-pushing. They will need to re-clone or reset local branches.

## 5. Verify

Run:

```bash
git rev-list --all --count
gitleaks git --redact --log-opts=--all
gitleaks dir --redact .
```

Search for known leaked markers without printing secrets into shared logs:

```bash
git grep -n -I "<unique-safe-marker>" $(git rev-list --all)
```

Use GitHub secret scanning to confirm no open alerts remain.

## 6. Resolve Alerts

Resolve the alert only after:

- the secret is rotated or confirmed false positive
- current tree is clean
- reachable Git history is clean
- CI secret scan passes

Use the most accurate resolution:

- revoked
- used in tests
- false positive
- will not fix, only when intentionally accepted and documented

## 7. Prevent Recurrence

After an incident, check:

- examples do not contain credential-shaped values
- `.gitignore` covers local config and generated databases
- CI scans full history
- local setup docs explain how to generate secrets
- team members understand that ignored files are local-only
