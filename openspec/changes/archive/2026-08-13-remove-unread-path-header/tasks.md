# Tasks

## 1. Remove the header

- [x] 1.1 Stop `performAction` setting `x-current-path` on an allowed response.
      Done when an allowed request's response carries no header naming the resolved
      path, asserted by a test that fails if the header is reintroduced.
- [x] 1.2 Keep the allow path otherwise unchanged — still `NextResponse.next()`, still
      passing the request through. Done when the existing proxy tests pass untouched,
      including the API bypass, redirect and fail-closed cases.

## 2. Confirm nothing depended on it

- [x] 2.1 Verify no reader exists anywhere in the repository, including dynamic header
      access. Done when a search for the header name returns only the test that asserts
      its absence, and every `next/headers` consumer is accounted for.
