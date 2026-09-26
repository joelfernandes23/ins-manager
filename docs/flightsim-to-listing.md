# Flightsim.to listing

## Listing details

**Title:** INS Manager

**Category:** Applications / Utilities

**Version:** 1.0.0

**Official simulator support:** Microsoft Flight Simulator 2024

**Experimental compatibility:** Microsoft Flight Simulator 2020

**Required aircraft:** FlightSim Studio Boeing 727

**Required service:** SimBrief account and Pilot ID

## Short description

Load SimBrief routes into the FSS 727 CIVA, manage its nine waypoint slots, select direct-to waypoints, and correct simulated INS drift from a compact Windows utility.

## Description

INS Manager is a small desktop companion for the FlightSim Studio Boeing 727 and its classic CIVA inertial navigation system.

It downloads the latest route for your SimBrief Pilot ID and presents every waypoint in a compact, scrollable table. Waypoints can be sent to any of the nine CIVA slots. Automatic waypoint management keeps the next group of waypoints in sync, while direct-to selection updates the active CIVA slots after confirmation.

Optional drift correction can realign the simulated INS position every 10, 30, or 60 minutes. The default is 30 minutes. It corrects the position used by the INS but does not alter the aircraft developer's displayed accuracy model.

The application is developed and tested with Microsoft Flight Simulator 2024. MSFS 2020 connections are allowed for users who wish to try them, but 2020 remains experimental and is not officially supported.

### Features

- SimBrief flight-plan download
- Full waypoint table with coordinates, track, and distance
- Manual loading into CIVA slots 1 through 9
- Automatic management of upcoming waypoint slots
- Direct-to waypoint selection
- Configurable drift correction at 10, 30, or 60 minute intervals
- Light and dark themes
- Self-contained Windows x64 package

## Installation

1. Download and extract the ZIP to a normal folder.
2. Run `INSManager.exe`.
3. Open Settings and enter your SimBrief Pilot ID.
4. Start Microsoft Flight Simulator and load the FSS 727 into a flight.
5. Select **Connect**, then use the download button to load the current SimBrief route.

This is a desktop application. Do not place it in the Community folder.

The .NET runtime is included. No separate runtime installation is required.

Windows may display a SmartScreen warning because the executable does not yet have a commercial code-signing certificate. A SHA-256 checksum is provided with every GitHub Release.

## Known limitations

- Only the FlightSim Studio Boeing 727 is supported in version 1.0.0.
- Microsoft Flight Simulator 2024 is the only officially supported simulator version.
- Microsoft Flight Simulator 2020 compatibility is experimental and depends on the aircraft exposing the same CIVA local variables.
- The utility loads en-route waypoints from SimBrief. Procedure expansion from simulator navdata is not included in version 1.0.0.
- Drift correction realigns the INS position. It does not replace or freeze the FSS accuracy-index display.
- The application must run on the same Windows computer as Microsoft Flight Simulator.

## Privacy

INS Manager contains no telemetry or analytics. Settings are stored in local application data for the current Windows user. The configured Pilot ID is sent to SimBrief only when requesting a flight plan.

## Support and source

- Releases: https://github.com/joelfernandes23/ins-manager/releases
- Issues: https://github.com/joelfernandes23/ins-manager/issues
- Source: https://github.com/joelfernandes23/ins-manager

INS Manager is an independent utility. It is not affiliated with or endorsed by FlightSim Studio, SimBrief, Microsoft, or Asobo Studio.

## Version 1.0.0

- Initial public release
- FSS 727 CIVA waypoint loading and slot management
- Direct-to support
- SimBrief route download
- Configurable drift correction
- Official MSFS 2024 support
- Experimental MSFS 2020 connections
