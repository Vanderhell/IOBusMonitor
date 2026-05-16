# 30/60/90-day reform roadmap

## 0–14 days: correctness and trust baseline

Goal: stop obvious runtime failure paths and make the repository buildable by another person.

Deliverables:

- Debug and Release build verified.
- Target framework/documentation mismatch resolved.
- TimerService RTU/S7 bugs fixed.
- RTU `Measurements` initialization fixed.
- Basic unit test project added.
- GitHub Actions Windows build added.
- README updated only enough to be honest.

Exit criteria:

- Fresh clone builds on Windows.
- Polling can start/stop repeatedly without unhandled exception in demo configuration.
- Unit tests run in CI.

## 15–45 days: technician-grade application

Goal: make the app usable by someone who did not write the code.

Deliverables:

- Modern WPF shell.
- Dashboard with device/point/measurement status.
- Search/filter/grouping.
- Device test connection button.
- Better validation in admin pages.
- Storage indexes and fast history query.
- CSV export.
- Demo/sample mode.

Exit criteria:

- A user can download ZIP, open demo mode, see live values, open charts, export CSV.
- No hardware required to understand value proposition.

## 46–90 days: adoption and paid-services readiness

Goal: make it possible to present/sell services around the tool.

Deliverables:

- Portable release ZIP.
- Optional installer.
- Website/landing README copy.
- Support policy.
- Paid services page.
- Device template format.
- Example templates for 2–3 common Modbus devices or simulated devices.
- Release notes and changelog.

Exit criteria:

- Public release page has binaries, screenshots, docs and limitations.
- README communicates what it is, who it is for, and what it is not.
- You can send the repo link to an integrator without apologizing for presentation.
