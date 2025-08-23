# HtmlClaimsDataImport — CQRS Violations (Ranked)

This document tracks current CQRS design deviations in the `HtmlClaimsDataImport` web app so we can remediate them incrementally. Items are ordered from most to least serious based on architectural impact and coupling risk.

## 1) Application depends on Infrastructure (most serious)
- Location: `Application/Handlers/UploadFileCommandHandler.cs`
- Issue: The handler references `Infrastructure.Services.TempDirectoryCleanupService` for temp-dir base path validation.
- Why it’s a violation: Application layer must be independent of Infrastructure; referencing infrastructure types in a handler inverts dependency direction.
- Risk: Tight coupling blocks reuse/testing and complicates future infrastructure changes.
- Suggested fix: Introduce an Application-level abstraction (e.g., `ITempDirectoryPolicy` or extend `ITempDirectoryService` with `ResolveValidatedTempDir(...)`) and implement it in Infrastructure. Remove all infrastructure references from the handler.

## 1a) Fix implemented: Remove Application → Infrastructure dependency
- Summary: Moved the temp directory interface into Application and encapsulated tmpdir validation/resolution behind that interface. The handler no longer references infrastructure types.
- Files added: `HtmlClaimsDataImport/Application/Interfaces/ITempDirectoryService.cs` (now includes `ResolveUploadTempDirectory(string?)`).
- Files updated:
  - `HtmlClaimsDataImport/Application/Handlers/UploadFileCommandHandler.cs` → uses `ResolveUploadTempDirectory` and no longer references `TempDirectoryCleanupService`.
  - `HtmlClaimsDataImport/Infrastructure/Services/TempDirectoryService.cs` → implements the new Application interface and provides validation against the base path; ensures directory exists and registers it.
  - `HtmlClaimsDataImport/Infrastructure/Services/TempDirectoryCleanupService.cs` → references the Application interface type for registrations.
  - `HtmlClaimsDataImport/Pages/ClaimsDataImporter.cshtml.cs` and `HtmlClaimsDataImport/Program.cs` → updated usings to reference the Application interface where needed.
- Files removed: `HtmlClaimsDataImport/Infrastructure/Services/ITempDirectoryService.cs` (interface relocated to Application).
- Behavior impact: No functional change; validation is now encapsulated in the implementation. Layering is corrected.

## 2) Query returns UI model instead of Application DTO
- Location: `Application/Interfaces/IPreviewService.cs`, `Application/Queries/GetPreviewDataQuery.cs`, `Infrastructure/Services/PreviewService.cs`
- Issue: Query handler returns `HtmlClaimsDataImport.Models.PreviewDataModel`, a UI view model defined in the web project.
- Why it’s a violation: Queries should return application read models/DTOs, not presentation-specific models.
- Risk: Coupling of Application to Presentation prevents reuse and complicates testing.
- Suggested fix: Create an Application DTO (e.g., `PreviewDataDto` under `Application/Queries/Dto`). Change `IPreviewService` and the query to use that DTO; map to `PreviewDataModel` in the Razor layer.

## 2a) Fix implemented: Application DTO for preview query
- Summary: Replaced UI `PreviewDataModel` usage in the Application/Infrastructure layers with an Application DTO and mapped it in the Razor Page.
- Files added: `HtmlClaimsDataImport/Application/Queries/Dtos/PreviewDataDto.cs`.
- Files updated:
  - `HtmlClaimsDataImport/Application/Interfaces/IPreviewService.cs` → returns `Task<PreviewDataDto>`.
  - `HtmlClaimsDataImport/Application/Queries/GetPreviewDataQuery.cs` → `IRequest<PreviewDataDto>`.
  - `HtmlClaimsDataImport/Application/Handlers/GetPreviewDataQueryHandler.cs` → handles and returns `PreviewDataDto`.
  - `HtmlClaimsDataImport/Infrastructure/Services/PreviewService.cs` → constructs and returns `PreviewDataDto` (no UI model references).
  - `HtmlClaimsDataImport/Pages/ClaimsDataImporter.cshtml.cs` → maps `PreviewDataDto` to existing UI `PreviewDataModel` before rendering.
- Behavior impact: No functional change; views remain unchanged. Improves layering and testability.
- Build status: Solution builds cleanly (0 warnings, 0 errors).

## 3) ASP.NET types leak into Application contracts
- Location: `Application/Interfaces/IFileUploadService.cs` (uses `IFormFile`), `Application/Commands/UploadFileCommand.cs` (takes `IFormFile`).
- Issue: Application interfaces and commands depend on ASP.NET abstractions.
- Why it’s a violation: Application should be web-agnostic; use stream/byte array + metadata instead.
- Risk: Hinders non-web callers and unit testing without ASP.NET.
- Suggested fix: Redefine contracts to accept `Stream content, string fileName, long length, string contentType` (or similar). Adapt from `IFormFile` in the PageModel before calling the mediator.

## 4) Presentation logic in Infrastructure service
- Location: `Infrastructure/Services/FileUploadService.cs` — `GenerateFileStatusResponse(...)` returns HTML.
- Issue: Service mixes UI generation with application/infrastructure logic.
- Why it’s a violation: Breaks separation of concerns; services shouldn't emit HTML.
- Risk: Encourages further leakage of UI into lower layers and complicates testing.
- Suggested fix: Remove or move HTML generation into Razor/UI helpers. Keep `FileUploadService` focused on file operations and metadata.

## 5) Commands return presentation strings instead of result objects
- Location: `Application/Handlers/LoadDataCommandHandler.cs`, `Infrastructure/Services/DataImportService.cs`.
- Issue: Handlers return free-form strings like "json invalid: …" or "Import failed: …".
- Why it’s a violation: Commands should return structured result types (e.g., success flag, data, error list). UI should format messages.
- Risk: Makes error handling brittle and UI-specific messages leak into Application.
- Suggested fix: Introduce `LoadDataResult { bool Success; string? ImportTable; string? Error; }` (or equivalent). Return that from handler/service and let the PageModel map to user-facing strings.

## 5a) Fix implemented: Structured result for LoadData
- Summary: Replaced string returns with a structured `LoadDataResult` in the Application layer. The Razor Page maps the result to user-facing text, preserving current UI behavior.
- Files added: `HtmlClaimsDataImport/Application/Commands/Results/LoadDataResult.cs`.
- Files updated:
  - `HtmlClaimsDataImport/Application/Interfaces/IDataImportService.cs` → `ProcessFileImport` returns `LoadDataResult`.
  - `HtmlClaimsDataImport/Infrastructure/Services/DataImportService.cs` → returns `LoadDataResult.Ok/Fail` with `ImportTableName` and status.
  - `HtmlClaimsDataImport/Application/Commands/LoadDataCommand.cs` → `IRequest<LoadDataResult>`.
  - `HtmlClaimsDataImport/Application/Handlers/LoadDataCommandHandler.cs` → returns `LoadDataResult` for validations/errors/success.
  - `HtmlClaimsDataImport/Pages/ClaimsDataImporter.cshtml.cs` → returns JSON `{ success, importTableName, statusMessage }`.
  - `HtmlClaimsDataImport/wwwroot/js/dataLoading.js` → expects JSON and renders `statusMessage` client-side.
  - Logging moved to `ILogger` in handler/service (no `Console.WriteLine`).
- Behavior impact: Client now receives JSON; UI renders the message. Tests updated to assert `success` and `importTableName` in addition to `statusMessage`.

## 6) UI action bypasses mediator for stateful workflow
- Location: `Pages/ClaimsDataImporter.cshtml.cs` — `OnPostFileSelected` updates state and returns HTML without a command.
- Issue: Potentially stateful workflow step is handled in UI code only.
- Why it’s a violation: If this step is purely presentation (selection UI), it’s acceptable; if it carries domain significance, it should be modeled as a command.
- Risk: Domain state drift between requests; logic becomes hard to test.
- Suggested fix: If selection affects domain/session state, introduce a command (e.g., `SelectFileCommand`). Otherwise, document that it’s a pure UI concern.

---

## Remediation Plan (high-level)
- Short term: Address (1), (2), (3) to restore clean layering; remove HTML from services (4).
- Medium term: Introduce structured results for commands (5).
- Optional: Normalize UI-only actions vs. domain-significant actions and add commands where warranted (6).

## Notes
- Positives: Commands and queries are separated via MediatR; query side (`PreviewService`) appears read-only; handlers encapsulate mutations.
- Scope: This list covers the HtmlClaimsDataImport web app; the core library (`LibClaimsDataImport`) wasn’t evaluated here for CQRS boundaries.
