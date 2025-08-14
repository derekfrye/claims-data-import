# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Structure

This is a .NET 9.0 solution containing three projects:

- **LibClaimsDataImport** - Shared library containing core claims data import functionality
- **CmdClaimsDataImport** - Console application for command-line claims data import operations
- **GuiClaimsDataImport** - Cross-platform MAUI application providing a GUI for claims data import

## Build and Development Commands

### Building the Solution
```bash
dotnet build ClaimsDataImport.sln
```

### Building Individual Projects
```bash
# Console application
dotnet build CmdClaimsDataImport/CmdClaimsDataImport.csproj

# GUI application  
dotnet build GuiClaimsDataImport/GuiClaimsDataImport.csproj

# Library
dotnet build LibClaimsDataImport/LibClaimsDataImport.csproj
```

### Running Applications
```bash
# Console application
dotnet run --project CmdClaimsDataImport

# GUI application (requires appropriate platform)
dotnet run --project GuiClaimsDataImport
```

### Testing
```bash
# Run all tests
dotnet test LibClaimsDataImport.Tests/LibClaimsDataImport.Tests.csproj

# Run tests with detailed output
dotnet test LibClaimsDataImport.Tests/LibClaimsDataImport.Tests.csproj --logger "console;verbosity=detailed"
```

### Code Quality Analysis
```bash
# Run Roslynator analysis on all projects
roslynator analyze LibClaimsDataImport/LibClaimsDataImport.csproj
roslynator analyze CmdClaimsDataImport/CmdClaimsDataImport.csproj
roslynator analyze LibClaimsDataImport.Tests/LibClaimsDataImport.Tests.csproj

# Run analysis on entire solution
roslynator analyze ClaimsDataImport.sln
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

### GUI Application (GuiClaimsDataImport)
- MAUI cross-platform application targeting Android, iOS, macOS, and Windows
- Entry point: `MauiProgram.cs:7` - standard MAUI app initialization
- Main UI: `MainPage.xaml` with basic counter functionality as template
- Currently uses default MAUI template structure - needs customization for claims import functionality

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
- GUI project supports multiple platforms but may require Android/iOS SDKs for full compilation
- Library uses streaming approach - doesn't load entire CSV into memory
- Column type detection prioritizes: Money → DateTime → Integer → Decimal → String
- Comprehensive test suite using XUnit with multiple test files:
  - **DateParseTest.cs**: DateTime parsing with exact value validation, invalid dates, edge cases, leap years, multiple format variations (ISO 8601, US, European, natural language)
  - **MoneyParseTest.cs**: Money parsing with standard formats ($1,234.56), negative values in parentheses, whitespace handling, unusual but valid formats, edge cases, invalid formats
  - **IntegrationTest.cs** & **IntTest2.cs**: Full end-to-end CSV import validation tests
- Database table must exist before import (library doesn't create tables)
- All projects maintain zero Roslynator code analysis issues for consistent code quality

## Code Quality Standards

This codebase maintains high code quality standards:
- **Zero Roslynator diagnostics** across all projects
- Performance optimizations using `AsSpan()` instead of `Substring()` for string operations
- Proper use of `TryGetValue()` pattern for dictionary access to avoid double lookups
- Static method declarations where appropriate to improve performance
- Cached `JsonSerializerOptions` instances to avoid repeated allocations
- Comprehensive test coverage ensuring reliability across all components