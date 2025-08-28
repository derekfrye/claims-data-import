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
 - Tip: if a focused test run shows no failure details, run `dotnet clean` before `dotnet test` to surface full diagnostics.

### Style/Analyzer Build + Summary
- CLI: `ProjectStyleBuilder` (C# console app)
  - Cleans and builds, saves logs to `build_artifacts/`, and prints enriched summaries of diagnostics (includes rule messages and optional help URLs).
  - Flags:
    - `-w, --warnaserror`: treat warnings as errors during build
    - `-s, --solution <solution.sln|project.csproj>`: scope to solution or single project (default: `ClaimsDataImport.sln`)
    - `-p, --project <project.csproj>`: per-project top-rules breakdown (match by basename)
    - `-u, --include-urls`: include rule help URLs in summaries
  - Outputs (latest copies are maintained alongside timestamped logs):
    - `build_artifacts/clean.log`, `build_artifacts/build.log`
    - `build_artifacts/warnings_by_id.txt` (count + rule + message), `build_artifacts/warnings_by_project.txt`, `build_artifacts/warnings_by_id_for_project.txt`
    - `build_artifacts/diagnostics_samples.txt`
    - `build_artifacts/warnings_messages.tsv` and `warnings_messages_short.tsv` (rule → message, with/without URLs)
  - Examples:
    - Whole solution: `dotnet run --project ProjectStyleBuilder -- -w`
    - Lib only: `dotnet run --project ProjectStyleBuilder -- -w -s LibClaimsDataImport/LibClaimsDataImport.csproj`
    - Web + per-project breakdown: `dotnet run --project ProjectStyleBuilder -- -w -s HtmlClaimsDataImport/HtmlClaimsDataImport.csproj -p HtmlClaimsDataImport.csproj`

## Coding Style & Naming Conventions
- Language: C# on .NET 9.0 with `<Nullable>enable</Nullable>` and implicit usings.
- Analyzers: Meziantou.Analyzer enabled. Fix warnings; keep file names matching top-level types as a convention.
- Indentation: 4 spaces; braces on new lines; `using` directives outside namespaces.
- Naming: PascalCase for types/methods; camelCase for locals/params.
- JS assets in `wwwroot/js/`: keep file names lowerCamelCase (e.g., `fileUpload.js`).

### Mediator/CQRS
- Library: `Mediator` v3 (martinothamar). Patterns: `ICommand<T>`/`IQuery<T>` with handlers returning `ValueTask<T>`.
- Packages:
  - Web app (`HtmlClaimsDataImport`): `Mediator.SourceGenerator` (PrivateAssets=all) and `Mediator.Abstractions`.
  - Libraries/handlers: `Mediator.Abstractions` only. Do not add the source generator outside the edge app.
- DI: call `builder.Services.AddMediator()` in `Program.cs`. Assembly options set via `[assembly: MediatorOptions(ServiceLifetime = ServiceLifetime.Scoped)]`.
- Do not reference MediatR; we’ve migrated off it.

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
