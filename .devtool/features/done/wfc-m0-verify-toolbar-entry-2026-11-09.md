---
id: "wfc-m0-verify-toolbar-entry-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC M0 Foundations"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:33:03.000Z"
completedAt: "2026-11-09T16:33:03.000Z"
labels: ["spike"]
order: "a2"
---
# WFC M0: Verify toolbar entry point

Check brief §10 item 4: whether a built-in GridMap menu exists in the 3D toolbar for `SpatialMenuHook`, or the addon needs its own button.

## Done when
- The entry point is decided and recorded here.
- The chosen entry appears when a `GridMap` is selected and hides otherwise.

## Result
There is no built-in GridMap menu in the 3D toolbar in 4.7. The toolbar has Mesh, MultiMesh and CSG menu buttons, and no MenuButton anywhere has "Grid" in its text. `SpatialMenuHook` therefore uses its fallback: the addon's own "GridMap" menu button, shown by `_MakeVisible`. Scatter's hook does find the MultiMesh menu in the same run, so the search itself works.

## Manual check
Select a `GridMap`: a **GridMap** menu appears in the 3D toolbar. Select another node: it disappears.

## Manual result (2026-11-09)
Passed in the editor: the GridMap menu appears for a selected `GridMap` and disappears for other nodes.
