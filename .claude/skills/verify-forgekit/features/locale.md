# Locale

The app speaks English and Traditional Chinese. English is the default and has no URL prefix;
Chinese lives under `/zh-TW`. The language menu switches locale and the choice is remembered.

## Sub-features

- `locale-prefix` serves `zh-TW` under `/zh-TW/...` and redirects `/en/...` to the unprefixed URL.
- `locale-switch` switches language from the menu and remembers it for unprefixed URLs.

## How to get to it (user POV)

- Type `/zh-TW/sign-in`, or any page under `/zh-TW`.
- Click the language button (accessible name `Change language`) on the sign-in or sign-up
  card, then `中` or `EN`.

## Driving it with Playwright

Preconditions:

- Baseline from `README.md`, in a fresh browser context (no `NEXT_LOCALE` cookie).

Steps:

- **Prefix.** Visit `/zh-TW/sign-in`: the button named `登入` is visible. Visit `/en/sign-in`:
  the path becomes `/sign-in` and the button is `Login`. Evidence: `01-zh-TW-sign-in.png`.
- **Switch.** On `/sign-in` click `Change language`, then the item `中`. The URL becomes
  `/zh-TW/sign-in` and `登入` shows. Evidence: `02-switched-to-zh-TW.png`.
- **Remembered.** Visit `/sign-in` again: it lands on `/zh-TW/sign-in`.
  Evidence: `03-choice-persisted.png`.

Run: `pnpm exec playwright test -c ../.claude/skills/verify-forgekit/playwright.config.ts journeys/locale.spec.ts`

## Gotchas

- The language menu is a radial menu; its items exist only after the toggle is clicked.
- `NEXT_LOCALE` persists for a year in a reused browser profile; Playwright's fresh context is
  what makes the first assertion meaningful.
- Translations live in `app/messages/<locale>/*.json`; a missing key renders the key path,
  which a screenshot shows and a URL assertion does not.
