---
id: "dgn-m3-failure-reporting-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:57:42.000Z"
completedAt: "2026-11-15T18:57:42.000Z"
labels: ["v1.0.0", "m3", "editor", "ui"]
order: "d5"
---
# DGN M3: Status and failure reporting

Result line, shortfall warnings, room and corridor failures with coordinates, WFC Generator's failure overlay, toaster messages, and restoring the seed after a failed randomised Generate.

Brief: §4.8, §5.

## Done when
- Manual check of each message with the prototype tile sets, recorded on this card.

## Results
Branch `godoodungeon-m1`.

- **Failure overlay.** After a failed Generate, each failed layout attempt's cell (from `AttemptFailure.Cell`: the room's failing cell, or the door whose corridor could not be routed) is drawn as a red box, slightly larger than the cell, in the bounds overlay. After an internal error (connectivity check), the unreachable rooms are marked the same way. The status says what the boxes mean. They are cleared by the next Generate, Clear Dungeon or opening the dialog. The overlay is never saved.
- **Results stay readable.** After a result, the next Preview Layout update (for example the seed put back after a failed randomised Generate) adds its line under the result instead of replacing it.
- **Toasts.** Only headlines are toasted: invalid settings, no layout in n attempts, internal error, the generator failing, the GridMap leaving the scene, and the WFC version warning. Details go to the status area only.
- **Seed.** A failed randomised Generate puts the seed back and does not pick the failed seed again while the dialog is open (as in WFC 1.3.2). Stopped or invalid runs also put it back.

## Messages checked (probe through the real dialog on `prototype_m2_dungeon.tscn` with the prototype tile sets)
```
PROBE 1 invalid:
        | The dungeon cannot be generated with these settings.
        | Choose the Door Tile from the room tile set.

PROBE 2 failed (wall): vertices=120 (24 = bounds only)
        | Waiting (0.0 s)
        | No layout could be generated in 4 attempt(s). The GridMap was not changed.
        | Layout 1: Corridors: No corridor could join room 1's door at (19, 0, 32) to room 7's door at (38, 0, 32): rooms, their clearance and hand-placed cells close every way.
        | Layout 2: Corridors: No corridor could join room 2's door at (20, 0, 8) to room 4's door at (38, 0, 6): rooms, their clearance and hand-placed cells close every way.
        | Layout 3: Corridors: No corridor could join room 6's door at (22, 0, 44) to room 8's door at (43, 0, 42): rooms, their clearance and hand-placed cells close every way.
        | Layout 4: Corridors: No corridor could join room 5's door at (24, 0, 4) to room 9's door at (34, 0, 4): rooms, their clearance and hand-placed cells close every way.
        | Red boxes in the viewport mark where each layout failed.
PROBE 3 randomised failure: seed after=2026 vertices=816

PROBE 3 randomised failure: seed after=2026 vertices=816
        | Generating…
        | No layout could be generated in 4 attempt(s). The GridMap was not changed.
        | Layout 1: Corridors: No corridor could join room 3's door at (34, 0, 35) to room 5's door at (22, 0, 32): rooms, their clearance and hand-placed cells close every way.
        | Layout 2: Corridors: No corridor could join room 2's door at (26, 0, 39) to room 3's door at (36, 0, 39): rooms, their clearance and hand-placed cells close every way.
        | Layout 3: Corridors: No corridor could join room 3's door at (34, 0, 36) to room 7's door at (20, 0, 33): rooms, their clearance and hand-placed cells close every way.
        | Layout 4: Corridors: No corridor could join room 4's door at (26, 0, 4) to room 5's door at (36, 0, 4): rooms, their clearance and hand-placed cells close every way.
        | Red boxes in the viewport mark where each layout failed.
        | Seed 616928666 failed, so Seed is back at 2026, the seed before this Generate. The next Generate tries a new seed.

PROBE 4 success:
        | Generated 10 rooms, 16 doors and 10 corridors in 0.0 s. Seed 2026.
        | Placed 10 of 12 rooms: 2 found no free place. Enlarge the bounds, or lower the room count, the room sizes or the gap.
        vertices=24
```
- Red boxes: 120 overlay vertices after the failed run (the bounds plus 4 boxes, one per attempt), back to 24 after the next successful Generate.
- Already checked on earlier cards: stopped ("Generation stopped. The GridMap was not changed."), Clear Dungeon, nothing to clear, the GridMap leaving the scene (same code as WFC Generator).

## Manual check (board)
Place a line of crates across the bounds of `prototype_m2_dungeon.tscn` so the rooms cannot all be joined, then Generate. The status lists 4 failed layouts and red boxes mark the doors. Remove the crates and Generate: the boxes go.

## Board check passed (2026-11-15)
With a line of crates across the bounds, the status lists the 4 failed layouts by door and red boxes mark them. After removing the crates, Generate succeeds and the boxes go.
