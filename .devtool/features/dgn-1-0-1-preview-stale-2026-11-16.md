---
id: "dgn-1-0-1-preview-stale-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-16T08:53:11.000Z"
modified: "2026-11-16T10:58:14.000Z"
completedAt: null
labels: ["v1.0.1", "editor", "ui", "bug"]
order: "d5"
---
# godoodungeon 1.0.1: Preview Layout goes stale when GridMap cells change

Review finding 2 (MINOR). `UpdatePreview` (`GodoodungeonPlugin.cs:569-597`) only recomputes when a setting in `PreviewKey()` changes. The GridMap cells that `ReadHandPlaced` reads are not part of the key, so painting, erasing or editing cells leaves the preview showing a layout Generate will not make. The README says the preview "draws the rooms, doors and corridors Generate would make".

- Preview on. Paint a statue cell where the preview shows a room: the preview does not change. Generate places that room elsewhere, because it keeps the Gap from the statue.
- Same after erasing a hand-placed cell or editing a generated one.

## Done when
- The preview also recomputes when the hand-placed cells in the bounds change: a cheap GridMap fingerprint (used-cell count plus a hash of the cells in the bounds) in the key, checked a few times per second, or a re-read on a timer while Preview Layout is on.
- Dragging the bounds handles stays smooth on the demo scenes.
- Manual: both cases above in the editor.

## Result
- `DungeonGridMap.Fingerprint(map)`: an order-independent hash over every used cell (position, item, orientation), the cell count and the stored record bytes. `UpdatePreview` recomputes it at most every 250 ms (and at once when the preview key was reset), and appends it to the preview key, so painting, erasing or editing a cell, or a record change, recomputes the preview within a quarter second.
- The fingerprint covers the whole map, not only the bounds: a bounds drag then changes the key once (the bounds) instead of twice (bounds, then the fingerprint for the new box). Painting outside the bounds costs one needless recompute.
- Cost, measured with a throwaway engine test: demo scenes before generating ~0.01 ms; 20,000 cells ~6.5 ms per check, 4 checks per second at most. A generated demo dungeon (~2,300 cells) is under 1 ms.
- Engine test `DungeonGridMapTests.The_fingerprint_changes_when_cells_or_the_record_change`: paint, rotate, change item, erase and a record write each change it; the same cells in another order give the same value. Build clean, 110 engine tests pass. CHANGELOG Unreleased entry.

## Manual check
1. `demo/godoodungeon/dungeon_obstacles.tscn`, Preview Layout on. Paint a cell where the preview shows a room: within a moment the preview moves the room (keeping the Gap). Generate places it where the preview shows.
2. Erase that cell: the preview goes back.
3. Generate, then edit one generated cell: the preview updates.
4. Drag the bounds handles: as smooth as in 1.0.0.

## Review (stern-reviewer, 2026-11-16)
- MINOR, fixed: my claim above was wrong. After Generate, the written cells changed the fingerprint, and within 250 ms the preview recomputed and added its line and the layout warnings below the result a second time. 1.0.0 did not do this: with Randomise Seed on, the recompute ran during the run. Clear Dungeon did the same. `KeepPreviewAfterWrite` now takes the new fingerprint after Generate and Clear Dungeon without recomputing, because the plugin's own write does not change which cells were placed by hand. It does nothing if a setting changed as well. Undo and redo of a Generate still recompute.

## Manual check (added)
5. Preview Layout on, Generate: the status shows the result once, without a second "Preview:" line or repeated room warnings. The same for Clear Dungeon.
- Final review pass (stern-reviewer, APPROVE): one gap below the reporting bar, closed. A cell painted less than 250 ms before Generate or Clear Dungeon, before the next check, was absorbed with the write. The fingerprint is now taken just before the write and must still equal the one the preview was computed from.
