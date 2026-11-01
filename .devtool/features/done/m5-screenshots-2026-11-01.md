---
id: "m5-screenshots-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M5 Polish"
dueDate: null
created: "2026-11-01T01:36:45.000Z"
modified: "2026-11-01T02:03:06.000Z"
completedAt: "2026-11-01T02:03:06.000Z"
labels: ["docs"]
order: "a4"
---
# M5: Screenshots

Result screenshots for the README: play each demo scene and take the image with godot-mcp `capture_viewport`. The dialog screenshot (an editor window) comes from the board.

## Result
Rendered each demo in a short-lived windowed Godot process (a `-s` script saving `root.get_texture().get_image()`), so the editor and its unsaved `demo.tscn` were not touched. Saved to `addons/multimesh_scatter/docs/*.png` (with `.gdignore`, not imported). Still needed: a screenshot of the dialog from the board.

## Result
The board sent the dialog screenshot: `docs/dialog.png`, added to the README under "Use".

## Board check
The final dialog screenshot from the board is `docs/dialog.png` (README, "Use").
