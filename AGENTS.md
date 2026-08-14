<!-- SPECTRA:START v1.0.2 -->

# Spectra Instructions

This project uses Spectra for Spec-Driven Development(SDD). Specs live in `openspec/specs/`, change proposals in `openspec/changes/`.

## Use `$spectra-*` skills when:

- A discussion needs structure before coding → `$spectra-discuss`
- User wants to plan, propose, or design a change → `$spectra-propose`
- Tasks are ready to implement → `$spectra-apply`
- There's an in-progress change to continue → `$spectra-ingest`
- User asks about specs or how something works → `$spectra-ask`
- Implementation is done → `$spectra-archive`
- Commit only files related to a specific change → `$spectra-commit`

## Workflow

discuss? → propose → apply ⇄ ingest → archive

- `discuss` is optional — skip if requirements are clear
- Requirements change mid-work? `ingest` → resume `apply`

## Parked Changes

Changes can be parked（暫存）— temporarily moved out of `openspec/changes/`. Parked changes won't appear in `spectra list` but can be found with `spectra list --parked`. To restore: `spectra unpark <name>`. The `$spectra-apply` and `$spectra-ingest` skills handle parked changes automatically.

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

## Commit & Pull Request Guidelines

Git history is not available in this checkout, so no repository-specific commit convention can be inferred. Use short, imperative subjects such as `Add report date validation`, and keep each commit focused. Pull requests should explain the change, verification performed, and any configuration impact; link relevant issues and include screenshots for visible UI changes. Never commit credentials or database passwords—use environment-specific configuration or the planned infrastructure service.
