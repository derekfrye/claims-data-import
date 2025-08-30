# HtmlClaimsDataImport — Local Setup

[![CI](https://github.com/derekfrye/claims-data-import/actions/workflows/ci.yml/badge.svg)](https://github.com/derekfrye/claims-data-import/actions/workflows/ci.yml)

Follow these steps to run the ASP.NET Core app locally for testing.

## Prerequisites
- .NET SDK 9.0 installed (`dotnet --version` shows 9.x).
- Optional: VS Code or Visual Studio for debugging.

## Get the Code
- Clone and open the repo root (contains `ClaimsDataImport.sln`).

## Restore and Build
- Restore/build all projects: `dotnet build ClaimsDataImport.sln`

## Run the Web App
- Use Development settings and run:
  - macOS/Linux: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project HtmlClaimsDataImport`
  - Windows (PowerShell): `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project HtmlClaimsDataImport`
- The console prints the URLs (typically `https://localhost:5001` and `http://localhost:5000`). Open the HTTPS address in your browser.

## Data & Configuration
- SQLite file `default.sqlite3.db` and `default.json` are copied to the output at build; they are safe to reset for local testing.
- Override settings in `HtmlClaimsDataImport/appsettings.Development.json` (never commit secrets).

## Verify
- Load the site, exercise file upload and import flows. Client scripts are served from `wwwroot/js` (no separate build step needed).

## Troubleshooting
- Port in use: set a custom port, e.g. `ASPNETCORE_URLS=http://localhost:5080 dotnet run --project HtmlClaimsDataImport`.
- Certificate warnings: re-run `dotnet dev-certs https --trust`.
- Sanity check: run tests with `dotnet test`.

## Licenses
- Project license: see `LICENSE` (BSD 2-Clause) in the repo root.
- Third-party notices: see `THIRD-PARTY-NOTICES.md` in the repo root.
