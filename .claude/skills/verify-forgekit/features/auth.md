# Authentication

A visitor can create an account with email and password, is signed in immediately, can sign in
again later, keeps the session across a reload, and is refused with a toast on a wrong
password. Signed-in-only routes send anonymous visitors to sign-in.

## Sub-features

- `auth-guard` sends an anonymous visitor on `/en/dashboard` to `/sign-in`.
- `auth-sign-up` creates the account from the sign-up form and establishes a session.
- `auth-sign-in` signs an existing account in; the session survives a reload.
- `auth-sign-in-wrong-password` refuses a wrong password and leaves no session.

## How to get to it (user POV)

- Open `/sign-up` (or `/zh-TW/sign-up`), or follow the `Sign up` link under the sign-in form.
- Open `/sign-in`, or any guarded URL such as `/en/dashboard` while signed out.

## Driving it with Playwright

Preconditions:

- Baseline from `README.md`. The journey generates its own email, `verify-<timestamp>@test.local`.

Steps:

- **Guard.** Visit `/en/dashboard`. The URL ends in `/sign-in`.
  Evidence: `01-guard-redirected-to-sign-in.png`.
- **Sign up.** On `/en/sign-up` fill `input[name="name"]`, `input[name="email"]`,
  `input[name="password"]`, `input[name="confirmPassword"]`, click `button[type="submit"]`.
  The URL leaves `/sign-up`, the toast "Account created successfully" shows, and
  `fetch("/api/auth/get-session")` returns the new email. Evidence: `02-*`, `03-*`.
- **Sign in, then reload.** On `/en/sign-in` fill email and password, submit, reload. The
  session still returns the email. Evidence: `04-*`.
- **Wrong password.** Submit a wrong password. The toast "Sign in failed. Please check your
  credentials" shows, the URL stays on sign-in, and `get-session` has no user. Evidence: `05-*`.
- **Side effect.** Take the email from `evidence/auth/03-session.json` and run
  `vf.sh sql "select u.email, (select count(*) from session s where s.userId = u.id) as sessions from user u where u.email = '<email>'"`.
  Expect one row with at least one session; save it as `evidence/auth/06-db-user-row.json`.

Run: `pnpm exec playwright test -c ../.claude/skills/verify-forgekit/playwright.config.ts journeys/auth.spec.ts`

## Gotchas

- There is no `/dashboard` page: after sign-up or sign-in the visitor lands on the home page,
  which is still the Next.js starter placeholder. Prove sign-in by the session, not the page.
- The sign-up route becomes off-limits once a session exists, so the proxy moves the visitor
  on; asserting one specific landing URL is brittle.
- `Login with SSO` needs Azure AD settings the run does not have; it is not covered.
- The wrong-password step must assert the toast: the URL alone would also pass if the form
  never submitted.
