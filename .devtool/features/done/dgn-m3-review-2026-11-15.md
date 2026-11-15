---
id: "dgn-m3-review-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:57:42.000Z"
completedAt: "2026-11-15T18:57:42.000Z"
labels: ["v1.0.0", "m3", "review"]
order: "d7"
---
# DGN M3: Milestone review

Demo for the board. The board moves this card to *Done* when approved. Do not start the next milestone before that.

## Review checklist for the board
1. Open the prototype scene, tune the layout with Preview Layout, then generate, undo, redo and clear.
2. Cancel a run and rebuild during a run.

## Board review (2026-11-15): approved
1. Preview Layout, Generate, undo, redo and Clear Dungeon were checked in the editor on the M3 cards: dialog, layout preview, clear dungeon, bounds handles and failure reporting.
2. Cancelling and rebuilding during a run were accepted on the automated coverage. Runs finish too fast to interrupt by hand, even at 400 × 400 with 400 rooms (`dgn-m3-run-cancel-reload`).
- Also in M3: corridors are routed room to room along the room graph (`dgn-m3-edge-router`), after the board saw guided paths wrapping corridors around rooms. Generate was greyed out after a reload (fixed). The overlay colours now display as written (sRGB).
- M3 approved. M4 (demo tile set) can start.
