# INS Manager

INS Manager is a small Windows utility for the classic inertial navigation systems found in Microsoft Flight Simulator aircraft.

The first release officially supports the FlightSim Studio Boeing 727 in Microsoft Flight Simulator 2024. Support for other aircraft may follow once the 727 integration has earned its keep.

## What it does

- Downloads the latest flight plan from SimBrief
- Lists every waypoint with its coordinates, track, and distance
- Sends waypoints to any of the nine CIVA slots
- Reads FROM, TO, accuracy, and waypoint slots from the aircraft
- Refills available slots when automatic waypoint management is enabled
- Selects a direct-to waypoint after confirmation
- Corrects simulated INS drift every 10, 30, or 60 minutes
- Supports light and dark themes

## Requirements

- Windows 10 or Windows 11
- Microsoft Flight Simulator 2024
- FlightSim Studio Boeing 727
- A SimBrief account

The release package includes the .NET runtime. There is no separate runtime to install and nothing belongs in the Community folder.

## Install and run

1. Download the Windows ZIP from the [latest GitHub Release](https://github.com/joelfernandes23/ins-manager/releases/latest).
2. Extract the ZIP to a normal folder.
3. Run `INSManager.exe`.
4. Open Settings, enter your SimBrief Pilot ID, and save.
5. Start MSFS 2024, load the FSS 727 into a flight, then select **Connect**.
6. Select the download button to load the current SimBrief flight plan.

Settings are stored for the current Windows user.

See the [user guide](docs/user-guide.md) for waypoint loading, automatic slot management, direct-to, and drift correction.

Windows may show a SmartScreen warning because the executable is not currently signed with a commercial code-signing certificate. The release includes a SHA-256 checksum for anyone inclined to check the arithmetic.

## Privacy

INS Manager does not include telemetry or analytics. It stores its settings in the current Windows user's local application data folder. When a flight plan is requested, the configured Pilot ID is sent to the SimBrief API.

## Support

Use [GitHub Issues](https://github.com/joelfernandes23/ins-manager/issues) for bugs and compatibility reports. Include the simulator version, FSS 727 version, the action you attempted, and the full error message. Reports that merely say it broke will be admired for their economy and little else.

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
