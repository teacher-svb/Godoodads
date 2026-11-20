---
id: "wave-1-4-1-box-undo-target-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-16T08:43:23.000Z"
modified: "2026-11-16T09:18:50.000Z"
completedAt: "2026-11-16T09:18:50.000Z"
labels: ["v1.4.1", "editor", "bug"]
order: "d2"
---
# godoowave 1.4.1: Undoing a box resize changes another GridMap's settings

Review finding 2 (MINOR). `CommitBox` (`WfcGeneratorPlugin.cs:918-930`) creates "Resize WFC Bounds" / "Resize WFC Pin Box" with no context object and only `this` as target, so the step goes into the editor's global history. When undone, `SetBox` writes to whatever `_settings` is open at that moment, not to the settings that were dragged.

- Drag the bounds on GridMap A, close the dialog, open it for GridMap B (B's stored settings load), Ctrl+Z: B's bounds in the dialog jump to A's old box.

## Done when
- The undo step is bound to the settings resource it changed: pass it as a method argument, or make `SetBox` a method on `WfcSettings` (shared code: also check godoodungeon, which uses the same gizmo).
- Engine test: resize, switch `_settings` to another resource, undo; only the first resource changes.
- Manual: the case above in the editor.

## Result
- `SetBox(box, origin, size)`, `BoxOrigin(box)` and `BoxSize(box)` are now methods on `WfcSettings` (shared, `addons/editor_tools_common/Wfc/Resources/`). `CommitBox` adds the do and undo calls on the dragged `_settings` resource instead of on the plugin, so undoing after the dialog has another GridMap's settings open changes only the old copy. The step stays in the global history, as before. The plugin's `SetBox`/`CommitBox` are private now.
- godoodungeon has the same defect (`dgn-1-0-1-box-undo-target-2026-11-16`, its own worktree). `DungeonSettings` does not use `WfcSettings`, so it is not changed here; the same fix applies there: a `SetBox` on `DungeonSettings` and `CommitBox` targeting the resource.
- Engine test `WfcSettingsBoxTests.Undoing_a_box_resize_changes_only_the_settings_that_were_dragged`: the plugin's do/undo pair on a plain `UndoRedo`, called by method name, for the bounds and the pin box; undo and redo change the dragged settings and leave another settings resource alone.

## Manual check
1. Open the dialog for GridMap A, drag a bounds handle, close the dialog.
2. Open the dialog for GridMap B (with stored settings), Ctrl+Z: B's bounds do not change. Reopen A: its dialog shows A's stored settings (the dragged values were never stored, as before).
3. With the dialog open on A, drag the bounds and the pin box, Ctrl+Z twice, Ctrl+Shift+Z: the boxes step back and forward as in 1.4.0.

## Released
In godoowave 1.4.1 (tag `godoowave-v1.4.1`, PR #19).
