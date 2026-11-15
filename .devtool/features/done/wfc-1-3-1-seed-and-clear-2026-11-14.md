---
id: "wfc-1-3-1-seed-and-clear-2026-11-14"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC Generator"
dueDate: null
created: "2026-11-14T18:00:00.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-14T19:10:00.000Z"
labels: ["v1.3.1", "resources", "adapters", "editor", "ui"]
order: "e0"
---
# WFC 1.3.1: Randomise Seed checkbox and Keep Placed Cells

Requested by the user on 2026-11-14.

1. **Randomise Seed** becomes a checkbox instead of a button. On: the Seed row is read-only and Generate picks a new seed before solving (the status line and stored settings show it). Off: the seed is editable and Generate keeps using it.
2. **Clear Bounds First** gets a **Keep Placed Cells** option, shown only while Clear is on. On: everything generated is cleared, including pinned cells, while hand-placed cells and generated cells edited since (both "placed by you", README) stay fixed. Off: everything is cleared, as before.

Both default to off, so settings stored on GridMaps by 1.3.0 behave as before.

## Done when
- `WfcSettings.RandomiseSeed` (bool) replaces the tool button; `Seed` is read-only while it is on.
- `WfcSettings.KeepPlacedCells` (bool), hidden while Clear Bounds First is off.
- `GridMapCells.ReadFixed` can treat pinned cells as free; the status reports cleared pins.
- `RecordAfter` drops any old entry (also a pinned one) on a cell that was free in this solve, so no stale pin is left where the new solve wrote an empty tile.
- Tests: clear with Keep Placed Cells, record after cleared pins, property visibility and read-only state.
- README (Options table, placed-cells section) and CHANGELOG 1.3.1 updated; brief note.
- `dotnet build` without new warnings, Core and engine tests pass, headless editor load clean.
- Manual: checkbox and read-only seed in the dialog, a new seed per Generate, Keep Placed Cells shown and hidden with Clear, hand-placed tiles kept and pins cleared.

## Result
- `WfcSettings`: `RandomiseSeed` (bool, before Seed) replaces the tool button; `PickNewSeed()` sets a random seed different from the current one. `_ValidateProperty` makes Seed read-only while Randomise Seed is on, and keeps `KeepPlacedCells` storage-only while Clear Bounds First is off. Both setters call `NotifyPropertyListChanged` on change.
- Plugin: Generate picks the new seed after validation, right before the solve, so the run, the status line and the stored settings use it. Clear + Keep Placed Cells calls `ReadFixed(..., clearPins: true)`; the "Replaced" status line adds "including N pinned cells". The fixed-cell conflict message now points at Clear Bounds First without Keep Placed Cells.
- `GridMapCells.ReadFixed`: `clearPins` frees matching pinned cells (counted in `Replaced` and the new `FixedCells.ClearedPins`). Edited and hand-placed cells stay fixed.
- `RecordAfter` removes the old entry of every free cell before writing, so a cleared pin solved as an empty tile leaves no stale pinned entry.
- README (placed-cells section, Options table), CHANGELOG 1.3.1 (Unreleased), brief §13.6. `plugin.cfg` stays 1.3.0 until the release.
- Tests: 4 new engine tests in `WfcRegenerateTests` (clear with Keep Placed Cells, cleared pin record, seed read-only + `PickNewSeed`, Keep Placed Cells visibility). Core 250 pass, engine 79 pass. `dotnet build` 0 warnings; headless editor load clean.
- Waiting for the manual editor check (see Done when).

Release approved by the user on 2026-11-14 (PR for 1.3.1). The manual editor check was not reported separately.

Release prep: `plugin.cfg` 1.3.1, CHANGELOG 1.3.1 dated 2026-11-14, suite README version and example zip, CLAUDE.md. Tag `wfc_generator-v1.3.1` after merge.
