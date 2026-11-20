---
id: "wave-1-4-1-run-settings-snapshot-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-16T08:43:23.000Z"
modified: "2026-11-16T09:18:50.000Z"
completedAt: "2026-11-16T09:18:50.000Z"
labels: ["v1.4.1", "editor", "bug"]
order: "d1"
---
# godoowave 1.4.1: Stored settings follow edits made during the solve

Review finding 1 (MINOR). The run does not take a copy of its settings. `Commit` (`WfcGeneratorPlugin.cs:473`) saves `_settings.Duplicate()` at commit time, but the inspector and the box handles (`_Forward3DGuiInput`, 816-858) stay editable while the solve runs.

- Randomise Seed off, Seed 5, Generate. During the animation type Seed 6, or drag a bounds handle. On commit the status says "Seed 5", but the settings stored on the GridMap say Seed 6 or the new bounds. Reopening the dialog no longer reproduces the scene (README: "the scene file can reproduce the result").
- During the run the overlay and handles show the new box while the preview fills the old one.

## Done when
- `OnActionPressed` puts a `Duplicate()` of the settings in `ActiveRun`, and `Commit` stores that copy.
- Handle drags are ignored while `_run != null`, as `OnPinRequested` and `OnClearRequested` already do.
- Engine test: change Seed and bounds on `_settings` after the run starts; the stored settings hold the run's values.
- Manual: both cases above in the editor.

## Result
- `ActiveRun` has a `Settings` copy, taken in `OnActionPressed` after Randomise Seed picked the run's seed. `Commit` stores that copy, so the GridMap gets the settings the solve started with; edits made during the solve stay in the dialog for the next Generate.
- `HandleTarget` is null while `_run != null`, so the handles are hidden and `_Forward3DGuiInput` ignores clicks, as `OnPinRequested`/`OnClearRequested` already refuse. A drag that loses its target is now cancelled (box put back) instead of left half-dragged without an undo step.
- The overlay and `BoxOrigin`/`BoxSize` read `Shown` (the run's copy during a solve), so the outline matches the box being filled. The run flag is part of the overlay state, so the gizmo redraws when a solve starts and ends.
- Engine test `WfcSettingsBoxTests.A_runs_settings_copy_keeps_its_values_when_the_dialog_settings_change`: Seed, both boxes, boundary and path socket changed on the dialog's settings; the copy keeps the run's values and shares the tile set. The plugin needs the editor's undo manager, so its wiring is checked manually.
- Build clean, 107 engine tests and 370 Core tests pass, headless editor load clean. CHANGELOG 1.4.1, README limitation line, plugin.cfg 1.4.1.

## Manual check
1. Randomise Seed off, Seed 5, Cells Per Frame low enough to watch (e.g. 200), Generate. During the animation type Seed 6 in the dialog. On commit the status says "Seed 5". Close and reopen the dialog: Seed is 5.
2. Generate again; during the animation try to drag a bounds handle: the handles are gone and the outline stays on the box being filled. Change Bounds Size in the inspector during the run: the outline does not move until the solve ends, and reopening the dialog shows the old size.
3. After the solve the handles come back and drag as before.

## Released
In godoowave 1.4.1 (tag `godoowave-v1.4.1`, PR #19).
