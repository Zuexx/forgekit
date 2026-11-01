# Sample resources and API reference

`/v1/resources` is the sample module that shows the Result pattern end to end: list, get by
id, and create, with FluentValidation failures mapped to `422`. In Development the API
reference is served by Scalar from the generated OpenAPI document.

## Sub-features

- `resources-list` returns the fixed sample list.
- `resources-get` returns a resource by id.
- `resources-create` returns `201` with the created resource.
- `resources-create-invalid` returns `422` with per-field errors in the standard error shape.
- `api-reference` serves `/scalar/v1` and its document at `/openapi/v1.json`.

## How to get to it (user POV)

- An API client calls `GET` or `POST $VF_API_URL/v1/resources`.
- A developer opens `$VF_API_URL/scalar/v1` in a browser.

## Driving it with curl

Preconditions:

- Baseline from `README.md`. The API runs with `ASPNETCORE_ENVIRONMENT=Development`; sample
  modules and Scalar are not mapped in Production.

Steps:

- **Create.** `curl -s -w '\nHTTP %{http_code}\n' -X POST "$VF_API_URL/v1/resources" -H 'content-type: application/json' -d '{"name":"Verify widget"}'`.
  Expect `HTTP 201` with an `id`.
- **Invalid.** The same with `{"name":""}`. Expect `HTTP 422`, `"code":"VALIDATION_ERROR"`,
  and `errors.Name` listing the failed rules.
- **List and get.** `GET /v1/resources` gives `HTTP 200` and a JSON array; `GET /v1/resources/1`
  gives `HTTP 200`.
- **Reference.** `GET /openapi/v1.json` must be `HTTP 200` JSON. Then open `/scalar/v1` with
  Playwright and screenshot the rendered operations list.
- Save every response under `$VF_EVIDENCE/api/`.

## Gotchas

- The sample handlers are stateless: a created resource is **not** stored and does not appear
  in the list, and there is no table to query. Do not report persistence for them.
- `GET /v1/resources/<any id>` returns `200` with a fabricated resource; there is no 404 path
  to prove without changing the sample.
- **Known failure, 2026-11-01:** `GET /openapi/v1.json` returns `500`
  (`MissingMethodException` on `Microsoft.OpenApi.OpenApiResponse.get_Content()`).
  `Microsoft.AspNetCore.OpenApi` 10.0.14 is built against `Microsoft.OpenApi` 2.x while the
  repo pins 3.x (`docs/DEPENDENCY_CONSTRAINTS.md`). `/scalar/v1` still returns its HTML shell,
  so a status check on that page alone passes while the reference is empty.
- A `422` is logged at `ERR` as "Unhandled exception" by the exception middleware. That is the
  current design, not a failure of the run.
