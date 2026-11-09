---
id: "wfc-m2-dialog-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:39:21.000Z"
completedAt: "2026-11-09T18:39:21.000Z"
labels: ["editor"]
order: "aF"
---
# WFC M2: Generate dialog

`ToolDialog` with GridMap target, `WfcSettings` inspector (tile set, bounds, seed with randomise, clear bounds first, limits), Generate and Cancel, and a status area. Errors go to the status area and `Notify`. Store settings in node metadata.

Brief: §4.7, §5.

## Done when
- A generation can be run end to end, and reopening the dialog restores its settings.
- Status shows cells, time, backtracks, restarts and seed.

## Result
Dialog with tile set, seed, clear bounds first, bounds and limits, a Generate/Stop button, progress, settings stored in node metadata, and fixed-cell, failure and tile set error reporting. Headless load is clean. Needs the manual check.


## Fix from manual check (2026-11-09)
The MeshLibrary error fired on `wfc_sandbox.tscn`, whose GridMap has a built-in library, so the check was correct. The error now names both libraries. The check also accepts two loaded copies of one file, as a precaution.

## Result
Manual check passed (2026-11-09): generate, fixed-cell count, failure report with coordinates, and settings restored on reopen.
