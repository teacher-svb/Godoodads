---
id: "wfc-m1-variants-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:42:27.000Z"
completedAt: "2026-11-09T16:42:27.000Z"
labels: ["core"]
order: "a2"
---
# WFC M1: Rotated variants

Generate Y-rotated variants for rotation modes None, 2-way and 4-way, rotating horizontal faces and vertical rotation indices. Merge variants that are identical after rotation.

Brief: §4.1, §6 item 4.

## Done when
- Unit tests cover each rotation mode and the merging of symmetric tiles.

## Result
Implemented in `CompiledTileSet`: rotation modes, identical-rotation merging, per-face validation, and rotation lookup for fixed cells.
