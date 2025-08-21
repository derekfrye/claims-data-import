# Repository Guidelines

## Project Structure & Module Organization
- `HtmlClaimsDataImport/`: ASP.NET Core web app (static assets in `wwwroot/`, config in `appsettings*.json`).
- `LibClaimsDataImport/`: core import library and `Importer/ClaimsDataImportConfig.json`.
- `CmdClaimsDataImport/`: console runner for import workflows.
- `HtmlClaimsDataImport.Tests/`, `LibClaimsDataImport.Tests/`: xUnit test projects.
- `ClaimsDataImport.sln`: solution entry point.

## Build, Test, and Development Commands
- Build all: `dotnet build ClaimsDataImport.sln` — restores and compiles all projects.
- Run web app: `dotnet run --project HtmlClaimsDataImport` — serves UI (use `ASPNETCORE_ENVIRONMENT=Development` locally).
- Run console: `dotnet run --project CmdClaimsDataImport -- <args>` — executes CLI import flows.
- Test all: `dotnet test` — runs unit/integration tests.
- Coverage: `dotnet test --collect:"XPlat Code Coverage"` — emits coverage via coverlet.

## Coding Style & Naming Conventions
- Language: C# on .NET 9.0 with `<Nullable>enable</Nullable>` and implicit usings.
- Analyzers: StyleCop enabled (see `HtmlClaimsDataImport/stylecop.json`). Fix warnings; file names should match top-level types.
- Indentation: 4 spaces; braces on new lines; `using` directives outside namespaces.
- Naming: PascalCase for types/methods; camelCase for locals/params.
- JS assets in `wwwroot/js/`: keep file names lowerCamelCase (e.g., `fileUpload.js`).

## Testing Guidelines
- Framework: xUnit; additional packages include `Microsoft.AspNetCore.Mvc.Testing` for web integration tests.
- Locations: add tests under the corresponding `*.Tests` project.
- Conventions: name files by feature, e.g., `Feature_Tests.cs`; integration tests may use `*_IntTest.cs`.
- Run focused tests: `dotnet test --filter FullyQualifiedName~Namespace.Type.Method`.

## Commit & Pull Request Guidelines
- Commits: imperative mood and scoped subject (e.g., "web: fix file upload mapping"). Provide context in the body and link issues (e.g., `Fixes #123`).
- PRs: clear description, steps to reproduce/verify, screenshots for UI, and updated docs when behavior changes. Ensure CI builds and tests pass.

## Security & Configuration Tips
- Configuration: prefer environment variables or user-secrets over committing secrets; local settings in `appsettings.Development.json`.
- Data: `default.sqlite3.db` is copied at build for dev/testing; treat as disposable.
