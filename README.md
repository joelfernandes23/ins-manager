# INS Manager

INS Manager is a small Windows utility for the classic inertial navigation systems found in Microsoft Flight Simulator aircraft.

The first release officially supports the FlightSim Studio Boeing 727 in Microsoft Flight Simulator 2024. Support for other aircraft may follow once the 727 integration has earned its keep.

## What it does

- Downloads the latest flight plan from SimBrief
- Lists every waypoint with its coordinates, track, and distance
- Sends waypoints to any of the nine CIVA slots
- Resynchronises upcoming slots when automatic waypoint management is enabled
- Selects a direct-to waypoint after confirmation
- Corrects simulated INS drift every 10, 30, or 60 minutes
- Supports light and dark themes

## Requirements

- Windows 10 or Windows 11
- Microsoft Flight Simulator 2024, or Microsoft Flight Simulator 2020 for experimental use
- FlightSim Studio Boeing 727
- A SimBrief account

The release package includes the .NET runtime. There is no separate runtime to install and nothing belongs in the Community folder.

## Install and run

1. Download the Windows ZIP from the latest GitHub Release.
2. Extract the ZIP to a normal folder.
3. Run `INSManager.exe`.
4. Open Settings, enter your SimBrief Pilot ID, and save.
5. Start MSFS 2024, load the FSS 727 into a flight, then select **Connect**.
6. Select the download button to load the current SimBrief flight plan.

Settings are stored for the current Windows user.

## MSFS 2020

MSFS 2020 connections are allowed on an experimental basis. The application will identify them as experimental, but it will not prevent waypoint, direct-to, or drift-correction commands from being sent.

MSFS 2020 is not an officially supported platform. The current adapter was developed and tested against MSFS 2024 and the FSS 727. Reports from 2020 users are welcome, especially when accompanied by the aircraft version and a useful description of what went wrong.

## Development

The repository expects the .NET 8 SDK and Python 3.10 or newer. Python is only used for the pre-commit hooks.

```powershell
.\scripts\setup.ps1
.\dev.ps1 run
```

Useful commands:

```powershell
.\dev.ps1 setup
.\dev.ps1 build
.\dev.ps1 test
.\dev.ps1 format
.\dev.ps1 check
```

The solution is split into a few deliberately dull projects:

- `InsManager.App`: WPF views, view models, and application startup
- `InsManager.Core`: route models and simulator-independent contracts
- `InsManager.Infrastructure`: SimBrief and settings persistence
- `InsManager.SimConnect`: MSFS connectivity and aircraft integration
- `InsManager.Diagnostics`: command-line tools for inspecting aircraft events
- `InsManager.Tests`: automated tests

## Simulator diagnostics

With the aircraft loaded, run the scanner to export matching CIVA and INS input events:

```powershell
.\dev.ps1 scan
```

Additional commands are available when the ordinary route to happiness has failed:

```powershell
.\dev.ps1 scan --civa-state
.\dev.ps1 scan --write-self-test
.\dev.ps1 scan --input-details INS
.\dev.ps1 scan --input-write-self-test
```

The write tests restore the values they change. The full MSFS SDK is optional and is only needed for the SimConnect Inspector or official samples.

## Releases

The project uses Conventional Commits and Release Please. A push to `main` updates the release pull request. Merging that pull request creates the version tag, changelog, and GitHub Release.

The Windows package is built only by GitHub Actions. The workflow tests the tagged source, creates a self-contained x64 ZIP, calculates its SHA-256 checksum, and attaches both files to the release.

The first automated release is pinned to `v1.0.0`. After that release, remove the one-time `release-as` entry from `release-please-config.json` and let Conventional Commits determine subsequent versions.
