# Change: Add Comprehensive API Documentation

## Why

The API needs consistent XML comments so developers can understand public contracts through IDE IntelliSense and generated OpenAPI/Scalar documentation. This is especially important for a starter kit because downstream forks should inherit clear examples for endpoints, handlers, DTOs, errors, and infrastructure types.

## What Changes

- Enable XML documentation generation for the API project.
- Document public handlers, request/response DTOs, models, constants, middleware, and infrastructure interfaces.
- Standardize XML comment conventions for summaries, remarks, parameters, returns, and exceptions.
- Provide reusable templates for command handlers, query handlers, endpoint modules, DTOs, and exceptions.
- Add a reviewer checklist so future forks keep generated documentation useful.

## Impact

- Affected specs: `documentation`
- Affected code: `Api.csproj`, public API types, XML comments, API documentation guides
- Runtime behavior: none
- Breaking changes: none
