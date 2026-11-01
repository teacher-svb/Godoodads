---
id: "m0-common-menu-integration-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M0 Foundations"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:12:36.000Z"
completedAt: "2026-11-01T00:12:36.000Z"
labels: ["editor", "ui", "common"]
order: "a4"
---
# M0: Hook into the built-in MultiMesh toolbar menu

Godot's built-in `MultiMeshEditor` adds a `MenuButton` ("MultiMesh", icon `MultiMeshInstance3D`) via `add_control_to_menu_panel`, which is the same container as `CustomControlContainer.SpatialEditorMenu`.

**Design**
- A helper that finds that sibling `MenuButton` by its icon rather than its (translated) text, and adds our items to its `PopupMenu` with ids that cannot clash.
- Fallback: if it is not found, add our own `MenuButton` and show or hide it via `_Handles` / `_MakeVisible`.
- Optional: hide the built-in "Populate Surface" item (behind a project setting, default off).

**Files**
- `addons/editor_tools_common/Editor/SpatialMenuHook.cs`

**Acceptance**
- With a `MultiMeshInstance3D` selected, the MultiMesh menu shows "Scatter Surface…".
- Disabling and re-enabling the plugin leaves no duplicate items.

## Result (needs a manual check)
Implemented in `SpatialMenuHook.cs`. A headless editor run confirmed the built-in MultiMesh menu was found (`IsHooked == true`), so the fallback button is not used.
The optional "hide built-in Populate Surface" setting is **not** built yet. It moves to M5 polish if wanted.
**Check in the editor:** select `Scatter` in the demo scene, open the MultiMesh menu and confirm "Scatter Surface…" is there. Toggle the plugin off and on and confirm no duplicates appear.
