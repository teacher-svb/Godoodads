---
id: "wfc-1-3-2-failed-seed-and-clear-2026-11-14"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-14T20:15:00.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-14T21:05:00.000Z"
labels: ["v1.3.2", "resources", "adapters", "editor", "ui"]
order: "e1"
---
# WFC 1.3.2: Keep the last good seed after a failure, and a Clear Bounds button

Requested by the user on 2026-11-14.

1. **Failed seed.** With Randomise Seed on, a Generate that does not succeed (no solution, out of attempts, path errors, solver error, stopped or cancelled) puts Seed back to the seed it had before, which matches the GridMap. The failed seed is remembered while the dialog is open, so the next Generate never picks it again.
2. **Clear Bounds button** next to Clear Bounds First, with the same rules: Keep Placed Cells off removes every cell in the bounds; on, it removes the generated cells (pinned too) and keeps hand-placed and edited cells. Tagged markers on cleared cells go too. One undo step with the cells, the regenerate record and the markers. Needs no tile set.

## Done when
- Seed is restored on every non-solved outcome and on stop/cancel; the status names the failed seed and the restored one.
- `PickNewSeed` avoids the current seed and the seeds that failed.
- `GridMapCells.Clear` plans the cleared cells; the record loses its entries inside the bounds.
- Tests: Clear with and without Keep Placed Cells (cells, counts, undo data, record), markers on cleared cells, `PickNewSeed` avoiding seeds.
- README, CHANGELOG 1.3.2 (Unreleased), brief §13.7.
- Build without warnings, tests pass, headless editor load clean.
- Manual: a failing randomised Generate shows the old seed and the next one tries a new seed; Clear Bounds in both modes, and undo.

## Result
- Plugin: `ActiveRun.PreviousSeed` holds the seed before Randomise Seed picked the run's. `StopRun` (Stop, closing the dialog, rebuild, target gone) and a finished run that did not solve call `RestoreSeed`, which puts Seed back unless the user changed it since. Failed runs (no solution, out of attempts, path errors, solver error) add the seed to `_failedSeeds`, which `PickNewSeed(avoid)` skips; a cancelled run does not. The status adds "Seed X failed, so Seed is back at Y …". `_failedSeeds` is cleared when the dialog opens.
- `WfcSettings`: `ClearRequested` signal and a **Clear Bounds** tool button after Keep Placed Cells. Keep Placed Cells is always shown now (it applies to the button too), so the 1.3.1 hiding is gone and Clear Bounds First is a plain property again.
- `GridMapCells.Clear` plans the removal: every occupied cell, or with Keep Placed Cells only cells that still match the record (pinned too). Returns the do/undo changes, a Free/Excluded mask for `GridMapMarkers.Replaced`, and counts. `RecordAfterClear` drops all record entries in the bounds.
- `OnClearRequested`: bounds checks, one "Clear WFC Bounds" undo action with cells, record and marker removals (the removal code is shared with Generate via `AddMarkerRemovals`). Reports cleared, kept, pinned and marker counts, or "Nothing to clear".
- README (placed-cells section, Options table), CHANGELOG 1.3.2 (Unreleased), brief §13.7.
- Tests: `WfcRegenerateTests` replaces the 1.3.1 visibility test and adds 5 cases (seed avoidance over 200 picks, Keep Placed Cells always shown, Clear all with undo and record, Clear with Keep Placed Cells and without a record, markers on cleared cells). Core 250, engine 83 pass. Build 0 warnings; headless editor load clean.
- The seed restore lives in the plugin and is not covered by headless tests: manual check.

Release approved by the user on 2026-11-14 ("let's publish it"). The manual editor check was not reported separately.

Release prep: `plugin.cfg` 1.3.2, CHANGELOG 1.3.2 dated 2026-11-14, suite README version and example zip, CLAUDE.md. Tag `wfc_generator-v1.3.2` after merge.
