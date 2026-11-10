---
id: "wfc-1-3-path-editor-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T13:00:00.000Z"
completedAt: null
labels: ["editor", "adapters"]
order: "e3"
---
# WFC 1.3: Guided paths in the editor

The Paths group in `WfcSettings` (Path Socket, Path Wander), routing before the solve in the dialog, errors in the status area, and route cells written as generated cells (regenerate routes again, Pin keeps them).

Brief: §13.3.

## Done when
- Engine tests: settings round-trip; regenerate re-routes; pinned path cells become endpoints.
- Manual: place two ends in the demo, generate, regenerate, pin, undo.
