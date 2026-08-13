# Tasks: Specify Authentication

## 1. Write the capability

- [x] 1.1 Describe credential registration, including the rules the form enforces
- [x] 1.2 Describe credential sign-in and session persistence
- [x] 1.3 Describe route authorization, including the fail-closed rule
- [x] 1.4 Describe the configuration boundaries a fork inherits

## 2. Verify the implementation matches

- [x] 2.1 Registration, sign-in, session survival, and wrong-password rejection pass end to end against PostgreSQL
- [x] 2.2 Both sign-up schemas accept a valid registration and reject mismatched passwords
- [x] 2.3 Reverting the fail-closed action turns its test red
- [x] 2.4 Reverting the segment-boundary API match turns its test red
- [x] 2.5 Changing AUTH_COOKIE leaves session detection working
- [x] 2.6 No administrators exist when BETTER_AUTH_ADMIN_USER_IDS is empty

## 3. Close out

- [x] 3.1 `openspec validate --strict` passes
- [x] 3.2 Archive so `authentication` becomes a specification
