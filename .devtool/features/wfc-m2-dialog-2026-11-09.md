---
id: "wfc-m2-dialog-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["editor"]
order: "aF"
---
# WFC M2: Generate dialog

`ToolDialog` with GridMap target, `WfcSettings` inspector (tile set, bounds, seed with randomise, clear bounds first, limits), Generate and Cancel, and a status area. Errors go to the status area and `Notify`. Store settings in node metadata.

Brief: §4.7, §5.

## Done when
- A generation can be run end to end, and reopening the dialog restores its settings.
- Status shows cells, time, backtracks, restarts and seed.
