---
id: "wfc-1-1-dialog-status-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T09:01:43.000Z"
completedAt: "2026-11-10T09:01:43.000Z"
labels: ["editor"]
order: "c5"
---
# WFC 1.1: Status for regenerate and weights

Report how many previously generated cells were replaced and how many edited cells were kept. Warn when a tile set has weight-0 tiles that are never generated, as information only.

Brief: §11.

## Done when
- Status lines appear as described (manual check).

## Result
- Success status adds "Replaced N cells from the previous generation." and "Kept N existing cells, including M generated cells you edited since."
- When tiles have weight 0, an info line says: "N tile(s) have weight 0: they are only used where you place them by hand."
- Waiting on: the manual check.

## Result
- Manual check passed (2026-11-10): the Replaced, Kept/edited and weight-0 lines all appear.
