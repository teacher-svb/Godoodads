---
id: "wfc-idea-tile-weights-2026-11-09"
status: "backlog"
priority: "high"
assignee: null
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T22:55:00.000Z"
modified: "2026-11-09T22:55:00.000Z"
completedAt: null
labels: ["idea", "core"]
order: "b0"
---
# WFC 1.1: Per-tile weights

Let each tile set how likely it is, so density no longer depends on copies of tiles.

**Size:** S

## Design notes
- Add a `Weight` (float, default 1) to `WfcTile` and `TileDefinition`. Each variant gets its tile's weight, so rotations no longer count extra unless the author wants them to (open question below).
- Collapse picks a variant in proportion to weight. Cell choice can stay "fewest options", or move to weighted entropy (Shannon). Compare both on the demo.
- Determinism must hold: same seed, same result.
- Rebuild the demo without its 40 + 40 air copies. Expected: about 144 variants down to about 60, and a faster solve.
- The README sections on density and limitations need rewriting.

## Open questions
- Is a weight per tile, or per variant (so rotations share it)?
- Weighted entropy or fewest options?
