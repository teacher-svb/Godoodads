---
id: "dgn-1-3-1-doorway-socket-2026-11-24"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-24T18:00:00.000Z"
modified: "2026-11-24T19:00:00.000Z"
completedAt: null
labels: ["v1.3.1", "core", "resources"]
order: "eE"
---
# godoodungeon: Several path sockets and a Doorway Socket

Corridors use one Path Socket from door to door. Users want corridors built from several kinds of tile, such as `corridor` and `pitfall`, while only some of them may sit right outside a room's door (no pitfall straight outside the door). Brief §14.1.

## Scope
- **Path Socket** takes a comma-separated list, like tile faces (`corridor, pitfall`, spaces optional). A face is a corridor opening when any of its labels has one of these names. The corridor router, the cell restrictions, the tile set checks and the connectivity check all use the list.
- New **Doorway Socket** (`DungeonSettings.DoorwaySocket`, also a comma-separated list, empty by default): the corridor cell outside a door must have one of these names on its face towards the door. Empty means any path socket, as before.
- Doorway Socket names must be among the Path Socket names (error otherwise). A name in either list that no corridor tile uses gives a warning; no tile at all gives an error.
- The inspector text of both settings explains the format.
- Files: `Core/DungeonTileRules.cs`, `Core/SolveInputs.cs`, `Core/DungeonConnectivity.cs`, `Core/DungeonJob.cs`, `Resources/DungeonSettings.cs`, `Adapters/DungeonGridMap.cs`, `Adapters/DungeonTileSets.cs`, Core tests, README, CHANGELOG, brief.

## Done when
- One Path Socket name and an empty Doorway Socket give the same dungeon for the same settings and seed (existing tests unchanged).
- A tile set with `corridor` and `pitfall` pieces and Doorway Socket `corridor` never places a pitfall outside a door, over many seeds, and the dungeon stays connected.
- Missing pieces (no tile with the doorway socket facing a door with the needed path faces) give a readable error naming the door side.

## Result
- `DungeonTileRules.SocketNames` parses the lists (split on commas, trimmed, empty and repeated entries dropped); `PathFaces`, `StraightVariant`, `EmptyVariant`, the bend, T and cross checks and `DungeonConnectivity` count a face when any label has a listed name (`HasAnyName`). The string overloads stay, so callers that pass one name are unchanged.
- `SolveInputs.Corridors(..., doorwaySocket)` groups variants by (path face mask, door face mask). The door face mask of a cell holds the faces that look into a door, and is only set when Doorway Socket is not empty. A missing class names the path faces and the door side: "no piece with 'corridor' or 'pitfall' faces on +X and -X and 'pitfall' on -Z, which a corridor next to a door needs".
- `DungeonTileRules.Check` errors: Doorway Socket names that are not Path Socket names, no corridor tile with any doorway name, a side no piece turns a doorway name towards. Warnings: a listed path or doorway name no tile uses.
- `DungeonSettings.DoorwaySocket` (empty by default), placeholder text on both settings, passed through `DungeonRequest` and `DungeonTileSets`.
- Tests: new `tests/Core.Tests/DungeonDoorwaySocketTests.cs` (13 tests). Without a doorway socket the high-weight pitfall straight does stand outside doors; with Doorway Socket `corridor` it never does over 20 seeds, still appears elsewhere, and every dungeon stays connected. An empty doorway socket gives the same solved cells as before. Core 566/566, engine 156/156, headless editor load without errors.
- Docs: README settings table, CHANGELOG 1.3.1, brief §14.1, root README and CLAUDE.md; `plugin.cfg` 1.3.1.
- **Not verified:** the inspector in the editor (placeholder text, the new row below Path Socket) and a real tile set such as the Edugame corridors. The dialog has no tooltips, so the README and the placeholders carry the help text; the placeholder only shows while a field is empty.
