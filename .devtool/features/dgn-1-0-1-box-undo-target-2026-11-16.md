---
id: "dgn-1-0-1-box-undo-target-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-16T08:53:11.000Z"
modified: "2026-11-16T10:34:32.000Z"
completedAt: null
labels: ["v1.0.1", "editor", "bug", "common"]
order: "d6"
---
# godoodungeon 1.0.1: Undoing a bounds resize changes another GridMap's settings

Review finding 3 (MINOR). Same defect as in godoowave's `CommitBox`. `CommitBox` (`GodoodungeonPlugin.cs:768-780`) creates "Resize Dungeon Bounds" with no context object and only the plugin as target, so the step goes into the editor's global history. When undone, `SetBox` writes to whatever `_settings` is open at that moment, not to the settings that were dragged.

- Drag the bounds on GridMap A, close the dialog, open it for GridMap B, Ctrl+Z: B's bounds in the dialog jump to A's old box.

## Done when
- The undo step is bound to the settings resource it changed: pass it as a method argument, or move `SetBox` onto the settings. Fix it the same way in godoowave, ideally through one shared helper next to `BoundsGizmoPlugin`.
- Engine test: resize, switch `_settings` to another resource, undo; only the first resource changes.
- Manual: the case above in the editor.

## Result
- `DungeonSettings.SetBounds(origin, size)` is the do and undo target; `CommitBox` adds both calls on the dragged `_settings` resource instead of on the plugin. The step goes into the edited scene's history: a resource without a path counts as built-in in `EditorUndoRedoManager` (the reviewer's correction), as the inspector's own edits do. The plugin's `SetBox`/`CommitBox` are private and lost their unused box index.
- No shared helper: `DungeonSettings` and `WfcSettings` have no common base, and the fix is the same two lines in each plugin (godoowave 1.4.1 did `WfcSettings.SetBox`).
- A drag that loses its target (deselected, dialog closed, generation started) is cancelled, box put back, as in godoowave 1.4.1.
- Engine test `DungeonSettingsTests.Undoing_a_bounds_resize_changes_only_the_settings_that_were_dragged`: the plugin's pair on a plain `UndoRedo`, called by name; undo and redo change only the dragged settings.
- Build clean, 109 engine tests pass. CHANGELOG Unreleased entry.

## Manual check
1. Open the dialog for GridMap A, drag a bounds handle, close the dialog.
2. Open the dialog for GridMap B (with stored settings, e.g. the second demo scene's GridMap or a duplicate), Ctrl+Z: B's bounds do not change.
3. With the dialog open on A, drag the bounds, Ctrl+Z, Ctrl+Shift+Z: the box steps back and forward as in 1.0.0.

## Review (stern-reviewer, 2026-11-16)
- MINOR, not fixed in code: the engine test builds the same do and undo pair as `CommitBox` on a plain `UndoRedo`, so it checks `SetBounds` and calling it by name, not the plugin. If `CommitBox` went back to targeting the plugin, the test would still pass. The plugin needs `EditorUndoRedoManager`, which headless tests do not have. The plugin wiring is covered only by the manual check.
- Pre-existing, fixed: `OpenDialog` now cancels a drag whose mouse release was lost, on the settings it started on, before it opens another GridMap's settings.
