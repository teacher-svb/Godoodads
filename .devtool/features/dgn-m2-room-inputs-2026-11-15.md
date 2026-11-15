---
id: "dgn-m2-room-inputs-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "core"]
order: "c2"
---
# DGN M2: Room solve inputs

Per room: grid, `initial` array with door cells fixed to the door variant rotated outward, and `SolveOptions.Boundary` with the room edge socket on the side faces.

Brief: §4.4.

## Done when
- Unit tests with a small synthetic tile set: rooms solve closed for every door side; a door tile with two edge faces is rejected.
