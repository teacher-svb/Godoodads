---
id: "dgn-m2-gridmap-write-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T16:47:30.000Z"
completedAt: "2026-11-15T16:47:30.000Z"
labels: ["v1.0.0", "m2", "adapters"]
order: "c6"
---
# DGN M2: GridMap read, write, record and undo

Read the hand-placed cells, clear the cells of the previous dungeon record, write the result through the tile set bindings, store the record under its own metadata key, and place markers, all as one undo action.

Brief: §4.6, §4.7.

## Done when
- Engine tests: hand-placed cells are unchanged; regenerate replaces only recorded cells; undo restores the bounds (asserted on cells); the MeshLibrary and tile sets are unchanged.

## Results
Branch `godoodungeon-m1`.

- `Adapters/DungeonGridMap.cs`:
  - **The record** is stored under `godoodads_dungeon_generated`, separate from WFC Generator's (`StoredRecord`, `ReadRecord`, `WriteRecord`). Empty data removes the entry.
  - **`ReadHandPlaced`**: occupied cells in the bounds box that are not unchanged record cells. Edited generated cells count as hand-placed (brief §4.6).
  - **`Request`** turns the settings, the checked tile sets and the hand-placed cells into a `DungeonRequest`.
  - **`Cells`** maps every non-excluded cell of every solve (rooms, then corridors) to an item and orientation through the bindings. The empty item clears the cell.
  - **`Write`**: the previous record's still-unchanged cells are cleared, then the new cells are written. The result is `CellChanges` (do/undo) and the new record (non-empty cells). Cells that are already right are skipped.
  - **`Clear`**: the Clear Dungeon changes. Unchanged record cells go; edited cells stay.
  - **Markers.** `PlanMarkers` covers every written cell whose tile has a marker, doors and lanes included. `ReplacedMarkers` finds tagged markers on cells the new dungeon writes, or that the old one still holds. `InstantiateMarker` tags a marker. They live in their own container `DungeonMarkers` with the tag `godoodads_dungeon_marker`, so WFC Generator's regenerate never touches them.
  - **Undo** is the caller's: the M3 dialog wraps `Changes.Do/Undo`, the record bytes and the marker nodes in one `EditorUndoRedoManager` action. The tests apply do, then undo, directly.
- **Finding: the corridor tile set needs T pieces.** The first engine tile set had straight, bend and end pieces only, and every run failed. Guided paths joins each corridor into the network with a T (a junction), so without one only two doors can be joined. New rule in `DungeonTileRules`: T pieces in all four ways (`HasJunction`), an error with an explanation. Brief §4.2 is updated. The test tiles gain T and Cross.

Checks: Core tests **354 passed** (1 new T rule test). Engine tests **98 passed**: 7 new in `DungeonGridMapTests`, with the shared resources in `DungeonEngineTiles`:
- Generate writes rooms, doors (one per layout door) and corners (4 per room), and the record matches the GridMap.
- **Hand-placed cells are unchanged**, inside and outside the bounds, and rooms avoid them.
- **Regenerate replaces only recorded cells.** An edited generated cell and a newly hand-placed cell stay. Every used cell is in the new record or is one of those. Old cells that are not reused are cleared.
- **Undo restores every cell** after replacing an earlier dungeon (checked on a snapshot of all used cells), and redo reapplies.
- **The MeshLibrary and both tile sets are unchanged** (items, names, meshes, tiles and sockets).
- Clear Dungeon keeps edited and hand-placed cells, and its undo restores the rest.
- Markers are planned for marker tiles (4 corners per room), tagged, and found for replacement, while untagged nodes in the container stay.
