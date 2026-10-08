<!-- SPECTRA:START v1.3.0 -->

# Spectra Instructions

This project uses Spectra for Spec-Driven Development(SDD). Specs live in `openspec/specs/`, change proposals in `openspec/changes/`.

## Use `$spectra-*` skills when:

- An explicitly requested Spectra decision needs structure → `$spectra-discuss`
- User explicitly requests a Spectra change proposal → `$spectra-propose`
- Continue tasks for an identified change → `$spectra-apply`
- Update requirements or plans for an identified change → `$spectra-ingest`
- Check implementation matches artifacts → `$spectra-verify`
- Review implementation quality and logic issues → `$spectra-review`
- Analyze artifact consistency before coding → `$spectra-analyze`
- Audit security sharp edges → `$spectra-audit`
- Check stale changes before resuming → `$spectra-drift`
- Debug a concrete bug systematically → `$spectra-debug`
- Implementation is done → `$spectra-archive`
- Commit only files related to a specific change → `$spectra-commit`

Explicit skill invocation takes precedence. Apply existing authorization within its unchanged scope.

## Workflow

discuss? → propose → apply ⇄ ingest → verify / review → archive

- `discuss` is optional — skip if requirements are clear
- Requirements change mid-work? Plan mode (`/plan`) → `ingest` → resume `apply`

## Parked Changes

Changes can be parked（暫存）— temporarily moved out of `openspec/changes/`. Parked changes won't appear in `spectra list` but can be found with `spectra list --parked`. To restore: `spectra unpark <name>`. The `$spectra-apply` and `$spectra-ingest` skills disclose parking and restore when the named operation is already explicitly requested; respect a known refusal, otherwise ask for missing authorization.

<!-- SPECTRA:END -->

# Repository Guidelines

## Project Structure & Module Organization

This is an ASP.NET Core 10 MVC application with Razor views and a Vue 3 interactive report screen. `Program.cs` configures dependency injection and routing. Keep HTTP/page-flow logic in `Controllers/`, business and catalog logic in `Services/`, domain-shaped data in `Models/`, and UI-specific data in `ViewModels/`. Razor pages live under `Views/`; static CSS, JavaScript, images, and vendored libraries belong in `wwwroot/`. The Vue entry point is `wwwroot/js/report-app.js`, and `scripts/copy-vendor.js` copies the pinned Vue runtime into `wwwroot/vendor/`.

Generated directories (`bin/`, `obj/`, `node_modules/`, and `.vs/`) must not be committed.

## Build, Test, and Development Commands

- `npm install` installs the pinned Vue dependency.
- `npm run copy:vendor` refreshes the local Vue runtime used without a CDN.
- `dotnet restore` restores .NET dependencies.
- `dotnet build` compiles the application and reports compiler warnings.
- `dotnet run` starts the site; development profiles use `https://localhost:7153` and `http://localhost:5281`.
- `dotnet test` runs tests once a test project is added. There is currently no automated test project.

Run `npm run copy:vendor` after changing the Vue package version.

## Coding Style & Naming Conventions

Follow `CODING_STANDARDS.md`. Use four-space indentation in C#, file-scoped namespaces, nullable reference types, and implicit usings. Use `PascalCase` for types, methods, properties, and matching C# filenames; prefix interfaces with `I`; use `_camelCase` for private fields and `camelCase` for parameters and locals. Prefer descriptive names over abbreviations. Keep controllers thin and inject services through ASP.NET Core dependency injection.

## Testing Guidelines

For new server-side behavior, add a separate test project such as `OpdAccrRptWeb.Tests/` and name test files after the subject (`ReportCatalogServiceTests.cs`). Use descriptive test methods that state behavior and expected result. At minimum, build the solution and manually verify affected Razor/Vue flows before opening a pull request.

## Global Report UI Conventions

- Report query date fields must use the existing Gregorian HTML date-picker experience (`type="date"`), consistent with established reports such as C10. When a legacy repository or Oracle query requires a ROC date, convert `YYYY-MM-DD` to the required ROC representation explicitly at the request boundary; do not expose ROC text entry in the browser unless a report specification explicitly requires it.
- Pending report queries must use the existing shared table skeleton/shimmer loading pattern. Do not introduce report-specific hourglass, funnel, spinner, or text-only loading indicators unless the user explicitly requests a different loading experience.
- Reuse the shared report page structure, classes, result-list conventions, empty state, total count, and pagination before adding report-specific presentation. Print-preview layouts may remain report-specific and must be entered through an explicit preview/print action when a normal query result list is required.

## Commit & Pull Request Guidelines

Git history is not available in this checkout, so no repository-specific commit convention can be inferred. Use short, imperative subjects such as `Add report date validation`, and keep each commit focused. Pull requests should explain the change, verification performed, and any configuration impact; link relevant issues and include screenshots for visible UI changes. Never commit credentials or database passwords—use environment-specific configuration or the planned infrastructure service.
