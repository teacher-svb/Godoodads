---
id: "dgn-m3-bounds-handles-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:52:31.000Z"
completedAt: "2026-11-15T18:52:31.000Z"
labels: ["v1.0.0", "m3", "editor"]
order: "d2"
---
# DGN M3: Bounds handles in the viewport

Bounds box with snapped handles while the dialog is open, reusing the shared `BoundsGizmoPlugin` in `editor_tools_common/Wfc/Editor/`. Drags go through undo.

Brief: §5.

## Done when
- Manual check: dragging updates the dialog and undoes in one step.

## Results
Branch `godoodungeon-m1`.

- `GodoodungeonPlugin` implements `IBoundsBoxes` (one box, the bounds) and registers its own shared `BoundsGizmoPlugin` ("godoodungeon bounds"), with input forwarding always on, as WFC Generator does.
- While the dialog is open, an overlay with no owner (so it is never saved) draws the bounds in **violet** (WFC Generator uses orange). It redraws only when the bounds change, including changes typed in the inspector.
- Handles sit on the six faces. Dragging one snaps to cell boundaries and keeps at least one cell. Release adds one undo step, "Resize Dungeon Bounds". Right-click or Escape during a drag puts the bounds back. Handles only react while the dialog's GridMap is selected.
- Headless editor load: clean. Build: 0 warnings. Dragging needs a mouse, so it is checked by hand.

## Manual check (board)
1. Open the dialog on a GridMap. The violet bounds box and its six handles show.
2. Drag a handle. The box snaps to cells, and Bounds Origin and Size in the dialog follow.
3. Ctrl+Z puts the box back in one step. Escape during a drag cancels it.

## Board check (2026-11-15)
- Passed: handles show and drag with snapping, the dialog follows, Ctrl+Z undoes in one step, and Escape cancels.
- The box looked **yellow**, not violet. The likely cause: the overlay material treated vertex colours as linear (brightened on screen), and the demo scene tonemaps with Filmic. Fixed: the overlay material uses `VertexColorIsSrgb` and `DisableFog`, and the bounds colour is a deeper violet (0.55, 0.2, 0.95). The colour gets another look at the M3 review.
- Colour recheck after the sRGB fix: the bounds box is violet, and the preview shows cyan rooms, yellow doors and green corridors.
