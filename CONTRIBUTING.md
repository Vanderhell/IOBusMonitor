# Contributing

`IOBusMonitor` is a Windows desktop app built on .NET Framework and WPF. Keep contributions practical and narrowly scoped.

## Before you change code

- Prefer existing patterns and services in the repository.
- Keep public UI text in English unless the existing screen is already localized.
- Avoid adding new frameworks unless there is a clear payoff.

## Build expectations

- Use Visual Studio 2019 or newer, or the matching Build Tools/MSBuild environment.
- Restore packages before building.
- Validate the solution in Windows when a change touches build, packaging, or WPF XAML.

## Tests

- Add or update unit tests for library behavior when possible.
- Keep tests focused on deterministic logic.
- Do not add flaky UI or hardware-dependent tests unless there is a strong reason.

## Pull requests

- Make the purpose of the change obvious from the commit or PR title.
- Include verification notes when the change affects build, packaging, or UI behavior.
- Keep unrelated cleanup out of the same change unless it reduces risk.
