# 727 INS Manager

A Windows desktop utility for managing the FlightSim Studio Boeing 727 CIVA INS in Microsoft Flight Simulator.

## Status

Initial UI and development environment scaffold. Simulator integration is not implemented yet.

## Prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- Python 3.10 or newer for pre-commit

## Development setup

```powershell
.\scripts\setup.ps1
dotnet restore
dotnet build
dotnet test
dotnet run --project .\src\InsManager.App
```

The repository uses pre-commit for whitespace, YAML, merge-conflict, and .NET formatting checks. Tests run on the pre-push hook.
