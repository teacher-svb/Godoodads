---
id: "wfc-m4-socket-preview-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M4 Authoring tools"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T22:35:10.000Z"
completedAt: "2026-11-09T22:35:10.000Z"
labels: ["editor"]
order: "aN"
---
# WFC M4: Socket preview

An `EditorInspectorPlugin` for `WfcTileSet` showing the selected tile in a 3D preview with labelled faces and a rotation view.

Brief: §5.

## Done when
- Each face label appears on the correct side, for each rotation (manual check).

## Result
`TileSetPreview`, added by an `EditorInspectorPlugin` for `WfcTileSet`: tile and rotation pickers, 3D view with labelled faces and drag to orbit, and identical tile copies listed once. A windowed screenshot showed correct labels for rotated stairs. Needs the manual check in the editor.

## Result
Manual check passed (2026-11-09): the panel appears in the Inspector, the 3D view orbits, and stair rotations keep `landing` on the high side.
