---
id: "wfc-1-5-pinbox-cells-2026-11-20"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T01:00:00.000Z"
completedAt: "2026-11-20T01:00:00.000Z"
labels: ["v1.5.0", "core", "adapters"]
order: "d2"
---
# godoowave 1.5: Pin box nodes: Generate and Clear keep pin box cells

Reading fixed cells, the record after a generation and Clear take a list of pin boxes (origin, size) instead of record pins: occupied cells in a box are fixed or excluded as today, empty cells excluded; record entries inside boxes stay generated; Clear and Clear Bounds First keep box cells with Keep Placed Cells on or off. Remove `GridMapCells.Pin`, `SetPinnedInside`, `IsPinned`, `clearPins`/`keepPinned` and the pin counts; the record reads and ignores the pinned bit and writes 0. Keep the methods godoodungeon calls.

Brief: §16.2, §16.4, §16.5.

## Done when
- Core tests: fixed/excluded per cell for boxes overlapping each other and the bounds; record entries kept inside boxes and replaced after the box goes; old records with pinned bits decode, and pinned cells are free; Clear keeps box cells in both modes; determinism with boxes.
- Engine tests: regenerate and path tests updated (pinned cells as path endpoints become pin box cells); godoodungeon tests still pass.

## Results (2026-11-20)
- **Core:** `CellBox` (Origin, Size, `Contains`, `AnyContains`). `GeneratedRecord` holds (Item, Orientation) only: `IsPinned`, `SetPinnedInside` and the `pinned` parameter are gone, `RemoveInside` takes boxes to keep, reading drops the 0x80 bit and writing never sets it (byte-identical to a pin-free 1.4 record, so godoodungeon records are unchanged).
- **Adapters (`GridMapCells`):** `ReadFixed(…, pinBoxes, keepPlaced)`: cells in boxes are fixed or excluded when occupied and excluded when empty; `keepPlaced: false` frees every other occupied cell (Clear Bounds First, Keep Placed Cells off), which replaces `AllFree` there. `RecordAfter(…, pinBoxes)` keeps entries in boxes; `Clear(…, pinBoxes)` and `RecordAfterClear(…, pinBoxes)` keep box cells and entries in both modes. `Pin`, `clearPins`, `Pinned`, `ClearedPins` removed; `FixedCells.PinBoxCells` and `ClearedCells.PinBoxCells` count the bounds cells inside boxes. Methods godoodungeon calls (`Apply`, `Set`, `Matches`, `Contains`, `Cells`, `Encode`, `Decode`) keep their signatures.
- **Needed here to keep the build green (overlaps `wfc-1-5-pinbox-dialog`):** `WfcSettings` loses the Pin Box group (`PinOrigin`, `PinSize`, Place Around Bounds, Pin Generated Cells, Unpin, `PinRequested`); `BoxOrigin`/`BoxSize`/`SetBox` are the bounds only. The plugin collects `GridMapBox.ActiveChildren<WfcPinBox>` for Generate and Clear, drops `OnPinRequested`, the old pin outline and box 1 of the handles, and reports "Kept N cells in M pin boxes as they were." The dialog card adds Add Pin Box and handles for box children with the GridMap selected.
- **Tests:** Core: old-format record with pinned bits decodes as plain cells and re-encodes byte-identical; `RemoveInside` with keep boxes; `CellBox`. Engine: overlapping boxes sticking out of the bounds (fixed, edited, empty → excluded, record entries kept, replaced once the boxes go, old pins ignored); Clear Bounds First in both modes; Clear in both modes with record; freed cell solved empty leaves no entry; guided path under a pin box is all placed; fixture `tests/godoowave/fixtures/wfc_1_4/pinned_1_4.tscn` (1.4 settings with PinOrigin/PinSize and a record with pinned bits) loads and its pins are freed. Core 511/511, engine 139/139 (godoodungeon included), editor load clean.
- Determinism with boxes: `ReadFixed`/`RecordAfter` are pure functions of the map, record and boxes, and the solve is seeded as before.
