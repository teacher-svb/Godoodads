---
id: "dgn-m1-room-placement-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T13:41:24.000Z"
completedAt: "2026-11-15T13:41:24.000Z"
labels: ["v1.0.0", "m1", "core"]
order: "b2"
---
# DGN M1: Room placement

Seeded room placement with sizes in range, gap, margin and occupied cells as obstacles, a fixed number of attempts per room, and a shortfall warning.

Brief: §4.3, §4.7.

## Done when
- Unit tests: no overlaps or gap violations over 200 seeds; occupied cells are avoided; the same seed gives the same rooms; a crowded setup reports the shortfall.

## Results
Branch `godoodungeon-m1`.

- `Core/RoomPlacer.cs`: `Place(layout, settings, seed, occupied)`. Per room, up to `AttemptsPerRoom` candidates, each with a random size in range (drawn again on every attempt, so a smaller size can still fit) and a random position inside the margin. A candidate is rejected when its footprint grown by the gap overlaps a placed room or an occupied cell (brief §4.3: the gap applies to hand-placed cells too, so a corridor fits between them). When a room finds no place, placement stops and the layout gets the warning "Placed n of m rooms: …". The occupancy grid is only read. Randomness: `WfcRandom` on the given seed (the layout attempt's seed from `LayoutSeeds`).
- `Core/LayoutText.cs`: a top-down text picture (`#` wall, `.` floor, `D` door, `X` occupied), for tests and the M1 review.

Checks: Core tests **277 passed** (7 new in `DungeonRoomPlacementTests`):
- No overlaps, gap, margin or size violations over 200 seeds (defaults: 48×48, 12 rooms), with more than 11 rooms per seed on average.
- Cells within the gap of hand-placed cells (a line and a block) stay free over 100 seeds, and the grid is not changed.
- The same seed gives the same rooms, and the next seed differs.
- A 24×24 grid with 20 rooms reports the shortfall, while 96×96 places all 12 without a warning.
- Rooms too big for the bounds: 0 placed, with the warning. Fixed sizes are used as given.
- The text picture of seed 1 with a 4×4 hand-placed block (shown in the test output).
