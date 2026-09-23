## Why

The kit ships three locales (en, zh-TW, ko-KR), but only English and Traditional Chinese
are actually needed going forward. Carrying a third locale that nobody maintains costs every
future i18n change an extra translation file to keep in sync and an extra branch in the
locale switcher, for a language this kit does not need to support.

## What Changes

- **BREAKING**: Remove `ko-KR` from `routingConfig.locales` (`app/i18n/config.ts`) — a URL
  under `/ko-KR/...` that previously rendered now 404s via `next-intl`'s existing
  `hasLocale`/`notFound()` check in `app/app/[locale]/layout.tsx`, the same mechanism that
  already 404s any other unsupported segment today.
- Remove the `ko-KR` entry from `messages/index.ts`'s `locales` loader map and delete the
  `messages/ko-KR/` directory (`auth.json`, `common.json`, `form.json`, `toast.json`,
  `validation.json`, `index.ts`).
- Remove the `ko-KR` entry from `LocaleSwitcher`'s `localeLabels` map
  (`app/components/locale-switcher.tsx`) — the switcher already derives its item list from
  `routingConfig.locales`, so removing the config entry alone would leave a dead label; both
  need to change together.
- Update `app/proxies/resolve-context.test.ts`'s inline routing fixture and its
  locale-stripped-path test case off `ko-KR` onto `zh-TW` — this test builds its own fixture
  rather than importing the real config, so it does not break either way, but keeping a test
  case exercised against a locale the kit no longer ships is misleading.
- Update locale mentions in `README.md` (root), `app/README.md`, and `docs/STRUCTURE.md` to
  drop ko-KR.

## Capabilities

### New Capabilities
- `internationalization`: which locales this kit ships and how a fork adds or removes one —
  no existing spec covers this area today.

### Modified Capabilities
(none — no existing capability spec mentions locales)

## Impact

Grounded in `codegraph_explore` over the `forgekit` project (not estimated):

- `routingConfig` (`app/i18n/config.ts:1`) — 3 callers: `app/components/locale-switcher.tsx`,
  `app/i18n/routing.ts`, `app/proxy.ts`. No covering tests found for the real symbol (the one
  test that exercises this shape, `resolve-context.test.ts`, uses its own inline fixture, not
  this config, so it verifies the *mechanism* but not this specific locale list).
- `LocaleSwitcher` (`app/components/locale-switcher.tsx:21`) — 8 callers across
  `app/app/[locale]/(user)/layout.tsx`, `app/components/nav-breadcrumb.tsx`,
  `sign-in-card.tsx`, `sign-up-card.tsx`. No covering tests found.
- `routing` (`app/i18n/routing.ts:5`) — 2 callers: `app/app/[locale]/layout.tsx`,
  `app/i18n/request.ts`.
- `messages/index.ts`'s `locales` loader map and `getMessages` — not indexed as a named
  symbol by codegraph in this pass; found and confirmed by direct read instead, since it is a
  plain object literal (`en`/`ko-KR`/`zh-TW` dynamic `import()`s), the kind of contract
  codegraph's symbol index does not resolve by string key.
- A literal grep across `app/` for `ko-KR`/`Korean`/`한국` (not caught by the symbol graph,
  per the same caveat) found exactly five files plus the `messages/ko-KR/` directory itself:
  `messages/index.ts`, `components/locale-switcher.tsx`, `i18n/config.ts`,
  `proxies/resolve-context.test.ts`, and `app/README.md`. The same search at the repo root
  additionally found `README.md` and `docs/STRUCTURE.md`. No other file, including every e2e
  spec under `app/e2e/`, references `ko-KR`.
- No currently-shipped API or component prop is typed against the specific locale set (e.g.
  no `type Locale = 'en' | 'zh-TW' | 'ko-KR'` union outside `messages/index.ts`'s own
  `Locale = keyof typeof locales`, which narrows automatically once the `ko-KR` entry is
  removed) — nothing beyond the files above should need a change.
- What breaks if this is wrong: a fork that already relies on `ko-KR` support (translated
  content already written against it, or a bookmarked `/ko-KR/...` URL) loses that locale
  outright with no migration path other than reverting this change or re-adding the messages
  themselves. This is accepted as the intended, explicit behavior of the change, not a defect
  to guard against.
