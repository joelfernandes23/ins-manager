# INS Manager

A Windows desktop utility for managing classic inertial navigation systems in Microsoft Flight Simulator.

## Status

The application currently supports the FlightSim Studio Boeing 727 in Microsoft Flight Simulator 2024. It downloads real flight plans from SimBrief, writes waypoint coordinates directly to the aircraft CIVA slots, supports direct-to selection, and can correct INS position drift at a configurable 10-minute, 30-minute, or one-hour interval.

## Architecture

- `InsManager.App` — WPF views, MVVM view models, and application composition
- `InsManager.Core` — simulator-independent route and INS domain contracts
- `InsManager.Infrastructure` — SimBrief, settings, and persistence adapters
- `InsManager.SimConnect` — MSFS connectivity and aircraft-specific adapters
- `InsManager.Diagnostics` — command-line tools for discovering aircraft input events
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
.\dev.ps1 scan
.\dev.ps1 format
.\dev.ps1 check
.\dev.ps1 package 0.1.0
```

The package command creates a self-contained Windows x64 ZIP and SHA-256 checksum in `artifacts\release`. End users do not need to install .NET and should run `INSManager.exe`; this is a desktop utility, not a Community-folder package.

## Releases

Every push and pull request is formatted, built, tested, and packaged by GitHub Actions. A manual Release workflow run produces downloadable workflow artifacts without publishing a release.

Pushing a semantic-version tag publishes the same package as a GitHub Release:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The repository uses pre-commit for whitespace, YAML, merge-conflict, and .NET formatting checks. Tests run on the pre-push hook.

## SimConnect

Start MSFS 2024, load the FSS Boeing 727 into a flight, then select **Connect** in INS Manager.

Run `.\dev.ps1 scan` with the aircraft loaded to export matching CIVA/INS input events to `input-events.json`. Optional search terms can be supplied after `scan`.

Use `.\dev.ps1 scan --civa-state` for a read-only dump of the live CIVA position and waypoint slots. `.\dev.ps1 scan --write-self-test` temporarily verifies slot 9 and restores its original values.

Use `.\dev.ps1 scan --input-details INS` to inspect the loaded aircraft's INS input events. `.\dev.ps1 scan --input-write-self-test` safely verifies selector writes by retaining their existing values.

The full Microsoft Flight Simulator SDK is optional for building this project. Install it from **MSFS 2024 DevMode → Help → SDK Installer** when the SimConnect Inspector or official samples are needed.
