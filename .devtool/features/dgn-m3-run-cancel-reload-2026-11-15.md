---
id: "dgn-m3-run-cancel-reload-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m3", "editor"]
order: "d4"
---
# DGN M3: Background run, cancel and reload

Generate runs the `DungeonJob` in the background with progress per stage. Cancel restores the earlier state. The run stops before an assembly reload, when the GridMap is removed and when the scene closes.

Brief: §4.5.

## Done when
- Manual checks: cancel mid-run; rebuild during a run; close the scene during a run. None leave cells behind or hang the editor.
