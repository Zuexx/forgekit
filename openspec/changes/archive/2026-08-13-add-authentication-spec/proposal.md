# Change: Specify Authentication

## Why

Authentication has no specification. Sign-in, registration, session handling, and route authorization are implemented and now tested, but nothing states what they are supposed to do.

That gap had consequences. This area accumulated five defects that shipped unnoticed, and in each case there was no written expectation to check an implementation against:

- the database adapter was passed in a shape that made every auth query throw, so credential authentication never worked at all
- the proxy's action handler fell through to allow, so a deny decision would have granted access
- the API bypass matched on a prefix rather than a segment boundary, so a page such as `/apidocs` skipped policy entirely
- the session cookie name was written out by hand in one place and derived from a constant in another, so renaming the app would have signed everyone out
- registration was unreachable: its client schema stacked two contradictory rules and rejected every input, behind a submit handler that only logged

The behavior is now verified end to end against a real database. This change records it, so the next change to this area has something to be checked against, and `archive` guidance to verify implementations against spec deltas becomes executable here rather than vacuous.

## What Changes

- Add an `authentication` capability covering credential registration and sign-in, session persistence, route authorization, and the configuration boundaries that keep a fork safe.
- No code changes. Every requirement describes behavior that exists and is covered by tests.

## Impact

- Affected specs: `authentication` (new)
- Affected code: none
- Breaking changes: none
