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
The solution currently does not contain test projects. Test commands should be added when tests are implemented.

## Architecture Overview

### Core Library (LibClaimsDataImport)
- Currently contains a placeholder `Class1` - this is where the main claims data import logic should be implemented
- Targets .NET 9.0
- Referenced by both console and GUI applications

### Console Application (CmdClaimsDataImport) 
- Entry point: `Program.cs:3` - simple "Hello, World" implementation
- References the core library for shared functionality
- Suitable for batch processing and automation scenarios

### GUI Application (GuiClaimsDataImport)
- MAUI cross-platform application targeting Android, iOS, macOS, and Windows
- Entry point: `MauiProgram.cs:7` - standard MAUI app initialization
- Main UI: `MainPage.xaml` with basic counter functionality as template
- Currently uses default MAUI template structure - needs customization for claims import functionality

## Development Notes

- All projects use .NET 9.0 with nullable reference types enabled
- GUI project supports multiple platforms but may require specific SDKs for deployment
- The solution structure suggests this is designed for importing claims data, but the actual import logic is not yet implemented
- Both applications currently contain placeholder/template code that should be replaced with actual functionality