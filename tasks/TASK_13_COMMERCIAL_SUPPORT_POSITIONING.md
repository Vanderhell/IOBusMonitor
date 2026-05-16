# TASK 13 — Commercial support positioning

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

Add honest paid-services positioning without making the MIT project feel crippled.

## Context from audit

The monetization model should be free open-source core plus paid services/custom integrations for system integrators and service technicians.

## Required changes

- Add `SUPPORT.md` describing free community support vs paid support/custom work.
- Add `docs/SERVICES.md` with service examples: custom device templates, protocol adapter, deployment help, branded build, CSV/report customization, training.
- Add contact section in README linking to GitHub issues for bugs and contact/email/website if available for paid work.
- Do not add artificial license restrictions to the MIT core.
- Do not claim certification, industrial safety approval, or guaranteed PLC compatibility unless verified.

## Acceptance criteria

- Paid path is visible but not aggressive.
- MIT open-source promise remains clear.
- Issues/support boundaries are clear.
- No unverifiable enterprise claims.

## Required verification commands

- `No build required unless README/SUPPORT references generated files; if changed, run msbuild Release x64 anyway.`

## Final output

Use the exact mandatory final report format from `PROJECT_RULES.md`.
