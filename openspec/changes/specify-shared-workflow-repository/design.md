## Context

The arrangement this change specifies is already built and merged (PR #34), and runs in
forgekit, forgekit-ios, and forgekit-android. See proposal.md — Why for the motivation.

That ordering shapes this document. The decisions below were made during implementation, not in
advance of it; recording them here is what makes them reviewable and what lets the next change
argue with them. Where a decision was reached by finding out the hard way, this says so, because
the finding is the part worth carrying forward.

## Goals / Non-Goals

**Goals:**

- Describe the arrangement in terms a reader can check against any of the three repositories,
  not in terms of this repository's file layout.
- Record why the boundary falls where it does, so that a future file can be placed on the
  correct side of it without re-deriving the reasoning.

**Non-Goals:**

- Specifying the contents of the shared rules. What the OpenSpec rules and operation guidance
  say is upstream's business; this describes only how they arrive and stay intact.
- Specifying anything stack-specific. How a repository builds and tests itself belongs to its
  own verification command, which is explicitly on the repository's side of the boundary.
- Changing behaviour. Every requirement describes what the merged implementation already does.

## Decisions

**The delivery mechanism is a second git remote, not a package.**

Publishing the workflow as an npm package would give real version pinning and let dependency
automation bump it. It was rejected because the git hook and the MCP declaration have to be
actual files at fixed paths — a package would still need a step that copies them into place, so
the copy survives either way and the release overhead is added for nothing. The chosen mechanism
also reuses an idiom this repository already documents for syncing its shared API layer.

An OpenSpec `store` was rejected for a different reason: a store is registered on a machine, and
the binding requirement is that a fresh clone on a different laptop carries the rules with it.
That is the same reason these rules do not live in an agent's personal memory.

**The configuration file is split by marker, not by file.**

`openspec/config.yaml` is the one file that legitimately mixes shared content with
repository-owned content, and OpenSpec reads it as a single file. Splitting it would mean asking
the tool to support an include it does not have. A marker line with the shared content below it
keeps the file the tool expects while making the boundary explicit to a reader, and makes the
merge a truncate-and-append rather than a parse-and-splice.

Truncating at the marker rather than appending is what makes re-running safe. The alternative —
appending and hoping — produces a file with two `rules:` keys, which YAML resolves silently to
the last one; the failure would show up as guidance quietly changing rather than as an error.

**The stack half of preflight is declared as data, not branched in code.**

The alternative was a per-stack copy of the check, or a conditional inside it. Both reintroduce
the thing being removed: three copies that drift, or one file whose branches are exercised only
in the repository that uses them. A declaration keeps the script byte-identical everywhere, so a
fix lands in all three repositories at once and a bug in it is found by all three.

**A missing declaration fails rather than defaulting.**

This is the decision most worth arguing with, so the reasoning is explicit. A default is
available and superficially reasonable: with no source globs, measure every tracked file. It was
rejected because the check would then still produce an answer — a wrong one, moving on a README
edit — and a reader cannot distinguish it from a correct one. Failing costs a setup step;
defaulting costs the check's meaning.

**Machine-level tools are resolved against the machine, and labelled as such.**

Every other resolution in preflight deliberately avoids consulting the machine, because asking
`PATH` answers "does something by this name exist here", which passes for a shell builtin and
gives a different verdict on a fresh clone than on the author's laptop. A compiler cannot live in
the repository's toolchain, so for those tools there is no alternative. The report says which
kind of answer it is giving rather than blurring the two, because the weaker guarantee is only
honest if it is visible.

## Risks / Trade-offs

**A consuming repository can still be edited locally, and nothing prevents it** → The sync
overwrites it at the next run, which is the drift this arrangement accepts in exchange for not
building enforcement. Mitigated by documenting the boundary where the editor is looking: in
`AGENTS.md`, in the repository's dependency constraints, and in the marker line itself.

**Drift between repositories is possible until each one syncs** → Accepted deliberately. Unlike
duplicated package versions, which fail hard as a build error, a repository running last week's
preflight is merely behind — it does not break. The detector is preflight itself.

**The specification lives in one of the three consumers** → forgekit is the only one of the three
with an OpenSpec instance holding specs; the other two have the tooling but no specs yet. The
requirements written here govern all three, and there is nothing that holds forgekit-ios or
forgekit-android to them. See Open Questions.

**A shared file removed upstream is reported, not removed locally** → The sync says so rather
than deleting, because deleting files in a consumer on upstream's behalf is a larger power than
this mechanism should hold. The consequence is that a retired file lingers until someone acts on
the report.

**Found while verifying (task 3.2): the report does not change the exit code.** With
`openspec/rules.yaml` absent upstream, the sync reports `MISSING`, then completes the splice
from the stale local copy and exits `0`. The requirement as written is satisfied — it asks for a
report, and a report is given — but `pnpm sync-workflow && pnpm preflight` in an automated
caller stays green while the merged configuration silently comes from a file upstream has
retired. That is the "report nobody reads" failure this repository keeps designing against, and
it is under-specified rather than mis-implemented. Resolving it means deciding whether the
requirement should demand a non-zero exit, and is deliberately left to the change's reviewer
rather than settled here.

## Migration Plan

None. The implementation is merged and running; this change adds specification only. If the
requirements are found to describe something the implementation does not do, the correct
response is to fix the implementation or amend the requirement — not to migrate anything.

## Open Questions

- Should this specification move to the upstream repository, so that all three consumers are
  held to the same written contract rather than only this one? Deferring is safe: the
  requirements do not change if their home does, and the decision is better made once
  forgekit-ios and forgekit-android have specs of their own and it is clear whether they would
  read a shared one.

**Settled during implementation.** The scope reading in tasks.md 1.1 is agreed: where a
requirement describes something the implementation does not do, the response is to fix the
implementation upstream or amend the requirement — not to narrow the requirement to match what
happens to be there.

**Deferred for this change.** Whether this specification moves upstream is not decided here.
Nothing in the requirements changes if their home does, and the decision reads better once
forgekit-ios and forgekit-android have specs of their own.
