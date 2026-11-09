---
id: "wfc-m0-verify-thread-reload-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M0 Foundations"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:31:59.000Z"
completedAt: "2026-11-09T16:31:59.000Z"
labels: ["spike"]
order: "a3"
---
# WFC M0: Verify background thread and reload

Check brief §10 item 6: can a C# background thread be reliably stopped before an assembly reload? Build a throwaway long-running thread started from the plugin and find the right shutdown hook.

Headless runs cannot trigger a reload, so this needs a manual check in the editor. Move to `review` for that.

## Done when
- The shutdown approach is recorded here.
- Rebuilding while the thread runs neither crashes nor hangs the editor.

## Approach
`WfcGeneratorPlugin` implements `ISerializationListener`. `OnBeforeSerialize` runs before the assembly unloads, sets a cancel flag and joins the thread. `_ExitTree` does the same. This is a temporary spike, replaced by the solve runner in M2. Build and headless load pass, but headless cannot trigger a reload, so the hook itself is not yet proven.

## Manual check
1. Focus Godot and select a `GridMap`. Choose **GridMap > Start reload test thread (spike)**. One CPU core goes busy, and the Output panel says the thread started.
2. Run `dotnet build Godoodads.csproj` after making any small change to a C# file, otherwise nothing is reloaded.
3. Focus Godot. Output should show `stop on before assembly reload, thread stopped = True`, then `assembly reloaded`, and the editor should keep working.

## Manual result (2026-11-09)
Passed in the editor. Rebuilding while the thread ran printed `stop on before assembly reload, thread stopped = True`, then `assembly reloaded`. Use `ISerializationListener.OnBeforeSerialize` for the M2 solve runner.
