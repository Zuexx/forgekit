## 1. Remove ko-KR from routing and messages

- [x] 1.1 Remove `"ko-KR"` from `routingConfig.locales` in `app/i18n/config.ts`
- [x] 1.2 Remove the `'ko-KR': () => import('./ko-KR')` entry from `app/messages/index.ts`'s
  `locales` loader map
- [x] 1.3 Delete the `app/messages/ko-KR/` directory (`auth.json`, `common.json`, `form.json`,
  `toast.json`, `validation.json`, `index.ts`)

## 2. Remove ko-KR from the UI

- [x] 2.1 Remove the `"ko-KR"` entry from `localeLabels` in
  `app/components/locale-switcher.tsx`

## 3. Update tests and fixtures

- [x] 3.1 In `app/proxies/resolve-context.test.ts`, change the inline routing fixture's
  locale list and the locale-stripped-path test case (`/ko-KR/about`) to use `zh-TW` instead
  of `ko-KR`

## 4. Update documentation

- [x] 4.1 Update the i18n locale list in `README.md` (root)
- [x] 4.2 Update the i18n locale list and remove the `ko-KR/` tree entry in
  `docs/STRUCTURE.md`
- [x] 4.3 Update the locale list in `app/README.md`

## 5. Verify

- [x] 5.1 `grep -rn "ko-KR\|Korean\|한국"` across the repo (excluding `node_modules`, `.next`,
  build output) returns no matches
- [x] 5.2 `cd app && pnpm check && pnpm lint && pnpm test` all pass — 0 type errors, 0 lint
  errors (1 pre-existing unrelated warning), 53/53 tests
- [x] 5.3 Verified against a real production build + a throwaway SQLite DB with a real
  signed-up user (not the dev database): unauthenticated `/ko-KR` safely redirects to
  `/sign-in` (ABAC's protected-by-default, same as any other unrecognized path — no locale
  segment is ever blindly trusted); **authenticated** `/ko-KR` and `/ko-KR/sign-in` both
  genuinely 404 with no leaked content, confirming the internationalization spec's requirement
  holds for the case that actually matters (the sibling kit's real bug was specifically an
  authenticated-visitor leak, not an unauthenticated one)
- [x] 5.4 Verified against the real rendered `/sign-in` page HTML: no Korean label text
  ("한국어"/"한") present; "中"/"English" (the two remaining locales) each appear exactly once
