---
id: "dgn-m4-demo-tilesets-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:15:47.000Z"
completedAt: "2026-11-15T19:15:47.000Z"
labels: ["v1.0.0", "m4", "demo"]
order: "e2"
---
# DGN M4: Room and corridor demo tile sets

`dungeon_room_tileset` and `dungeon_corridor_tileset` with sockets, weights and markers (a torch marker on some walls, a spawn marker in some room cells), built by the same build scene.

Brief: §4.2.

## Done when
- Both tile sets pass the tile set check; their preview shows no red faces.

## Results
Branch `godoodungeon-m1`.

- `dungeon_room_tileset.tres`: Floor (12), Floor Cracked (3), Floor Spawn (0.25, spawn marker), Crate (0.8), Barrel (0.8), Pillar (0.3), Wall (4), Wall Torch (0.6, torch marker), Wall Banner (0.6), Corner (1), Door (0). Edge socket `outside+`, "in" between interior cells, "edge" along walls, "v" vertically. External Sockets: `outside-`.
- `dungeon_corridor_tileset.tres`: Corridor (3), Corridor Torch (0.4, torch marker), Bend (1), T (1), Cross (1), Empty (20). Path socket `path`, closed sides `x`.
- **Finding: red faces in the shared preview.** The shared tile set check (and so the preview) reported every `outside+` face as having no partner: by design its partner `outside-` comes from the room bounds, and no tile carries it. Board decision (2026-11-15): **External Sockets** on `WfcTileSet`, sockets supplied from outside the tiles, which the check, the edge-only analysis and the preview count as partners. This is additive shared code: an empty list changes nothing, and generation ignores it. godoodungeon warns when the room tile set lacks `<edge>-`. The prototype room tile set got it too, and both prototype builders were rerun (unchanged results). WFC Generator CHANGELOG (Unreleased) and README, and brief §4.2, are updated.
- Checks: `DungeonTileSets.Check` reports no errors or warnings for the demo tile sets, and the WFC check for both reports no warnings (the preview uses the same face test). Core tests **360 passed** (an external-sockets checker test). Engine tests **99 passed** (the missing-external warning and a bad label).

## Manual check (board)
Open `dungeon_room_tileset.tres` and `dungeon_corridor_tileset.tres` in the inspector and browse the tiles in the preview: no red face labels.

## Board check passed (2026-11-15)
The tile set preview shows the pieces as intended with no red face labels. Both demo scenes generate good, dungeon-like layouts over several seeds, and the statues and pool stay untouched.
