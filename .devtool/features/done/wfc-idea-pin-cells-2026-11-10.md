---
id: "wfc-idea-pin-cells-2026-11-10"
status: "done"
priority: "medium"
assignee: null
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:19:39.000Z"
modified: "2026-11-10T09:29:58.000Z"
completedAt: "2026-11-10T09:29:58.000Z"
labels: ["idea", "editor"]
order: "b8"
---
# WFC 1.2: Pin generated cells

Keep a good part of a generated result while regenerating the rest. Today you have to edit a cell to stop a regenerate from replacing it.

**Size:** S–M

## Design notes
- "Pin" removes cells from the 1.1 regenerate record (`GeneratedRecord`), so they count as hand-placed. "Unpin" adds them back.
- Choose cells with a box in the viewport, or pin everything outside a smaller bounds box (combines with `wfc-idea-bounds-handles`).
- One undo action per pin.

## Open questions
- How is the selection made: a box drag, or the GridMap editor's own selection?

## Result
Split into tickets for 1.2 (2026-11-10, brief §12). Decisions are recorded in the brief.
