---
id: "dgn-m2-settings-resource-2026-11-15"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "resources"]
order: "c1"
---
# DGN M2: DungeonSettings resource

`DungeonSettings` (`[Tool, GlobalClass]`) with groups for tile sets (room, corridor, room edge socket, door tile, path socket, path wander), bounds, layout, seed (with Randomise Seed) and limits. Validation of §4.2 against the compiled tile sets. Stored in GridMap metadata under its own key.

Brief: §4.2, §4.6.

## Done when
- Engine tests: each §4.2 validation error is reported; settings survive a save and reload of the scene.
