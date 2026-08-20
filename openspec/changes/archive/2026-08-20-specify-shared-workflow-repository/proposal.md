## Why

The AI workflow now runs in three repositories — this one, forgekit-ios, and forgekit-android —
from a single source in forgekit-workflow. That arrangement is load-bearing and entirely
undocumented as behaviour: nothing states which files a repository may edit, that re-running the
sync must be safe, or that the stack declaration preflight now depends on must fail loudly when
it is missing rather than quietly measure the wrong thing.

The `workflow-toolchain` spec still describes preflight, the hooks, and the configuration
readback accurately, but it describes them as if this repository owned them. A reader following
that spec would conclude that editing `scripts/preflight.sh` here is the way to change preflight,
which is precisely the mistake the arrangement exists to prevent — the edit survives until the
next sync and then disappears without a word.

## What Changes

- State that a defined set of workflow files is owned upstream and that a consuming repository
  edits them only upstream, with the sync as the delivery mechanism.
- State that the sync is idempotent: re-running it replaces the managed region of
  `openspec/config.yaml` rather than appending a second copy, and never touches the repository's
  own `context:` above the marker.
- State that the sync refuses to write a spliced configuration that lost its `rules:` or
  `operations:`, leaving the previous file in place.
- State that the stack-specific inputs preflight needs are declared by the repository rather
  than written into the shared script, and that an absent `sourceGlobs` declaration is a
  failure — extending the existing "a check that cannot measure its subject fails" requirement
  to the specific way this declaration can go missing.
- State that a tool the repository declares as a stack requirement counts as part of its
  declared toolchain for the purposes of the citation check.

No behaviour changes. Every requirement here describes what the merged implementation already
does; the change closes the gap between the spec and the repository.

## Capabilities

### New Capabilities

None. This extends an existing capability rather than introducing one.

### Modified Capabilities

- `workflow-toolchain`: adds requirements for shared-file ownership, sync idempotence, the
  refusal to write a broken splice, and the per-repository stack declaration. The existing
  requirements are unchanged; `A check that cannot measure its subject fails` gains a scenario
  naming the missing-declaration case.

## Impact

**Files.** `scripts/preflight.sh`, `scripts/sync-workflow.sh`, `.githooks/pre-push`, `.mcp.json`,
`.claude/settings.json`, `openspec/rules.yaml`, the managed region of `openspec/config.yaml`, and
the `forgekit` block of `package.json`.

**This impact was not grounded in `codegraph_explore`, and the reason matters.** The graph
indexes symbols; every artifact this change describes is a shell script, a JSON or YAML
declaration, or a marker line in a comment. Calling the graph here would return nothing and
reporting that nothing as a small blast radius would be false. The Impact below comes from
literal searches instead.

**What references these files** (`git grep -l`, excluding `openspec/changes/`):

| Literal | Files |
|---|---|
| `preflight` | 9 |
| `.mcp.json` | 6 |
| `sync-workflow` | 6 |
| `scripts/preflight.sh` | 5 |
| `scripts/sync-workflow.sh` | 4 |
| `.githooks/pre-push` | 3 |
| `openspec/rules.yaml` | 3 |

The marker literal `# >>> forgekit-workflow: managed region` is load-bearing in exactly two
places — `scripts/sync-workflow.sh`, where it is defined, and `openspec/config.yaml`, where it
delimits the managed region. A change to either without the other silently stops the splice
being idempotent, which is the reason it is worth specifying.

It occurs in two further places that are prose about it rather than uses of it: this document,
and the implementation plan. The line numbers are deliberately omitted — an earlier draft of
this paragraph quoted them, and they were wrong before the change was even merged, which is the
failure the proposal rule about checking figures before quoting them exists to prevent.

**Beyond this repository.** The same requirements govern forgekit-ios and forgekit-android, which
run the identical `scripts/preflight.sh` and `scripts/sync-workflow.sh`. Those repositories are
not edited by this change, and the spec written here is the one they are also held to.

**Not included.** The requirements describe the arrangement; they do not specify the contents of
`openspec/rules.yaml`, which is upstream's business, nor any stack-specific verification, which
belongs to each repository's own `scripts/verify.sh`.
