# Remove Unread Path Header Implementation Plan

> **For agentic workers:** This plan's tasks are tightly coupled — one edit and its
> verification — so `superpowers:subagent-driven-development`'s own decision tree routes
> it to manual execution rather than per-task subagent dispatch.

**Goal:** Stop the proxy setting a response header that nothing reads.

**Architecture:** One removal in the allow branch of `performAction`, plus a test that
fails if the header returns.

**Tech Stack:** Next.js proxy, vitest.

## Global Constraints

- The allow branch must otherwise behave identically — still `NextResponse.next()`.
- `Resource.path` stays; the policy reads it.

## OpenSpec Coverage

- openspec/changes/remove-unread-path-header
- 1.1, 1.2, 2.1

## Task 1: Remove the header and pin its absence

- [x] Delete the `x-current-path` assignment from the allow branch of `app/proxies/actions.ts`
- [x] Add a test asserting an allowed response carries no `x-current-path`
- [x] Run `pnpm test` — existing proxy tests must pass unchanged
