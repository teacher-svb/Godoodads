---
id: "dgn-1-0-1-run-settings-snapshot-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-16T09:45:59.000Z"
modified: "2026-11-16T10:34:32.000Z"
completedAt: null
labels: ["v1.0.1", "editor", "bug"]
order: "d6c"
---
# godoodungeon 1.0.1: Stored settings follow edits made during the generation

Found while fixing `dgn-1-0-1-box-undo-target-2026-11-16`: the same defect as godoowave's `wave-1-4-1-run-settings-snapshot-2026-11-16`. `Commit` (`GodoodungeonPlugin.cs`) saved `_settings.Duplicate()` at commit time, but the inspector and the bounds handles stay editable while the job runs.

- Randomise Seed off, Seed 5, Generate. While it runs, type Seed 6 or drag a bounds handle. On commit the status says "Seed 5", but the settings stored on the GridMap say Seed 6 or the new bounds, so reopening the dialog no longer reproduces the dungeon.

## Done when
- `OnActionPressed` puts a `Duplicate()` of the settings in `ActiveRun`, and `Commit` stores that copy.
- Handle drags are ignored while `_run != null`.
- Engine test: change Seed and bounds on the dialog's settings after the copy; the copy holds the run's values.
- Manual: the case above in the editor.

## Result
- `ActiveRun` has a `Settings` copy taken after Randomise Seed picked the run's seed; `Commit` stores it.
- `HandleTarget` is null while a run is active, so the handles are hidden and clicks pass through; the run flag is part of the overlay state, so the gizmo redraws when a generation starts and ends.
- Unlike godoowave, the outline and Preview Layout keep showing the dialog's settings during a run: there is no cell-by-cell preview to match, and both describe the next Generate.
- Engine test `DungeonSettingsTests.A_runs_settings_copy_keeps_its_values_when_the_dialog_settings_change`. Build clean, 109 engine tests pass. CHANGELOG Unreleased entry.

## Manual check
1. Randomise Seed off, Seed 5, on `demo/godoodungeon/dungeon_free.tscn` with enough rooms that Generate takes a moment. While it runs, type Seed 6: on commit the status says "Seed 5"; reopen the dialog: Seed is 5.
2. While a generation runs the bounds handles are gone; they come back afterwards.

## Review (stern-reviewer, 2026-11-16)
- MINOR, not fixed in code: the engine test checks `Duplicate()`, not the plugin. If `Commit` went back to storing `_settings.Duplicate()`, the test would still pass. The plugin wiring is covered only by the manual check.
