---
id: "wfc-1-5-pinbox-cells-2026-11-20"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T13:00:00.000Z"
completedAt: null
labels: ["v1.5.0", "core", "adapters"]
order: "d2"
---
# godoowave 1.5: Pin box nodes: Generate and Clear keep pin box cells

Reading fixed cells, the record after a generation and Clear take a list of pin boxes (origin, size) instead of record pins: occupied cells in a box are fixed or excluded as today, empty cells excluded; record entries inside boxes stay generated; Clear and Clear Bounds First keep box cells with Keep Placed Cells on or off. Remove `GridMapCells.Pin`, `SetPinnedInside`, `IsPinned`, `clearPins`/`keepPinned` and the pin counts; the record reads and ignores the pinned bit and writes 0. Keep the methods godoodungeon calls.

Brief: §16.2, §16.4, §16.5.

## Done when
- Core tests: fixed/excluded per cell for boxes overlapping each other and the bounds; record entries kept inside boxes and replaced after the box goes; old records with pinned bits decode, and pinned cells are free; Clear keeps box cells in both modes; determinism with boxes.
- Engine tests: regenerate and path tests updated (pinned cells as path endpoints become pin box cells); godoodungeon tests still pass.
