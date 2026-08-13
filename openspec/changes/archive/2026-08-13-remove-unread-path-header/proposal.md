## Why

`performAction` sets an `x-current-path` response header on every allowed request, and nothing reads it. The only occurrence of the name in the repository is the line that writes it, and the sole consumer of `next/headers` reads cookies. It is a contract with no other party — carried on every page response, and liable to be mistaken for something load-bearing by the next person to touch the proxy.

## What Changes

- Remove the `x-current-path` header from `performAction`.
- Record the header's absence in the `authentication` capability, so a later change that wants the current path in a server component adds it deliberately rather than assuming it is already there.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `authentication` — the route authorization requirement gains a scenario stating that an allowed response carries no header describing the decision or the resolved route.

## Impact

Blast radius: `performAction` has one production call site, `app/proxies/create-proxy.ts`, plus a direct call from `app/proxies/create-proxy.test.ts`. `codegraph_explore` reports three callers for it; the extra ones are references rather than call sites, so the figure needs checking before it is quoted.

The graph does not settle whether anything consumes the header, because a header is a string-keyed contract rather than a symbol. That was checked separately by searching for the name and for every `next/headers` import; both confirm no reader exists.

- Affected code: `app/proxies/actions.ts`
- Affected specs: `authentication`
- Breaking changes: none for this repository, but real for a fork. Next copies a
  middleware response's headers onto the inbound request headers, so
  `headers().get("x-current-path")` in a server component did resolve. This kit shipped
  the writing half of that widely-published pattern from its first commit; a product that
  added the reading half loses it here, from a diff that looks inert.
- Not included: the `Resource.path` value itself, which `performAction` reads to build the header, stays — the policy uses it.
