# TASK 11 — Release packaging and versioning

You are Codex working inside the `Vanderhell/IOBusMonitor` repository.

EXECUTION MODE:
- Execute the task exactly as written.
- Do not redesign unrelated parts.
- Do not migrate framework/UI technology unless explicitly requested.
- Do not add unrelated features.
- Do not modify licensing.
- Do not add yourself as Author or Co-authored-by in any commit or file.
- Keep code compatible with C# 7.3 unless the project is explicitly changed.
- End with the mandatory report sections from `PROJECT_RULES.md`.

## Objective

Make the app downloadable and identifiable as a real release.

## Context from audit

Current README says copy `bin/Release`. That is not enough for non-developer adoption.

## Required changes

- Add version information to AssemblyInfo and show app version in About/status bar.
- Create PowerShell packaging script `build/package-release.ps1` that restores, builds Release x64, collects required EXE/DLL/assets, and creates ZIP.
- Exclude local Settings/Data/Logs from release package unless creating demo package intentionally.
- Add `RELEASE_NOTES_TEMPLATE.md`.
- Add optional checksum generation for release ZIP.
- Document release steps in `docs/RELEASE.md`.

## Acceptance criteria

- One command creates a clean portable ZIP.
- ZIP contains required runtime DLLs/assets/manuals but no developer junk.
- App version is visible in UI.
- Release documentation is accurate.

## Required verification commands

- `.\build\package-release.ps1`
- `msbuild .\IOBusMonitor.sln /p:Configuration=Release /p:Platform=x64`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
