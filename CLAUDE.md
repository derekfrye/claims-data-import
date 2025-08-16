# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

This is a .NET 9.0 solution containing five projects:

- **LibClaimsDataImport** - Shared library containing core claims data import functionality
- **CmdClaimsDataImport** - Console application for command-line claims data import operations
- **HtmlClaimsDataImport** - ASP.NET Core web application providing a web-based GUI for claims data import
- **LibClaimsDataImport.Tests** - XUnit test suite for the core library
- **HtmlClaimsDataImport.Tests** - Integration test suite for the web application

## Build and Development Commands

### Building the Solution
```bash
# Standard build (GUI requires MAUI workloads)
dotnet build ClaimsDataImport.sln

# Recommended: Build with warnings as errors for code quality
dotnet build ClaimsDataImport.sln --warnaserror
```

### Building Individual Projects
```bash
# Console application (recommended with --warnaserror)
dotnet build CmdClaimsDataImport/CmdClaimsDataImport.csproj --warnaserror

# Web application (recommended with --warnaserror)
dotnet build HtmlClaimsDataImport/HtmlClaimsDataImport.csproj --warnaserror

# Library (recommended with --warnaserror) 
dotnet build LibClaimsDataImport/LibClaimsDataImport.csproj --warnaserror
```

### Running Applications
```bash
# Console application
dotnet run --project CmdClaimsDataImport

# Web application (starts local web server)
dotnet run --project HtmlClaimsDataImport
```

### Testing
```bash
# Run all tests for the entire solution
dotnet test ClaimsDataImport.sln

# Run library tests only
dotnet test LibClaimsDataImport.Tests/LibClaimsDataImport.Tests.csproj

# Run web application integration tests only
dotnet test HtmlClaimsDataImport.Tests/HtmlClaimsDataImport.Tests.csproj

# Run tests with detailed output
dotnet test ClaimsDataImport.sln --logger "console;verbosity=detailed"
```

### Code Quality Analysis
```bash
# Run Roslynator analysis on all projects
roslynator analyze LibClaimsDataImport/LibClaimsDataImport.csproj
roslynator analyze CmdClaimsDataImport/CmdClaimsDataImport.csproj
roslynator analyze HtmlClaimsDataImport/HtmlClaimsDataImport.csproj
roslynator analyze LibClaimsDataImport.Tests/LibClaimsDataImport.Tests.csproj
roslynator analyze HtmlClaimsDataImport.Tests/HtmlClaimsDataImport.Tests.csproj

# Run analysis on entire solution
roslynator analyze ClaimsDataImport.sln
```

### StyleCop.Analyzers (Future Enhancement)
```bash
# Add StyleCop.Analyzers for comprehensive C# style enforcement (aspiration)
dotnet add LibClaimsDataImport/LibClaimsDataImport.csproj package StyleCop.Analyzers

# Note: Currently produces 180+ violations that need addressing
# Works seamlessly with dotnet build and --warnaserror flag
```

## Architecture Overview

### Core Library (LibClaimsDataImport)
- Full-featured claims data import library with sophisticated CSV parsing and SQLite import capabilities
- Targets .NET 9.0
- Referenced by both console and GUI applications
- Core components:
  - **DataTypeDetector**: Centralized data parsing logic for multiple data types
  - **FileSpec**: CSV analysis and automatic column type detection
  - **File**: Database import functionality with transaction support
  - **ImportConfig**: JSON-based configuration management system

### Console Application (CmdClaimsDataImport) 
- Entry point: `Program.cs` with sophisticated argument parsing and error handling
- Components:
  - **ArgumentParser**: Command-line argument processing
  - **ImportProcessor**: End-to-end import orchestration
- References the core library for shared functionality
- Suitable for batch processing and automation scenarios

### Web Application (HtmlClaimsDataImport)
- ASP.NET Core web application providing browser-based GUI for claims data import
- Entry point: `Program.cs:1` - ASP.NET Core initialization with custom temp directory support
- Main UI: `Pages/ClaimsDataImporter.cshtml` with file upload and import functionality
- Features:
  - File upload support for CSV, JSON config, and SQLite database files
  - Session-based temporary file management with automatic cleanup
  - Integration with core library for claims data processing
  - Self-contained deployment configuration for easy distribution

## Implementation Details

### Core Library Features
- **FileSpec Class**: Analyzes CSV files to detect column types automatically
  - Supports SQL Server Money format detection ($1,234.56)
  - Detects integers (int/long) and decimals
  - Defaults to string for mixed or unrecognized data
  - Located in: `LibClaimsDataImport/Importer/FileSpec.cs`

- **File Class**: Handles CSV import to SQLite database
  - Validates database and table existence before import
  - Uses parameterized queries to prevent SQL injection
  - Supports transactional imports with rollback on failure
  - Located in: `LibClaimsDataImport/Importer/File.cs`

### Console Application Usage
```bash
CmdClaimsDataImport --database <path> --table <name> --filename <path> [--config <path>]
```

Example:
```bash
CmdClaimsDataImport --database claims.db --table claims_data --filename data.csv
CmdClaimsDataImport --database claims.db --table claims_data --filename data.csv --config custom_config.json
```

### Dependencies
- **Sylvan.Data.Csv**: High-performance CSV reading with automatic delimiter detection
- **Microsoft.Data.Sqlite**: SQLite database connectivity for .NET
- **System.Text.Json**: JSON configuration file parsing and serialization

## Development Notes

- All projects use .NET 9.0 with nullable reference types enabled
- Web application includes self-contained deployment configuration for easy distribution
- Library uses streaming approach - doesn't load entire CSV into memory
- Column type detection prioritizes: Money → DateTime → Integer → Decimal → String
- Comprehensive test suite using XUnit with multiple test suites:
  - **LibClaimsDataImport.Tests**: Core library unit tests
    - **DateParseTest.cs**: DateTime parsing with exact value validation, invalid dates, edge cases, leap years, multiple format variations (ISO 8601, US, European, natural language)
    - **MoneyParseTest.cs**: Money parsing with standard formats ($1,234.56), negative values in parentheses, whitespace handling, unusual but valid formats, edge cases, invalid formats
    - **IntegrationTest.cs** & **IntTest2.cs**: Full end-to-end CSV import validation tests
  - **HtmlClaimsDataImport.Tests**: Web application integration tests
    - **FileSelectionIntegrationTests.cs**: Tests for web-based file upload and selection functionality
- Database table must exist before import (library doesn't create tables)
- All projects maintain zero Roslynator code analysis issues for consistent code quality

## Code Quality Standards

This codebase maintains high code quality standards:
- **Zero Roslynator diagnostics** across all projects
- **Zero build warnings** - all builds use `--warnaserror` flag to treat warnings as errors
- Performance optimizations using `AsSpan()` instead of `Substring()` for string operations
- Proper use of `TryGetValue()` pattern for dictionary access to avoid double lookups
- Static method declarations where appropriate to improve performance
- Cached `JsonSerializerOptions` instances to avoid repeated allocations
- Comprehensive test coverage ensuring reliability across all components

### Quality Enforcement
- All builds should use `--warnaserror` flag to maintain zero-warning codebase
- Roslynator analysis ensures adherence to best practices and performance guidelines
- Tests must pass before committing changes

### StyleCop.Analyzers Configuration

The web application (HtmlClaimsDataImport) includes StyleCop.Analyzers with the following suppressions:

**Suppressed Rules:**
- **SA1633**: File header copyright text - Not required for internal web application files
- **SA1513**: Closing brace followed by blank line - Conflicts with modern C# formatting preferences
- **SA0001**: XML comment analysis disabled - Reduces noise from documentation analyzer
- **CS1591**: Missing XML comment for publicly visible members - Web app internal classes don't require public API documentation
- **SA1600**: Elements should be documented - Same rationale as CS1591 for internal web application
- **SA1515**: Single-line comment preceded by blank line - Conflicts with natural commenting style
- **SA1502**: Element should not be on single line - Allows concise property declarations
- **SA1505**: Opening braces not followed by blank line - Modern C# style preference
- **SA1508**: Closing braces not preceded by blank line - Modern C# style preference  
- **SA1201**: Elements should appear in correct order - Allows logical grouping over strict ordering

**Active Rules:**
- All other StyleCop rules are active and enforced

### Future Code Quality Goals
- **Consistent formatting**: Continue refining StyleCop configuration based on team preferences
- **Selective enforcement**: Enable additional rules as codebase matures and patterns stabilize