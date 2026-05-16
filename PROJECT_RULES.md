# IOBusMonitor Codex execution rules

Target project: `Vanderhell/IOBusMonitor`  
Primary goal: make the project credible enough for real use by system integrators and service technicians, while keeping the codebase WPF/.NET Framework for a fast repair cycle.

## Non-negotiable constraints

- Keep WPF desktop application.
- Keep .NET Framework line unless a specific task explicitly changes target framework.
- Do not migrate to Avalonia, MAUI, WinUI, .NET 8, or web app during this reform.
- Do not rewrite the whole application.
- One task = one branch = one pull request.
- No speculative features.
- No marketing claims unless backed by working code, screenshots, tests, or release artifact.
- Do not add yourself as `Author`, `Co-authored-by`, or any AI attribution in commits.
- Preserve MIT licensing.
- Prefer boring, maintainable C# 7.3-compatible code.
- All user-facing UI strings should be English for GitHub/public release consistency.

## Mandatory final report format for every Codex run

Codex must end with exactly these sections:

```text
CHANGED FILES
- ...

CODE CHANGES
- ...

TEST EVIDENCE
- command: ...
- result: PASS/FAIL

BUILD EVIDENCE
- command: ...
- result: PASS/FAIL

VERIFIED FACTS
- ...

NOT VERIFIED
- ...

INCOMPLETE
- ...
```

Rules for report:

- Facts only.
- Do not say “should work” unless it was tested.
- Mark every untested claim as `NOT VERIFIED`.
- If build/test cannot run, state the exact reason.
- Do not include future-looking language such as “next we can”, “will be easy”, “ready for production” unless verified.
