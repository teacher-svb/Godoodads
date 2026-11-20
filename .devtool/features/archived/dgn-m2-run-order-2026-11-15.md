---
id: "dgn-m2-run-order-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T16:28:27.000Z"
completedAt: "2026-11-15T16:28:27.000Z"
labels: ["v1.0.0", "m2", "core"]
order: "c4"
---
# DGN M2: Dungeon run: order, seeds and retries

A `DungeonJob` that runs layout, rooms and corridors on a background thread with derived seeds, room retries and Layout Attempts, honours cancellation, and returns one combined result with the cells, stats and any failure.

Brief: §4.5.

## Done when
- Unit tests: the same seed gives the same cells; a forced room failure retries; cancellation stops within one solve.

## Results
Branch `godoodungeon-m1`.

- `Core/DungeonJob.cs`:
  - **`DungeonRequest`** is plain data the thread can read: layout settings, origin, corridor height, both compiled tile sets, door tile, sockets, wander, seed, layout attempts, solver limits, and the hand-placed GridMap cells.
  - **`DungeonJob`** has `Start` (a background thread), `RunNow` (the same work on the calling thread, for tests), `Cancel`, `Wait`, `IsFinished`, `Stage` (for example "Layout 1: room 3 of 12") and `Error`.
  - **Order.** Validate (settings and tile sets, giving `Invalid`). Then for each layout attempt: `LayoutGenerator` with `LayoutSeeds.Layout`, one room solve each with `LayoutSeeds.Room`, then the corridor solve with `LayoutSeeds.Corridors`.
  - **Solves.** Each runs as a `SolveJob`, so a cancel stops it mid-solve, and the corridor solve keeps guided paths' 8 route attempts.
  - **Retries.** A room that is still not solved after the solver's restarts, a corridor plan error, or an unsolved corridor solve starts the next layout attempt. Each failure is recorded as an `AttemptFailure` (message and GridMap cell). Errors that do not depend on the attempt (bad door tile, corridor tile set) return `Invalid` at once.
  - **Hand-placed cells** are projected onto the layout grid within the room and corridor layers for placement, and excluded as GridMap cells in the corridor solve.
  - **`DungeonResult`**: status (Solved, Invalid, Failed, Cancelled), the layout, the solves (rooms, then corridors, each with input and cells), the failed attempts, the layout attempt used, backtracks, restarts and route stats.

Checks: Core tests **350 passed**, 7 new in `DungeonJobTests`:
- The same seed gives the same layout and cells, and another seed differs.
- There is one solve per room plus the corridors, at the right origins.
- **A forced room failure** (corners with weight 0) is retried on all 3 layout attempts, then fails with "Room 1 at … could not be solved" and a cell per attempt.
- **A real corridor routing failure** within 150 seeds is replaced by the next layout attempt, and the result reports it.
- **Cancelling** a 200×200, 120-room run 30 ms in stops it with status Cancelled, well within the 5 s limit.
- Invalid settings, a missing tile set and a corner as the door stop before any layout. Hand-placed cells are avoided by rooms and excluded from corridors, and cells far above the dungeon layers are ignored.
