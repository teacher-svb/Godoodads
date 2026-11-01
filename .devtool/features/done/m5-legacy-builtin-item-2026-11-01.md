---
id: "m5-legacy-builtin-item-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M5 Polish"
dueDate: null
created: "2026-11-01T02:04:49.000Z"
modified: "2026-11-01T02:10:16.000Z"
completedAt: "2026-11-01T02:10:16.000Z"
labels: ["ui", "common"]
order: "a8"
---
# Rename the built-in item to "Populate Surface (Godot)"

Board decision (M5 review), answering the open question about the built-in item: keep it, but label it as legacy.

`SpatialMenuHook.AppendToHostItems(" (legacy)")` appends the suffix to every built-in item of the host menu, so the item's translated text is kept. The original texts are stored (reload-safe Godot Dictionary) and restored in `Remove()`, so disabling the plugin undoes it and re-enabling does not double the suffix.

## Result (needs a manual check)
Built (editor build clean). README "Use" and CHANGELOG updated.
**Check:** focus Godot, toggle the plugin off and on. The MultiMesh menu should read "Populate Surface (Godot)" and "Populate Surface (GDDS)". Toggle once more to confirm the suffix does not double.

## Follow-up (board)
Our item is renamed to **"Populate Surface (GDDS)"** (GDDS = Godoodads), so the two items pair up in the menu. The dialog title and undo action use the same name (`ToolName` const). README, root README, CHANGELOG and CLAUDE.md updated.

## Follow-up (board)
"(legacy)" was misleading, so the built-in item is now labelled **"Populate Surface (Godot)"**. Code and docs updated.

## Board check (2026-11-01)
Accepted with the M5 review (v1.0.0).
