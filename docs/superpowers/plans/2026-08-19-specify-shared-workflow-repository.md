# Specify the Shared Workflow Arrangement — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove every requirement in the `workflow-toolchain` delta holds against the already-merged implementation across all three ForgeKit-family repositories, and fix whatever does not.

**Architecture:** The arrangement is already built and running; this change adds specification. So the work is verification, not construction. Each task takes one requirement, exercises it against the real repositories, and — critically — exercises its *negative* case too: a guard that has never been seen to fail is a guard nobody has evidence for. Where a check turns out not to hold, the fix goes upstream in forgekit-workflow and returns through `pnpm sync-workflow`, because that is the arrangement being specified.

**Tech Stack:** bash 3.2 (stock macOS), git, pnpm 11, OpenSpec 1.9, CodeGraph 1.5.

## Global Constraints

- **Destructive checks run against a scratch copy**, never the working tree. Use `$SCRATCH` (defined in Task 1). The two exceptions are single-file mutations that `git checkout -- <path>` restores exactly; those must be restored in the same step that made them, and the restoration verified with `git status --porcelain`.
- **Never edit a shared file in this repository.** `scripts/preflight.sh`, `scripts/sync-workflow.sh`, `.githooks/pre-push`, `.mcp.json`, `.claude/settings.json`, `openspec/rules.yaml` are owned by forgekit-workflow. A fix to any of them is made there, pushed, and pulled back with `pnpm sync-workflow`. Editing one here is the exact mistake this change exists to specify against.
- **`bash -n` after every script segment**, not once at the end.
- **Assert the value, never only the absence.** A step that checks "no error" passes when the path leading to it broke. Pair every absence check with something that establishes the path was taken — an exit code, a named output line, a count.
- Local repository paths: forgekit `/Users/zuexx/Documents/labs/prototype`, iOS `/Users/zuexx/Documents/labs/forgekit-ios`, Android `/Users/zuexx/Documents/labs/forgekit-android`, workflow `/Users/zuexx/Documents/labs/forgekit-workflow`.
- GitHub connectivity was down when this plan was written. Every step below runs offline; the only network-dependent work is pushing, which is already queued.

## OpenSpec Coverage

Change: `openspec/changes/specify-shared-workflow-repository`

| Task id | Covered by |
|---|---|
| 1.1 | Task 1 |
| 1.2 | Task 1 |
| 2.1 | Task 2 |
| 2.2 | Task 2 |
| 3.1 | Task 3 |
| 3.2 | Task 3 |
| 4.1 | Task 4 |
| 4.2 | Task 4 |
| 5.1 | Task 5 |
| 5.2 | Task 5 |
| 5.3 | Task 5 |
| 6.1 | Task 6 |
| 6.2 | Task 6 |

---

### Task 1: Settle the two decisions and prepare the scratch area

**Files:**
- Modify: `openspec/changes/specify-shared-workflow-repository/design.md` (Open Questions section)
- Modify: `openspec/changes/specify-shared-workflow-repository/tasks.md` (tick 1.1, 1.2)

**Interfaces:**
- Produces: `$SCRATCH` — an absolute path every later task uses for destructive checks.

- [ ] **Step 1: Define the scratch area**

```bash
export SCRATCH=/private/tmp/claude-501/-Users-zuexx-Documents-labs-prototype/525ec639-0cd1-4460-97ed-282e747c12f5/scratchpad/spec-verify
rm -rf "$SCRATCH" && mkdir -p "$SCRATCH"
echo "SCRATCH=$SCRATCH"
```

Expected: the path is printed and the directory exists and is empty.

- [ ] **Step 2: Record that the scope reading is agreed (task 1.1)**

Append to the Open Questions section of `design.md`:

```markdown
**Settled during implementation.** The scope reading in tasks.md 1.1 is agreed: where a
requirement describes something the implementation does not do, the response is to fix the
implementation upstream or amend the requirement — not to narrow the requirement to match what
happens to be there.
```

- [ ] **Step 3: Record that the open question stays deferred (task 1.2)**

Append to the same section:

```markdown
**Deferred for this change.** Whether this specification moves upstream is not decided here.
Nothing in the requirements changes if their home does, and the decision reads better once
forgekit-ios and forgekit-android have specs of their own.
```

- [ ] **Step 4: Verify design.md still validates**

Run: `./node_modules/.bin/openspec validate specify-shared-workflow-repository --strict`
Expected: `Change 'specify-shared-workflow-repository' is valid`

- [ ] **Step 5: Tick 1.1 and 1.2, then commit**

```bash
git add openspec/changes/specify-shared-workflow-repository/
git commit -m "docs(openspec): settle the scope reading and defer the spec's home"
```

---

### Task 2: The ownership boundary is one list, not three

**Files:**
- Read: `scripts/sync-workflow.sh` (the `SHARED_PATHS` array)
- Read: `AGENTS.md` (the "The shared workflow" section)
- Read: `docs/DEPENDENCY_CONSTRAINTS.md` (the "The shared workflow repository" entry)
- Create: `$SCRATCH/boundary-check.sh`

**Interfaces:**
- Consumes: `$SCRATCH` from Task 1.
- Produces: nothing later tasks depend on.

- [ ] **Step 1: Write the extractor**

```bash
cat > "$SCRATCH/boundary-check.sh" <<'EOF'
#!/usr/bin/env bash
# Extracts the shared-file list from each of the three places that state it, and reports any
# path present in one and missing from another. Three lists that must agree is exactly the
# shape of drift this change specifies against.
set -uo pipefail
R=/Users/zuexx/Documents/labs/prototype

sed -n '/^SHARED_PATHS=(/,/^)/p' "$R/scripts/sync-workflow.sh" \
  | grep -vE '^SHARED_PATHS=\(|^\)' | tr -d ' ' | sort -u > "$SCRATCH/from-sync.txt"

grep -oE '`[a-z._/-]+\.(sh|json|yaml)`|`\.githooks/pre-push`' "$R/AGENTS.md" \
  | tr -d '`' | sort -u > "$SCRATCH/from-agents.txt"

grep -oE '`[a-z._/-]+\.(sh|json|yaml)`|`\.githooks/pre-push`' "$R/docs/DEPENDENCY_CONSTRAINTS.md" \
  | tr -d '`' | sort -u > "$SCRATCH/from-constraints.txt"

echo "sync declares:        $(wc -l < "$SCRATCH/from-sync.txt" | tr -d ' ')"
echo "AGENTS.md mentions:   $(wc -l < "$SCRATCH/from-agents.txt" | tr -d ' ')"
echo "constraints mention:  $(wc -l < "$SCRATCH/from-constraints.txt" | tr -d ' ')"
echo
echo "--- in sync but not named in AGENTS.md ---"
comm -23 "$SCRATCH/from-sync.txt" "$SCRATCH/from-agents.txt"
echo "--- in sync but not named in DEPENDENCY_CONSTRAINTS.md ---"
comm -23 "$SCRATCH/from-sync.txt" "$SCRATCH/from-constraints.txt"
EOF
bash -n "$SCRATCH/boundary-check.sh" && echo "syntax ok"
```

Expected: `syntax ok`.

- [ ] **Step 2: Run it and read the output**

Run: `bash "$SCRATCH/boundary-check.sh"`
Expected: both `comm` sections are empty. Every path the sync overwrites is named in both documents.

If a path appears under either heading, that is a real finding: the documented boundary does not cover a file the sync replaces, so a reader has no way to know that file is not theirs. Fix the document — and if the missing path belongs to `AGENTS.md`, fix it here, because `AGENTS.md` is repository-owned.

- [ ] **Step 3: Prove a repository-owned file survives a sync (task 2.2)**

```bash
cd /Users/zuexx/Documents/labs/prototype
md5 -q scripts/verify.sh package.json > "$SCRATCH/owned-before.txt"
sed -n '1,60p' openspec/config.yaml | md5 -q >> "$SCRATCH/owned-before.txt"
pnpm sync-workflow
md5 -q scripts/verify.sh package.json > "$SCRATCH/owned-after.txt"
sed -n '1,60p' openspec/config.yaml | md5 -q >> "$SCRATCH/owned-after.txt"
diff "$SCRATCH/owned-before.txt" "$SCRATCH/owned-after.txt" && echo "owned files unchanged"
```

Expected: `owned files unchanged`, and `git status --porcelain` reports nothing.

This asserts the checksums match rather than merely that the sync exited zero — a sync that did nothing at all would also exit zero.

- [ ] **Step 4: Tick 2.1 and 2.2, then commit**

```bash
git add openspec/changes/specify-shared-workflow-repository/tasks.md
git commit -m "docs(openspec): verify the ownership boundary is stated consistently"
```

---

### Task 3: Synchronising is repeatable, and says so when upstream drops a file

**Files:**
- Read: `scripts/sync-workflow.sh`
- Read: `openspec/config.yaml` (the marker line)
- Create: `$SCRATCH/upstream-missing/` (a scratch clone of forgekit-workflow)

**Interfaces:**
- Consumes: `$SCRATCH` from Task 1.

- [ ] **Step 1: Run the sync twice and diff the results (task 3.1)**

```bash
cd /Users/zuexx/Documents/labs/prototype
pnpm sync-workflow >/dev/null 2>&1
git status --porcelain > "$SCRATCH/after-first.txt"
md5 -q openspec/config.yaml > "$SCRATCH/config-first.txt"
pnpm sync-workflow >/dev/null 2>&1
git status --porcelain > "$SCRATCH/after-second.txt"
md5 -q openspec/config.yaml > "$SCRATCH/config-second.txt"
diff "$SCRATCH/after-first.txt" "$SCRATCH/after-second.txt" && echo "tree identical"
diff "$SCRATCH/config-first.txt" "$SCRATCH/config-second.txt" && echo "config identical"
```

Expected: both `tree identical` and `config identical`.

- [ ] **Step 2: Assert exactly one marker line**

```bash
grep -c '^# >>> forgekit-workflow: managed region' openspec/config.yaml
```

Expected: `1`.

This is the assertion that would catch an append-instead-of-truncate regression. Two markers means two `rules:` keys, which YAML resolves silently to the last one — the guidance would change without any error being raised, which is why it is checked by count rather than by "the sync succeeded".

- [ ] **Step 3: Build a scratch upstream with a file removed (task 3.2)**

```bash
git clone -q /Users/zuexx/Documents/labs/forgekit-workflow "$SCRATCH/upstream-missing"
cd "$SCRATCH/upstream-missing"
git rm -q openspec/rules.yaml
git -c user.name=t -c user.email=t@t commit -q -m "remove rules.yaml to exercise the missing-path report"
cd -
```

Expected: the clone exists and its HEAD no longer contains `openspec/rules.yaml`.

Verify that before continuing: `git -C "$SCRATCH/upstream-missing" cat-file -e HEAD:openspec/rules.yaml` must exit **non-zero**.

- [ ] **Step 4: Point a scratch consumer at it and run the sync**

```bash
git clone -q /Users/zuexx/Documents/labs/prototype "$SCRATCH/consumer"
cd "$SCRATCH/consumer"
git remote add workflow "$SCRATCH/upstream-missing"
bash scripts/sync-workflow.sh 2>&1 | tee "$SCRATCH/missing-run.txt"
grep -q "MISSING" "$SCRATCH/missing-run.txt" && echo "reported the missing path"
grep -q "openspec/rules.yaml" "$SCRATCH/missing-run.txt" && echo "named the file"
cd -
```

Expected: both `reported the missing path` and `named the file`.

The clone is used so this never touches the real working tree, and so the run exercises the same code path a real consumer would take.

- [ ] **Step 5: Tick 3.1 and 3.2, then commit**

```bash
cd /Users/zuexx/Documents/labs/prototype
git add openspec/changes/specify-shared-workflow-repository/tasks.md
git commit -m "docs(openspec): verify the sync is idempotent and reports dropped files"
```

---

### Task 4: A broken merge cannot be written, and an empty one is still caught

**Files:**
- Read: `scripts/sync-workflow.sh` (the guard before `mv`)
- Modify (then restore): `openspec/config.yaml`

**Interfaces:**
- Consumes: `$SCRATCH/consumer` from Task 3.

- [ ] **Step 1: Make the merge produce a result without its sections (task 4.1)**

Work in the scratch consumer, and empty the upstream rules file so the spliced result has no `rules:`:

```bash
cd "$SCRATCH/consumer"
git remote set-url workflow /Users/zuexx/Documents/labs/forgekit-workflow
git fetch -q workflow main
md5 -q openspec/config.yaml > "$SCRATCH/config-before-guard.txt"
: > openspec/rules.yaml       # the file the splice reads from, now empty
bash scripts/sync-workflow.sh 2>&1 | tee "$SCRATCH/guard-run.txt"
echo "exit=${PIPESTATUS[0]}"
```

Expected: a non-zero exit and a line containing `FAIL` and `left untouched`.

Note the sync re-fetches shared files first, which restores `openspec/rules.yaml` from upstream. If the guard does not fire on this route, empty the file *after* the fetch by editing the script's inputs instead; the requirement is about the merged result, so any route that produces a sectionless result is a valid exercise.

- [ ] **Step 2: Assert the existing configuration was left alone**

```bash
md5 -q openspec/config.yaml > "$SCRATCH/config-after-guard.txt"
diff "$SCRATCH/config-before-guard.txt" "$SCRATCH/config-after-guard.txt" && echo "config untouched"
cd -
```

Expected: `config untouched`.

This is the assertion that matters. A guard that fails loudly but has already overwritten the file protects nothing.

- [ ] **Step 3: Exercise the deeper readback (task 4.2)**

In the real repository, break a block the way YAML actually breaks — a plain scalar containing `': '` — then confirm preflight catches what the merge guard cannot:

```bash
cd /Users/zuexx/Documents/labs/prototype
python3 - <<'PY'
import io
p='openspec/config.yaml'
s=io.open(p,encoding='utf-8').read()
s=s.replace("rules:\n  proposal:\n", "rules:\n  proposal:\n    - broken: a plain scalar with a colon space\n",1)
io.open(p,'w',encoding='utf-8').write(s)
PY
pnpm preflight; echo "preflight exit=$?"
```

Expected: non-zero exit, with a line reporting the proposal rules or apply guidance as unreadable.

- [ ] **Step 4: Confirm the merge guard would NOT have caught it**

```bash
grep -c '^rules:' openspec/config.yaml
grep -c '^operations:' openspec/config.yaml
```

Expected: `1` and `1`.

Both sections are present, so the sync's own guard passes — which is the point of having the deeper readback as a separate requirement rather than folding the two into one.

- [ ] **Step 5: Restore and confirm the restoration**

```bash
git checkout -- openspec/config.yaml
git status --porcelain openspec/config.yaml
pnpm preflight | tail -1
```

Expected: `git status` prints nothing, and preflight's last line is `Workflow is operational.`

- [ ] **Step 6: Tick 4.1 and 4.2, then commit**

```bash
git add openspec/changes/specify-shared-workflow-repository/tasks.md
git commit -m "docs(openspec): verify the merge guard and the configuration readback"
```

---

### Task 5: The declaration governs what the shared check measures

**Files:**
- Modify (then restore): `package.json` (the `forgekit` block)
- Modify (then restore): `AGENTS.md` (one citation, for the negative case)
- Read: `scripts/preflight.sh` in all three repositories

**Interfaces:**
- Consumes: `$SCRATCH` from Task 1.

- [ ] **Step 1: Remove sourceGlobs and run preflight (task 5.1)**

```bash
cd /Users/zuexx/Documents/labs/prototype
python3 - <<'PY'
import json, io, collections
p='package.json'
c=json.load(io.open(p,encoding='utf-8'), object_pairs_hook=collections.OrderedDict)
c['forgekit']['sourceGlobs']=[]
io.open(p,'w',encoding='utf-8').write(json.dumps(c,indent=2,ensure_ascii=False)+"\n")
PY
pnpm preflight 2>&1 | tee "$SCRATCH/no-globs.txt"; echo "exit=${PIPESTATUS[0]}"
```

Expected: non-zero exit, and a line reading `no source globs declared, so index freshness cannot be measured`.

- [ ] **Step 2: Assert no index verdict was given while the declaration was missing**

```bash
grep -c "index present and reflects current source" "$SCRATCH/no-globs.txt"
grep -c "index is older than" "$SCRATCH/no-globs.txt"
```

Expected: `0` and `0`.

This is the requirement's real content. Reporting a failure is not enough — the check must not also emit a verdict about the index, because a verdict alongside a failure is exactly the "substituted a broader subject" behaviour the requirement forbids.

- [ ] **Step 3: Restore and confirm**

```bash
git checkout -- package.json
git status --porcelain package.json
pnpm preflight | tail -1
```

Expected: nothing from `git status`, and `Workflow is operational.`

- [ ] **Step 4: Confirm a declared stack tool resolves as a citation (task 5.2)**

`AGENTS.md` already cites `dotnet`, which `forgekit.requiredTools` declares.

```bash
pnpm preflight 2>&1 | grep -E "^  (ok|FAIL) +dotnet"
```

Expected: a line reading `ok    dotnet (on PATH)`.

- [ ] **Step 5: Confirm an undeclared, uninstalled citation still fails**

```bash
python3 - <<'PY'
import io
p='AGENTS.md'
s=io.open(p,encoding='utf-8').read()
s=s.replace("## Conventions that are easy to get wrong",
            "Temporary negative check: `notarealtool`\n\n## Conventions that are easy to get wrong",1)
io.open(p,'w',encoding='utf-8').write(s)
PY
pnpm preflight 2>&1 | grep -E "notarealtool"; echo "exit=${PIPESTATUS[0]}"
git checkout -- AGENTS.md
git status --porcelain AGENTS.md
```

Expected: a `FAIL` line naming `notarealtool` as not in the declared toolchain, then nothing from `git status`.

Both halves are needed. Step 4 alone would pass even if the check resolved everything unconditionally; Step 5 is what shows it still discriminates.

- [ ] **Step 6: Confirm the shared check is byte-identical across the three repositories (task 5.3)**

```bash
for d in /Users/zuexx/Documents/labs/prototype \
         /Users/zuexx/Documents/labs/forgekit-ios \
         /Users/zuexx/Documents/labs/forgekit-android; do
  printf "%s  %s\n" "$(md5 -q "$d/scripts/preflight.sh")" "$(basename "$d")"
done | sort
```

Expected: three lines, all with the same checksum.

If they differ, the repository that is behind needs `pnpm sync-workflow` — and the difference itself is worth reading before syncing, because a local edit to a shared file is the failure the arrangement is specified against.

- [ ] **Step 7: Confirm all three report the workflow operational**

```bash
for d in /Users/zuexx/Documents/labs/prototype \
         /Users/zuexx/Documents/labs/forgekit-ios \
         /Users/zuexx/Documents/labs/forgekit-android; do
  printf "%-20s %s\n" "$(basename "$d")" "$(cd "$d" && pnpm preflight 2>&1 | tail -1)"
done
```

Expected: three lines, each ending `Workflow is operational.`

- [ ] **Step 8: Tick 5.1, 5.2 and 5.3, then commit**

```bash
cd /Users/zuexx/Documents/labs/prototype
git add openspec/changes/specify-shared-workflow-repository/tasks.md
git commit -m "docs(openspec): verify the stack declaration governs the shared check"
```

---

### Task 6: Close the change

**Files:**
- Modify: `openspec/changes/specify-shared-workflow-repository/tasks.md`

**Interfaces:**
- Consumes: everything above.

- [ ] **Step 1: Run the full local gate (task 6.1)**

```bash
cd /Users/zuexx/Documents/labs/prototype
pnpm preflight; echo "preflight exit=$?"
pnpm verify;   echo "verify exit=$?"
```

Expected: both exit `0`, with the test counts read in the session rather than assumed. `pnpm verify` runs `pnpm install --frozen-lockfile` in `app/`; if GitHub is still unreachable this may fail on network rather than on the change. A network failure is not a passing gate — report it as blocked rather than ticking 6.1.

- [ ] **Step 2: Clean up the scratch area**

```bash
rm -rf "$SCRATCH"
ls -d "$SCRATCH" 2>&1 | grep -q "No such file" && echo "scratch removed"
```

Expected: `scratch removed`.

- [ ] **Step 3: Request review (task 6.2, first half)**

Use `superpowers:requesting-code-review` against the branch's diff. The reviewer did not sit through this session, which is the point — the things worth catching are the ones stopped being questioned after the third read.

- [ ] **Step 4: Tick 6.1 and 6.2 only after review passes, then commit**

```bash
git add openspec/changes/specify-shared-workflow-repository/tasks.md
git commit -m "docs(openspec): close out the shared-workflow specification change"
```

A tick is a claim about the repository, not a note about intent — so this step comes after the review, not before it.

- [ ] **Step 5: Archive against the deltas, not this checklist**

Run `/opsx:archive`. Check each delta requirement against observed behaviour and write any divergence into the archive. Ticked boxes are a claim; the deltas are the acceptance criteria and are what the next reader will consult.
