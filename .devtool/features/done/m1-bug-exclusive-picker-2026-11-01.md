---
id: "m1-bug-exclusive-picker-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T13:00:00.000Z"
modified: "2026-11-01T00:40:02.000Z"
completedAt: "2026-11-01T00:40:02.000Z"
labels: ["bug", "editor", "common"]
order: "a1"
---
# Bug: node picker logs "parent window already has another exclusive child"

Found through the editor log (godot-mcp `get_editor_log`) when the board used the surface picker:

```
scene/main/window.cpp:1160 _set_transient_exclusive_child: Attempting to make child window exclusive, but the parent
window already has another exclusive child. This window: …/@SceneTreeDialog…, current exclusive child window: …/@ConfirmationDialog…
```

**Cause:** `ToolDialog` is a `ConfirmationDialog`, which is exclusive by default. `EditorInterface.PopupNodeSelector` opens its `SceneTreeDialog`, also exclusive, under the same parent window, and Godot allows only one exclusive child per parent.

**Fix:** `ToolDialog` sets `Exclusive = false` and keeps `Transient = true`, so it stays above the editor. A side benefit is that the editor (viewport, scene tree) stays usable while the dialog is open.

## Result (needs a manual check)
Built, then the plugin was disabled and re-enabled through godot-mcp so the dialog is recreated (a reload does not rebuild an existing dialog). Log is clean.
**Check:** open the dialog, use the Surface picker, then read the log. There should be no exclusive-child error, and the MultiMesh menu should show "Scatter Surface…" once.

## Board check (2026-11-01)
The picker was used again and the editor log shows no new entries, so the exclusive-child error is gone.
