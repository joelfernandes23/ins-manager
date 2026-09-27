# INS Manager

INS Manager loads a SimBrief route into the CIVA fitted to the FlightSim Studio Boeing 727.

It is a Windows desktop app for Microsoft Flight Simulator 2024. Nothing needs to be copied into the Community folder.

## Features

- Download the latest flight plan for a SimBrief Pilot ID
- View waypoint coordinates, track, and distance
- Load a waypoint into any of the nine CIVA slots
- Fill the next nine slots automatically from a selected waypoint
- Set a direct-to waypoint
- Correct INS drift every 10, 30, or 60 minutes
- Use a light or dark theme

## Requirements

- Windows 10 or 11
- Microsoft Flight Simulator 2024
- FlightSim Studio Boeing 727
- A SimBrief account

The app is currently tested only with MSFS 2024 and the FSS 727.

## Install

1. Download the Windows ZIP from the [latest release](https://github.com/joelfernandes23/ins-manager/releases/latest).
2. Extract it to a folder of your choice.
3. Run `INSManager.exe`.

The .NET runtime is included. Windows may show a SmartScreen warning because the app is not code-signed. Each release includes a SHA-256 checksum beside the ZIP.

## First flight

1. Open Settings, enter your SimBrief Pilot ID, and select Save.
2. Start MSFS 2024 and load the FSS 727 into a flight.
3. Select Connect in INS Manager.
4. Select the download button to load your latest SimBrief route.
5. Use Send to choose a CIVA slot, or DCT to make a waypoint direct-to.

With Auto waypoints enabled, Send and DCT also load the following waypoints into the remaining CIVA slots.

## Data and privacy

Settings are stored in `%LOCALAPPDATA%\INS Manager\settings.json`. INS Manager has no telemetry or analytics. It sends the configured Pilot ID to SimBrief only when downloading a flight plan.

## Problems

Open a [GitHub issue](https://github.com/joelfernandes23/ins-manager/issues) and include:

- What you were trying to do
- The full error message
- Your MSFS and FSS 727 versions
- Whether the problem happens again after restarting the app and simulator

## Development

You need the .NET 8 SDK. Python 3.10 or newer is used only by the Git hooks.

```powershell
.\scripts\setup.ps1
.\dev.ps1 run
```

The main commands are:

```powershell
.\dev.ps1 build
.\dev.ps1 test
.\dev.ps1 format
.\dev.ps1 check
```

Project layout:

- `InsManager.App` — WPF application and UI
- `InsManager.Core` — route models and interfaces
- `InsManager.Infrastructure` — SimBrief and local settings
- `InsManager.SimConnect` — simulator and FSS 727 integration
- `InsManager.Diagnostics` — tools for inspecting aircraft events
- `InsManager.Tests` — automated tests

To scan the loaded aircraft for CIVA and INS input events:

```powershell
.\dev.ps1 scan
```

Useful diagnostic options:

```powershell
.\dev.ps1 scan --civa-state
.\dev.ps1 scan --write-self-test
.\dev.ps1 scan --input-details INS
.\dev.ps1 scan --input-write-self-test
```

The write tests restore the values they change.

## Releases

Release Please keeps one release PR open and adds each releasable change to it. Ordinary merges to `main` do not publish a release.

When the accumulated changes are ready to ship, merge the release PR. That creates the version tag and GitHub Release, then builds and attaches the self-contained Windows ZIP and its checksum.
