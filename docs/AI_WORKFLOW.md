# AI Workflow

Two pieces, each covering what the other leaves out:

- **pstack** (Claude Code plugin) — understands the code, implements, and verifies. Its stance
  is "the best spec is code", so it ships no planning layer.
- **Matt Pocock's grill skills** — settle the decisions *before* there is code to point at.

Everything repo-specific (this file, `AGENTS.md`, the `verify-forgekit` skill) lives in this
repository. pstack itself is never edited here.

## Installing

Opening the repository in Claude Code and trusting the folder prompts to install pstack;
`.claude/settings.json` declares it at project scope. No manual `/plugin` commands needed.

pstack is **pinned to a commit**: `.claude/settings.json` declares an inline marketplace whose
`pstack` entry points at `cursor/plugins`, path `harness/pstack/claude-code`, with a `sha`.
An upstream release changes how the agent behaves, so it does not arrive on its own. To bump:

1. Pick the new commit from <https://github.com/cursor/plugins/commits/main/harness/pstack>
   and read what changed.
2. Replace `sha` (and the version in `description`) in `.claude/settings.json`.
3. Run `claude plugin update pstack@pinned-plugins` and open a PR with the bump alone.

Personal pstack setup stays out of the repo: `/pstack:setup-pstack` writes its model routing
under `~/.claude/`. With only Claude models available, `/pstack:interrogate` falls back to
same-family reviewers, so treat it as a second opinion rather than an independent one.

The grill skills are vendored as plain files under `.claude/skills/` and recorded in
`skills-lock.json` (same mechanism as the Better Auth skills), so `dotnet new` carries them
into every product. Update with `npx skills update -p`. Only these are installed: `grilling`,
`grill-me`, `grill-with-docs`, `domain-modeling`, `to-spec`, `setup-matt-pocock-skills`. The
rest of that collection (`tdd`, `code-review`, `diagnosing-bugs`, ...) overlaps pstack and is
left out on purpose — two skills competing for one trigger is the problem this setup replaced.

## Which route

| Situation | Route |
|---|---|
| A product just generated with `dotnet new forgekit`; its domain is undecided | `/grill-with-docs` → `/to-spec` → pstack |
| New module, new auth / tenant / permission model, public API shape | `/grill-with-docs` → `/to-spec` → pstack |
| Stress-testing an idea with no intent to build it yet | `/grill-me` |
| Bug fix, refactor, performance, dependency bump, docs | `/pstack:poteto-mode <task>` directly |
| A one-line request that already has acceptance criteria | `/pstack:sharpen` first; grill only if decisions remain open |

### Grill → spec → build

1. `/grill-with-docs <idea>` — one decision at a time, each with a recommended answer, until
   every branch of the design tree is resolved. Resolved terms go into the root `GLOSSARY.md`,
   decisions that pass the bar become ADRs in `docs/adr/` (three-digit numbers; see
   `docs/agents/domain.md`).
2. `/to-spec` — turns the conversation into a GitHub issue labelled `ready-for-agent`
   (`docs/agents/issue-tracker.md`).
3. `/pstack:poteto-mode implement #<issue>` — the issue's Implementation Decisions are the
   scope. The PR says `Closes #<issue>`.

There is no spec archive. "Why is it like this" is answered by the ADRs, the glossary, the
issues, and git history — which is what `/pstack:why` reads. The OpenSpec archive that preceded
this setup was deleted, not migrated; `git log --all -- openspec/` still finds it.

## Verification

pstack's UI-driving skills (`control-ui`, `/deslop`) come from a kit with no Claude Code
version. `.claude/skills/verify-forgekit/` fills that gap: it launches the API and the app
against SQLite, drives features through Playwright and curl, and leaves evidence (screenshots,
responses, database rows) that survives cleanup. A UI change is not done until a feature from
its feature map has run and the evidence is attached to the PR.

Lessons go into checks, not prose. When an agent makes the same mistake twice, prefer a test,
lint rule, or build failure that makes it impossible (`/pstack:correct`) over a new sentence
in `AGENTS.md`. Examples already in place: the Anvil → product reference fails the build, an
anonymous query filter on a soft-delete entity fails at model build, `RegisterModules` without
a product assembly throws, and a missing migration fails CI.

## Trust level

Agents may open pull requests; they do not merge them. Required checks (API, App, Secret scan)
are enforced by the branch ruleset (`scripts/protect-branch.sh`), and a human merges. Raise
this only after a long run of agent PRs that merged without rework.

## Personal mode

Your own working preferences (reply style, what to confirm first, what "done" means) belong
in a personal `-mode` skill under `~/.claude/skills/`, generated with `/pstack:automate-me`.
It is not committed: it describes a person, and `dotnet new` would hand it to every product.
