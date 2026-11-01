---
id: "m3-bug-reload-errors-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M3 Mask texture"
dueDate: null
created: "2026-11-01T01:10:57.000Z"
modified: "2026-11-01T01:19:03.000Z"
completedAt: "2026-11-01T01:19:03.000Z"
labels: ["bug", "editor", "common"]
order: "a5"
---
# Bug: errors in the editor log on assembly reload

Found with godot-mcp `get_editor_log` after a build (seq 5–13):

1. `ObjectDisposedException … MultiMeshScatterPlugin` in `SpatialMenuHook.SaveGodotObjectData`. After the plugin was disabled and re-enabled, the old hook RefCounted stayed alive until .NET garbage collection, still referencing the freed plugin. The reload tried to serialize it and failed.
2. Eight times `managed_callable->delegate_handle.value == nullptr`. The tool-button row connected each Button to the resource's lambda Callables (`() => SetTarget(...)`). Compiler-generated lambda names are not stable across builds, so the delegates cannot be restored after a reload. The stale connections then log this on every following reload.

**Fix:**
- `SpatialMenuHook.Remove()` clears every reference, including `_plugin`.
- `ToolButtonRowInspectorPlugin` uses `ToolRowButton`, a `[Tool]` Button that stores the target and property in fields and overrides `_Pressed` (C# Callable has no `Bind`).
- `ScatterSettings` uses named methods instead of lambdas.
- New convention: no lambdas in signal connections that outlive a reload.

## Result (needs a manual check)
- `SpatialMenuHook.Remove()` clears all references.
- New `ToolRowButton` (`[Tool]` Button with Target/Property fields, `_Pressed` override) is used by `ToolButtonRowInspectorPlugin`.
- `ScatterSettings` uses named methods.
- CONVENTIONS has two new reload rules.

Through godot-mcp: build (reload #1 still logged 4 stale-button errors, down from 8), plugin off/on, then touch + build (reload #2). The log stays clean since seq 17, but it is not certain reload #2 ran before the window was focused.
**Check:** focus the editor, open Scatter Surface and use a +Y/-Y button, then I read the log once more.

## Verified (2026-11-01)
The board used the dialog (Density test) after reload #2, and the log has stayed empty since seq 17 through two more reloads. The stale-callable and disposed-object errors are gone.
