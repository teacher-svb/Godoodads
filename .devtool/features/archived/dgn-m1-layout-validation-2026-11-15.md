---
id: "dgn-m1-layout-validation-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T15:57:01.000Z"
completedAt: "2026-11-15T15:57:01.000Z"
labels: ["v1.0.0", "m1", "core"]
order: "b5"
---
# DGN M1: Layout validation and messages

Errors before placing rooms (a room size that does not fit the bounds, gap below 2 × Wall Clearance + 1, room count below 2, minimum above maximum) and layout failures with readable messages for the status area.

Brief: §4.8.

## Done when
- Unit tests for each error and for the shortfall warning text.

## Results
Branch `godoodungeon-m1`.

- `Core/LayoutGenerator.cs`:
  - `LayoutValidator.Validate(settings)` lists every problem, each naming the setting as the dialog will show it and saying what to change:
    - Room Count below 2, Room Height below 1.
    - Min Room Size below 3 × 3 (a door needs a non-corner wall cell), Min above Max (the axis is named).
    - Wall Clearance or Margin below 0, Gap below `MinimumGap` (with the needed value and why).
    - Extra Connections outside 0 to 1, Max Doors Per Room or Attempts Per Room below 1.
    - Empty bounds, and the smallest room plus margins larger than the bounds (checked once the other numbers are sane).
  - `LayoutGenerator.Generate(settings, seed, attempt, origin, occupied)`: validate, then place, graph and doors on the attempt's seed (`LayoutSeeds.Layout`). Fewer than 2 placed rooms is an error after the shortfall warning.
- `DungeonLayout.Origin` and `GridCell(cell)`: layout messages name GridMap cells (layout Y is GridMap Z, on the bottom layer of the bounds). The door error says why: every side faces the bounds edge, a hand-placed cell or another room too closely.

Checks: Core tests **309 passed** (17 new in `DungeonLayoutValidationTests`: defaults valid, each error with its message, several problems reported together, the generator stopping on invalid settings, 50 seeds valid with a door per room, attempts that differ and repeat, the shortfall warning then the fewer-than-2 error, GridMap cells in messages from the origin). Build: 0 warnings.
