# Release checklist

## Before release

- [ ] Fresh clone builds on Windows.
- [ ] NuGet restore documented and tested.
- [ ] Debug build passes.
- [ ] Release x64 build passes.
- [ ] Tests pass.
- [ ] App starts on a clean machine or VM.
- [ ] Demo mode works without hardware.
- [ ] Start/Stop monitoring tested with no devices.
- [ ] Start/Stop monitoring tested with demo provider.
- [ ] At least one real Modbus TCP device tested, if available.
- [ ] At least one RTU scenario tested, if available.
- [ ] At least one Siemens S7 scenario tested, if available.
- [ ] Logs are created in expected folder.
- [ ] Settings/Data folders are not accidentally shipped with private plant data.
- [ ] README matches actual release state.
- [ ] Screenshots match actual UI.
- [ ] ZIP package contains all required DLLs.
- [ ] Version displayed in app matches release tag.
- [ ] Release notes include fixed bugs and known limitations.

## Release artifact

- [ ] `IOBusMonitor-vX.Y.Z-win-x64-portable.zip`
- [ ] SHA256 checksum
- [ ] release notes
- [ ] screenshots

## Do not release if

- [ ] Siemens path still fails through dynamic method mismatch.
- [ ] RTU values save as TCP point type.
- [ ] RTU polling throws null measurement collection.
- [ ] Dashboard loads all archive rows on startup.
- [ ] App requires Visual Studio installed to run release ZIP.
