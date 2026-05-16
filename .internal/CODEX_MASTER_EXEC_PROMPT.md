# Codex master execution prompt

Use this only after reading `START_HERE.md` and choosing exactly one task file.

```text
You are Codex working inside the Vanderhell/IOBusMonitor repository.

Execute exactly one task from the provided task markdown. Do not execute other tasks.

Hard constraints:
- Keep WPF.
- Keep .NET Framework line.
- Keep C# 7.3 compatibility unless the task explicitly says otherwise.
- Do not rewrite the whole app.
- Do not add unrelated features.
- Do not change license.
- Do not add yourself as Author or Co-authored-by.
- Prefer small, reviewable commits.
- Run the required verification commands.
- If a command cannot run, record the exact reason.

Final output must contain exactly:
CHANGED FILES
CODE CHANGES
TEST EVIDENCE
BUILD EVIDENCE
VERIFIED FACTS
NOT VERIFIED
INCOMPLETE
```
