# INS Manager

A Windows desktop utility for managing classic inertial navigation systems in Microsoft Flight Simulator.

## Status

The application currently supports the FlightSim Studio Boeing 727 in Microsoft Flight Simulator 2024. It downloads real flight plans from SimBrief and connects to the simulator through SimConnect.

Waypoint writes remain disabled until the FSS 727 input-event mapping has been verified. Connecting with the aircraft loaded exports the available controls to `%LocalAppData%\INS Manager\diagnostics\msfs2024-input-events.json`.

## Architecture

- `InsManager.App` — WPF views, MVVM view models, and application composition
- `InsManager.Core` — simulator-independent route and INS domain contracts
- `InsManager.Infrastructure` — SimBrief, settings, and persistence adapters
- `InsManager.SimConnect` — MSFS connectivity and aircraft-specific adapters
- `InsManager.Tests` — automated domain and adapter tests

## Prerequisites

- Windows 10 or Windows 11
- .NET 8 SDK
- Python 3.10 or newer for pre-commit
- Microsoft Flight Simulator 2024 for simulator integration testing

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

## SimConnect diagnostics

Start MSFS 2024, load the FSS Boeing 727 into a flight, then select **Connect** in INS Manager. A successful connection shows the number of aircraft controls discovered and writes the diagnostic input-event report under `%LocalAppData%\INS Manager\diagnostics`.

The full Microsoft Flight Simulator SDK is optional for building this project. Install it from **MSFS 2024 DevMode → Help → SDK Installer** when the SimConnect Inspector or official samples are needed.
