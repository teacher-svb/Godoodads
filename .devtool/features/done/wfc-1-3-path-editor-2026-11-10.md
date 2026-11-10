---
id: "wfc-1-3-path-editor-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T14:11:33.000Z"
completedAt: "2026-11-10T14:11:33.000Z"
labels: ["editor", "adapters"]
order: "e3"
---
# WFC 1.3: Guided paths in the editor

The Paths group in `WfcSettings` (Path Socket, Path Wander), routing before the solve in the dialog, errors in the status area, and route cells written as generated cells (regenerate routes again, Pin keeps them).

Brief: §13.3.

## Done when
- Engine tests: settings round-trip; regenerate re-routes; pinned path cells become endpoints.
- Manual: place two ends in the demo, generate, regenerate, pin, undo.

## Result
Paths group in `WfcSettings` (Path Socket, Path Wander, `HasPaths`). `SolveJob` takes an optional `PathRequest` and lays out the paths on its thread before solving (`Plan`; no result when the plan has errors). The router names GridMap coordinates (origin offset) and counts placed path cells. The plugin reports path errors and marks their cells red, says how many routes and cells were laid, warns when paths are on without placed path cells, and adds a hint when the routes were laid but the solve failed. Engine tests (`WfcPathTests`): settings round-trip; regenerate routes again; pinning keeps the whole path (one closed group, no routes); error coordinates. Engine 60 pass. The manual check waits for the hillside path tiles.

## Result
Manual check passed (2026-11-10): generate on hillside_paths joins both ends with a climbing path and reports the route; seeds and wander change the route; regenerate replaces it; pinned stretches stay; an erased end gives "leads nowhere" with a red cell and no change; works after a reload.
