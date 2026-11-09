---
id: "wfc-solver-sandbox-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:50:59.000Z"
modified: "2026-11-09T16:52:15.884Z"
completedAt: "2026-11-09T16:52:15.884Z"
labels: ["demo"]
order: "Zy"
---
# WFC: Solver sandbox scene

Requested by the board to try the solver before the dialog (M2) and the demo tileset (M3) exist. `demo/wfc_generator/wfc_sandbox.tscn` and `WfcSandbox.cs` build a hill-and-ramp tile set in code and write the solve into a GridMap. Temporary: replace or remove it once M3 ships real demo scenes.

## Result
Headless: 10 of 10 seeds solved at 24×8×24 with 0 backtracks, and 100×16×100 solved in 0.21 s. A windowed screenshot of seed 1 shows continuous terrain with correctly rotated ramps and corners. Grass was rare (38 of 3,395 placed blocks), which shows the expected lean towards tiles with many rotations.

The inspector buttons and Keep Existing Cells have not been tried in the editor. Generate has no undo here.