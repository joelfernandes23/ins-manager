# Flightsim.to release checklist

## Package

- [ ] Download `INS-Manager-1.0.0-win-x64.zip` from the GitHub Release
- [ ] Compare its SHA-256 hash with `INS-Manager-1.0.0-win-x64.zip.sha256`
- [ ] Extract the ZIP on a clean Windows account
- [ ] Confirm `INSManager.exe` starts without a separate .NET installation
- [ ] Confirm the application connects to MSFS 2024 with the FSS 727 loaded
- [ ] Confirm a SimBrief route can be downloaded
- [ ] Confirm a waypoint can be sent to a CIVA slot
- [ ] Confirm the direct-to confirmation dialog works
- [ ] Confirm drift correction can be enabled and disabled

## Listing

- [ ] Upload the full ZIP directly to Flightsim.to
- [ ] Select Microsoft Flight Simulator 2024 as the supported simulator
- [ ] Mention MSFS 2020 only as experimental compatibility
- [ ] List the FSS Boeing 727 and SimBrief as dependencies
- [ ] Paste the installation and known-limitations sections from `flightsim-to-listing.md`
- [ ] Link the GitHub source, releases, and issue tracker
- [ ] State that the application does not belong in the Community folder
- [ ] State that the executable is not commercially code signed

## Screenshots

- [ ] Main window with a SimBrief route loaded
- [ ] Settings panel showing aircraft, theme, and drift interval
- [ ] Send-waypoint slot selector
- [ ] Direct-to confirmation dialog
- [ ] Connected status with the FSS 727 visible in the simulator

Do not include a real SimBrief Pilot ID, account name, flight-plan remarks, or private VATSIM details in the screenshots.
