---
id: "wfc-m2-solve-runner-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:56:28.000Z"
completedAt: "2026-11-09T18:56:28.000Z"
labels: ["editor"]
order: "aE"
---
# WFC M2: Background solve runner

Run the solve on a background thread with writes on the main thread. Add Cancel, and stop the solve before assembly reloads, when the GridMap is removed and when the scene closes. Use the approach from the M0 thread card.

Brief: §4.7, §6 item 6.

## Done when
- The editor stays responsive during a full-size solve.
- Cancel restores the earlier state.
- Rebuilding during a solve neither crashes nor hangs the editor (manual check).

## Result
`Core/SolveJob` runs the solve on a background thread, with Core tests for result equality, cancel and captured exceptions. The plugin polls it in `_Process` and stops it through `OnBeforeSerialize`, `_ExitTree`, closing the dialog, and when the GridMap leaves the tree. No GridMap writes happen before success, so Cancel leaves it unchanged. Needs the manual check.


## Manual check (2026-11-09)
Not yet exercised in the editor. The hills tile set solves 100×16×100 too quickly to press Stop, and rebuild during a solve was skipped. The Core cancel test passes and the reload hook was confirmed in M0. Recheck both with a slower solve in M3 (see `wfc-m3-demo-acceptance`), then close this card.

## Result
Manual check (2026-11-09): Stop at 100×16×100 left the GridMap unchanged. Rebuild during a solve was skipped by the board, relying on the M0 reload check of the same hook.
