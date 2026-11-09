---
id: "wfc-m1-sockets-2026-11-09"
status: "backlog"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["core"]
order: "a1"
---
# WFC M1: Socket parsing and matching

Parse and match labels: `path`, `path+` / `path-`, `stair`, `stair@0` to `stair@3`. Empty or malformed labels are validation errors.

Brief: §4.2.

## Done when
- Unit tests cover every row of the §4.2 table, plus non-matches and invalid labels.
