# Contributing to ForgeKit

Thank you for your interest in contributing! This guide will help you get started.

## Code of Conduct

Be respectful and professional. We're building great things together.

## Getting Started

1. **Fork** the repository (if external contributor)
2. **Clone** your fork or the main repo
3. **Create a feature branch** from `main`:
   ```bash
   git checkout -b feature/your-feature-name
   # or
   git checkout -b fix/your-bug-fix
   ```

## Before You Start

### For Backend (API) Changes
- Read [api/docs/USER_GUIDE.md](api/docs/USER_GUIDE.md)
- Read [api/docs/EXTENDING_THE_API.md](api/docs/EXTENDING_THE_API.md)
- Review relevant [ADRs](api/docs/adr/)
- Check [openspec/AGENTS.md](openspec/AGENTS.md) for spec-driven development

### For Frontend (App) Changes
- Review [app/README.md](app/README.md)
- Check component structure in `app/components/`
- Understand the state management setup

### For Major Changes
Follow the **spec-driven development** process:
1. Read [openspec/AGENTS.md](openspec/AGENTS.md)
2. Create a proposal in `openspec/changes/`
3. Get approval before implementation
4. Update specs after merging

## Development Workflow

### API Development

```bash
cd api

# Restore and build
dotnet restore
dotnet build

# Run
dotnet run --project ForgeKit.Api

# Test
dotnet test

# Format code
dotnet format
```

API runs on `https://localhost:5000`

### App Development

```bash
cd app

# Install dependencies
pnpm install

# Run dev server
pnpm dev

# Test
pnpm test

# Lint
pnpm lint

# Format
pnpm format
```

App runs on `http://localhost:3000`

## Commit Messages

Follow conventional commits:

```
feat: add user authentication
fix: correct validation error message
docs: update API documentation
style: format code
refactor: simplify error handling
test: add unit tests for service
chore: update dependencies
```

Include **Co-authored-by** trailer:
```
feat: add new feature

Description of the change...

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

## Pull Request Process

1. **Update docs** if needed (especially for API/architecture changes)
2. **Run tests locally**:
   ```bash
   # API
   cd api && dotnet test
   
   # App
   cd app && pnpm test
   ```
3. **Push your branch** and create a PR
4. **Describe the change** clearly:
   - What problem does it solve?
   - How was it tested?
   - Any breaking changes?
5. **Link to related issues** if applicable
6. **Wait for review** and address feedback

## Testing Requirements

### API
- All public methods should have unit tests
- Integration tests for complex flows
- Run: `dotnet test`

### App
- Components should have tests
- Critical paths must be tested
- Run: `pnpm test`

## Code Style

### C# (Backend)
- PascalCase for public types/members
- camelCase for local variables
- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `dotnet format` to auto-format
- Nullable reference types enabled

### TypeScript (Frontend)
- Follow ESLint rules
- Use Prettier for formatting
- camelCase for variables and functions
- PascalCase for components and types
- Run `pnpm lint` and `pnpm format`

## Documentation

- **API changes?** → Update [api/docs/](api/docs/)
- **Architecture change?** → Create ADR in [api/docs/adr/](api/docs/adr/) or update [STRUCTURE.md](STRUCTURE.md)
- **New feature?** → Document in relevant README
- **Breaking changes?** → Update CONTRIBUTING guide and migration notes

## Troubleshooting

### API won't build?
```bash
cd api
dotnet clean
dotnet restore
dotnet build
```

### App won't start?
```bash
cd app
rm -rf node_modules .next
pnpm install
pnpm dev
```

### Tests failing?
- Check database connection (SQL Server running?)
- Verify environment variables are set
- Run `dotnet test --verbosity detailed` for more info

## Questions?

- Check existing [documentation](./docs/)
- Review [openspec/](./openspec/) for architectural decisions
- Look at similar code in the codebase
- Open an issue or discussion

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.

---

**Thank you for contributing! 🙏**
