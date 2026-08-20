## 1. Decisions settled before any work starts

- [x] 1.1 Confirm the scope boundary: this change adds specification and reconciles the
  implementation with it. It does not redesign the arrangement. If a requirement turns out to
  describe something the implementation does not do, the response is to fix the implementation
  or amend the requirement — never to quietly narrow the requirement to match. Done when this
  reading is agreed and recorded in the change.
- [x] 1.2 Confirm the open question in design.md (whether the specification should move
  upstream) stays deferred for this change. Done when the change either records the deferral or
  carries the answer.

## 2. The ownership boundary holds

- [x] 2.1 Verify every file the spec calls shared is actually delivered by the sync and listed
  in the documented boundary, and that nothing outside that list is touched. Done when the
  shared-path list in the sync, the boundary section in `AGENTS.md`, and the constraint entry in
  `docs/DEPENDENCY_CONSTRAINTS.md` name the same set, with no file in one and missing from
  another.
- [x] 2.2 Verify a repository-owned file survives a sync. Done when running the sync leaves
  `scripts/verify.sh`, `package.json`, and the `context:` block of `openspec/config.yaml`
  byte-identical, shown by a diff that reports no change to them.

## 3. Synchronising is repeatable

- [x] 3.1 Verify the sync is idempotent by running it twice from a clean tree. Done when the
  second run produces no diff against the first, and `openspec/config.yaml` contains exactly one
  marker line.
- [x] 3.2 Verify the sync reports a shared file that upstream no longer publishes, rather than
  leaving a stale copy unremarked. Done when a shared path is temporarily absent upstream and the
  sync names it, demonstrated against a scratch clone rather than by reading the code.

## 4. A broken merge cannot be written

- [x] 4.1 Verify the merge guard leaves the existing configuration untouched when the result
  would lose its sections. Done when the guard is made to fail against a scratch copy and
  `openspec/config.yaml` is shown unchanged afterwards.
- [x] 4.2 Verify the deeper readback still catches a configuration that parses but yields
  nothing, since the merge guard only checks the sections are present. Done when `pnpm preflight`
  reports the configuration as failing for a file whose blocks are emptied by a plain scalar
  containing `': '`, and passing once repaired.

## 5. The stack declaration governs what the shared check measures

- [x] 5.1 Verify an absent or empty `sourceGlobs` fails rather than measuring every tracked file.
  Done when removing the declaration makes `pnpm preflight` exit non-zero naming the missing
  declaration, and no check reports an index verdict while it is missing.
- [x] 5.2 Verify a declared stack tool resolves when an instruction file cites it, and that a
  tool that is neither declared nor installed still fails. Done when both cases are exercised in
  this repository and `pnpm preflight` gives opposite verdicts for them.
- [x] 5.3 Verify the same shared check runs unmodified against a different stack. Done when
  `scripts/preflight.sh` is byte-identical in forgekit, forgekit-ios, and forgekit-android, shown
  by comparing checksums, and all three report the workflow as operational.

## 6. Close the change

- [x] 6.1 Run the full local gate and record its output. Done when `pnpm preflight` and
  `pnpm verify` both exit zero in this repository with their output read in the session, not
  assumed.
  **Ran clean once the environment recovered.** `pnpm preflight` exit 0; `pnpm verify` exit 0
  with `Build succeeded` and `Passed! 5` + `Passed! 185`, 3 skipped by design. The earlier
  failure — `error MSB4018: CreateAppHost` / `System.OverflowException at
  Interop.Sys.IsMemberOfGroup(UInt32 gid)`, alongside DNS resolution failing in the same shell —
  reproduced on `main` in a separate worktree and in Debug as well as Release, and disappeared
  with the environment rather than with any change to this branch.
- [x] 6.2 Request review, then archive the change against the spec deltas rather than against
  this checklist. Done when the delta requirements are each checked off against observed
  behaviour and any divergence is written into the archive.
  Review raised five findings. The two that described behaviour the repository did not have —
  a missing-declaration clause quantified over all three declarations when only `sourceGlobs`
  behaved that way, and a claim that machine-resolved stack tools join the declared toolchain,
  which contradicted `Declared workflow toolchain` — were fixed in #36 rather than narrowed to
  match, per 1.1. The two empty-enumeration holes it found in preflight were fixed upstream in
  the same PR, and the `MISSING`/exit 0 behaviour it argued should be tightened was tightened.
