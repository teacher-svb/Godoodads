---
id: "dgn-m2-settings-resource-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T16:12:16.000Z"
completedAt: "2026-11-15T16:12:16.000Z"
labels: ["v1.0.0", "m2", "resources"]
order: "c1"
---
# DGN M2: DungeonSettings resource

`DungeonSettings` (`[Tool, GlobalClass]`) with groups for tile sets (room, corridor, room edge socket, door tile, path socket, path wander), bounds, layout, seed (with Randomise Seed) and limits. Validation of §4.2 against the compiled tile sets. Stored in GridMap metadata under its own key.

Brief: §4.2, §4.6.

## Done when
- Engine tests: each §4.2 validation error is reported; settings survive a save and reload of the scene.

## Results
Branch `godoodungeon-m1` (M2 continues on it, as the board decided).

- `Resources/DungeonSettings.cs` (`[Tool, GlobalClass]`, `Godoodads.Dungeon`). Groups:
  - **Tile Sets:** Room Tile Set, Corridor Tile Set, Room Edge Socket (`outside`), Door Tile, Path Socket (`path`), Path Wander. Door Tile is a drop-down of the room tile set's tile names, stored as the index; it updates when the room tile set changes.
  - **Bounds:** origin, and size 48 × 1 × 48 (Y holds the room and corridor layers).
  - **Rooms:** count, min and max size, height, gap, margin.
  - **Corridors:** Wall Clearance, Extra Connections, Max Doors Per Room, Corridor Height.
  - **Seed:** Randomise Seed (Seed becomes read-only while it is on) and Seed.
  - **Limits:** Layout Attempts (4), Attempts Per Room, Max Backtracks, Max Restarts.
  - Also: `ToLayoutSettings()` (bounds X and Z as the layout grid), `CheckBounds()` (room and corridor height against the bounds' Y, layout attempts), `PickNewSeed`, and `StoredOn`/`StoreOn` under the metadata key `godoodads_dungeon_settings`.
- `Core/DungeonTileRules.cs`: the §4.2 rules on compiled tile sets.
  - The edge socket must be one-sided: an error for a plain `outside` (naming the tiles), for `outside-` on tiles, and when no tile has `outside+`.
  - The door tile must be chosen and have exactly one `outside+` face. A weight above 0 is a warning.
  - Empty socket names are errors. The path socket must be used. Straights must exist along X and along Z. An empty tile with a weight above 0 must exist.
  - Lookups for the solve stage: `DoorVariant(outward)`, `StraightVariant(axis)`, `EmptyVariant`, `PathFaces`.
- `Adapters/DungeonTileSets.cs`: `Check(settings, map)` binds both tile sets and reports, in one list:
  - Missing tile sets, different MeshLibraries (between the tile sets, or against the GridMap), and binding errors.
  - The §4.2 rules, the layout validation and the bounds checks.

Checks:
- Core tests **327 passed**: 11 new in `DungeonTileRulesTests`, one per rule, plus the lookups for every door side, both straights and the empty tile.
- Engine tests **91 passed**: 6 new in `tests/godoodungeon/DungeonSettingsTests`:
  - Valid prototype-style settings bind with no errors or warnings.
  - Each §4.2 error comes through `DungeonTileSets.Check`, and the weighted door gives a warning.
  - Library, height and gap errors.
  - The Door Tile drop-down's hint string.
  - **The settings survive a save and reload of a scene**, in GridMap metadata, with every field and both tile sets intact.
- Build: 0 warnings. Headless editor load: clean.
- The inspector look (groups, drop-down) is checked by hand with the dialog in M3.
