---
id: "wfc-m2-solve-runner-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M2 GridMap pipeline"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
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
