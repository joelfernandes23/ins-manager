# 727 INS Manager

A Windows desktop utility for managing the FlightSim Studio Boeing 727 CIVA INS in Microsoft Flight Simulator.

## Status

Initial UI and development environment scaffold. Simulator integration is not implemented yet.

The current application runs in mock mode: it can simulate a connection and load a sample route into the ten-slot INS display without sending commands to MSFS.

## Architecture

- `InsManager.App` — WPF views, MVVM view models, and application composition
- `InsManager.Core` — simulator-independent route and INS domain contracts
- `InsManager.Infrastructure` — SimBrief, settings, and persistence adapters
- `InsManager.SimConnect` — MSFS connectivity and aircraft-specific integration
- `InsManager.Tests` — automated domain and adapter tests

## Prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- Python 3.10 or newer for pre-commit

## Development setup

```powershell
.\scripts\setup.ps1
.\dev.ps1 run
```

Common development commands:

```powershell
.\dev.ps1 setup
.\dev.ps1 build
.\dev.ps1 test
.\dev.ps1 format
.\dev.ps1 check
```

The repository uses pre-commit for whitespace, YAML, merge-conflict, and .NET formatting checks. Tests run on the pre-push hook.
