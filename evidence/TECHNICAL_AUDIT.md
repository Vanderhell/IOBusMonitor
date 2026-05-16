# Technical audit — IOBusMonitor

Audit date: 2026-05-15  
Repository: `Vanderhell/IOBusMonitor`  
Inspected areas: README, solution/project files, timer loop, SQLite storage/loading, WPF shell, dashboard view model, protocol read services.

## Executive verdict

The idea is useful. A desktop utility that can read Modbus TCP, Modbus RTU and Siemens S7, log to SQLite and show live/history data is a real industrial-service use case.

The current repository is not yet credible as a tool that people will adopt without knowing you personally. The biggest blockers are not only graphics. The bigger blockers are:

1. runtime bugs in the polling path,
2. weak build/release discipline,
3. no test proof,
4. poor dashboard scalability,
5. unprofessional UI shell,
6. weak public positioning.

## Severity table

| Severity | Area | Finding | Impact |
|---|---|---|---|
| Critical | Polling | RTU path calls TCP storage method | wrong `PointType`, wrong archive semantics, bad history grouping |
| Critical | Siemens S7 | `TimerService` dynamically calls `LoadPointDataAsync`, but S7 service has `LoadPLCActualDataAsync` | Siemens polling can fail at runtime |
| Critical | RTU reader | `PointViewModel.Measurements` is not initialized before `.Add(...)` | RTU polling can throw `NullReferenceException` |
| High | Storage | SQLite inserts are not wrapped in explicit transactions | poor performance and more fragile writes |
| High | History | loads `SELECT *` from every daily DB | becomes unusable with real data volume |
| High | Build credibility | README says .NET Framework 4.8, projects target v4.7.2 | public documentation mismatch |
| High | UI | emoji menu + hardcoded colors/sizes + borderless window | looks like hobby/demo app, not technician tool |
| Medium | Architecture | use of `dynamic` in polling | hides compile-time errors and caused S7 mismatch |
| Medium | Dashboard | repeated protocol blocks | expensive to maintain and inconsistent over time |
| Medium | Dependencies | duplicated/unclear UI packages and Roslyn scripting dependencies | larger app, more attack/update surface, harder packaging |
| Medium | Docs | no release workflow, no installer notes, weak screenshots/story | low adoption conversion |

## Product-market framing

Best target user: system integrator / field technician who needs a fast Windows tool to:

- check if PLC/register values are changing,
- log values during commissioning,
- compare historical values,
- export evidence to CSV,
- test simple scaling/formulas,
- avoid deploying a full SCADA or historian.

Bad target framing: “SCADA replacement”. That would create unrealistic expectations.

Good target framing:

> Portable Windows tool for field-bus monitoring, short-term logging and troubleshooting of Modbus TCP/RTU and Siemens S7 data.

## MVP definition for public use

The project becomes realistically usable when it has:

- portable release ZIP,
- sample/demo mode with no PLC needed,
- stable polling start/stop,
- clear per-device status,
- connection errors visible in UI,
- CSV export,
- basic history chart with date range,
- tested expression evaluator/address parser/storage path,
- clean README with screenshots and limitations.

## Commercial reality

Do not expect passive income from GitHub stars. The realistic money path is service-driven:

- custom device templates,
- custom protocol support,
- on-site setup,
- branded internal release,
- integration with existing plant SQLite/SQL/CSV workflows,
- support contract.

This means the repo must become a trust artifact: clean, buildable, documented, stable.
