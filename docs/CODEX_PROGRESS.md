# Codex Progress

Last updated: 2026-05-16

## Current status

- Completed through `TASK_13_COMMERCIAL_SUPPORT_POSITIONING.md`.
- Last completed implementation: README/support/services positioning updates plus shell XAML/resource fixes that restored successful WPF app project compilation.
- Full solution verification is still partially blocked by the available Windows toolchain for the SDK-style test project: `IOBusMonitorLib.Tests.csproj` cannot resolve `Microsoft.NET.Sdk` in the currently used MSBuild environment.

## Next task

- Next recommended task from `START_HERE.md`:
  - no remaining packaged task file after `TASK_13`; the next practical work item is fixing the Windows solution/test toolchain so full solution restore/build/test can pass consistently

## Important context

- `TASK_00` through `TASK_10` have been worked through sequentially.
- Many repo changes are already in progress; do not revert unrelated edits.
- Final reports were produced for each completed task using the mandatory format from `PROJECT_RULES.md`.
- The current environment can inspect and edit code, but cannot perform the required Windows restore/build verification commands locally.

## Support/services summary

- `README.md` now includes a support/services path, conservative contact section, and honest paid-services positioning without restricting the MIT core.
- Added `docs/SERVICES.md` with optional paid/custom work examples: device templates, protocol adapters, deployment help, branded builds, CSV/report customization, and training.
- `SUPPORT.md` now distinguishes free GitHub issue support from paid/custom support and links to the services page.
- `App.xaml`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `AppSettingsPage.xaml.cs`, `IOBusMonitor.csproj`, and `Resources/ShellTheme.xaml` were corrected so the WPF app project builds again.

## Verification state

- Windows WPF app project build with explicit MSBuild path -> PASS for `IOBusMonitor\IOBusMonitor.csproj` in `Debug|AnyCPU`
- Full solution `Release|x64` build with explicit MSBuild path -> failed only on `IOBusMonitorLib.Tests.csproj` because `Microsoft.NET.Sdk` was not available to that MSBuild environment
- Windows `nuget restore .\IOBusMonitor.sln` -> partially restored packages but reported the same SDK-resolution problem for `IOBusMonitorLib.Tests.csproj`

## Resume instruction

If the next user message is just “pokracuj”, continue with:

1. fix the Windows solution/test toolchain so `IOBusMonitorLib.Tests.csproj` can resolve `Microsoft.NET.Sdk`
2. keep using the mandatory final report format from `PROJECT_RULES.md`
3. preserve all existing changes unless explicitly asked otherwise
