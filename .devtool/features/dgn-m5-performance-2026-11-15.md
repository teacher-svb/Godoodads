---
id: "dgn-m5-performance-2026-11-15"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:20:16.000Z"
completedAt: null
labels: ["v1.0.0", "m5", "performance"]
order: "f2"
---
# DGN M5: Performance

Timings for 64×64 with 20 rooms and for a large case (128×128, 60 rooms) on the board's dev machine, split by stage.

Brief: §6.4, §8.

## Done when
- Timings are recorded on this card; the target is met or the board accepts the gap.

## Results
Branch `godoodungeon-m1`. `DungeonResult.Times` (new) records milliseconds per stage, summed over the layout attempts: layout, rooms, routing, corridors (solve) and check.

**Core** (`DungeonPerformanceTests`, category Performance, prototype tile rules, rooms 5–11, 10 seeds each, this machine):

| Case | Solved | Per dungeon | Layout | Rooms | Routing | Corridors | Check | Slowest |
|---|---|---|---|---|---|---|---|---|
| 64×64, 20 rooms | 10 of 10 | **10 ms** | 0.5 | 4.3 | 2.1 | 2.6 | 0.1 | 11 ms |
| 128×128, 60 rooms | 10 of 10 | **48 ms** | 7.1 | 15.8 | 13.5 | 9.5 | 0.6 | 95 ms |

**Engine** (`DungeonDemoTests.Generate_and_write_timings`, demo tile sets, 5 seeds, each run replacing the previous dungeon):

| Case | Generate | Write (diff, cells, record) | Cells |
|---|---|---|---|
| 64×64, 20 rooms | 12 ms | 8 ms | 974 |
| 128×128, 60 rooms | 41 ms | 15 ms | 3,440 |

The target (under 10 s at 64×64) is met by a factor of about 500. Headless runs use a dummy renderer, so the GridMap's mesh rebuild after the write is not included. That needs the board's machine.

## Manual check (board)
On your machine, open `demo/godoodungeon/dungeon_free.tscn`, set Bounds Size to 128 × 1 × 128 and Room Count to 60, and Generate. The status line gives the time. Note it here, and whether the editor stays responsive while the GridMap redraws.
