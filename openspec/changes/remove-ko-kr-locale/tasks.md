## 1. Remove ko-KR from routing and messages

- [ ] 1.1 Remove `"ko-KR"` from `routingConfig.locales` in `app/i18n/config.ts`
- [ ] 1.2 Remove the `'ko-KR': () => import('./ko-KR')` entry from `app/messages/index.ts`'s
  `locales` loader map
- [ ] 1.3 Delete the `app/messages/ko-KR/` directory (`auth.json`, `common.json`, `form.json`,
  `toast.json`, `validation.json`, `index.ts`)

## 2. Remove ko-KR from the UI

- [ ] 2.1 Remove the `"ko-KR"` entry from `localeLabels` in
  `app/components/locale-switcher.tsx`

## 3. Update tests and fixtures

- [ ] 3.1 In `app/proxies/resolve-context.test.ts`, change the inline routing fixture's
  locale list and the locale-stripped-path test case (`/ko-KR/about`) to use `zh-TW` instead
  of `ko-KR`

## 4. Update documentation

- [ ] 4.1 Update the i18n locale list in `README.md` (root)
- [ ] 4.2 Update the i18n locale list and remove the `ko-KR/` tree entry in
  `docs/STRUCTURE.md`
- [ ] 4.3 Update the locale list in `app/README.md`

## 5. Verify

- [ ] 5.1 `grep -rn "ko-KR\|Korean\|한국"` across the repo (excluding `node_modules`, `.next`,
  build output) returns no matches
- [ ] 5.2 `cd app && pnpm check && pnpm lint && pnpm test` all pass
- [ ] 5.3 Manually visit `/ko-KR` and `/ko-KR/sign-in` against a real dev/build server and
  confirm both 404 (matches the internationalization spec's "unsupported locale segment"
  requirement)
- [ ] 5.4 Manually confirm the locale switcher shows exactly `EN` and `中` (zh-TW), in that
  order, with no third option
