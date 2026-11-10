---
id: "wfc-1-1-generated-record-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T08:54:16.000Z"
completedAt: "2026-11-10T08:54:16.000Z"
labels: ["engine"]
order: "c3"
---
# WFC 1.1: Record what Generate wrote

After a successful solve, store the cells the generation wrote (position, item, orientation, non-empty cells only) on the GridMap as compressed metadata, changed in the same undo action as the cells.

Brief: §11.2.

## Done when
- Engine tests: the record round-trips, undo restores the previous record, and the scene file grows by well under 1 MB for a 100×16×100 generation (report the size).

## Result
- `Core/GeneratedRecord` stores the non-empty cells a generation wrote: position, item and orientation. They are sorted by position, written as separate planes (x, y, z as int16, item int32, orientation byte) and compressed with .NET Deflate, behind a version byte. Missing or damaged data reads as an empty record.
- `GridMapCells.RecordAfter` keeps the previous record outside the bounds and adds the non-empty cells written to free cells inside them. `WriteRecord`, `StoredRecord` and `ReadRecord` use the GridMap metadata key `godoodads_wfc_generated`.
- The plugin's undo action writes the record with `ApplyRecord` do/undo methods, not a metadata property. Setting `metadata/…` to null leaves an empty entry in Godot 4.7, which the undo test caught. The settings metadata from 1.0 still uses the property, so undoing a first generation leaves an empty settings entry (harmless; noted for a later fix).
- Size: a 100×16×100 demo generation (seed 1, no ground layer) wrote 55,224 non-empty cells. The record is 19.8 KB encoded, and the saved scene grows from 807,612 to 834,043 bytes (+26 KB, limit 1 MB).
- Tests: `WfcGeneratedRecordTests` (Core, 8) and `WfcRegenerateTests` (engine, 5), which cover the round-trip, undo back to no entry, and the size.
