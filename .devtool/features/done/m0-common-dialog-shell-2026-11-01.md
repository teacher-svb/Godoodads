---
id: "m0-common-dialog-shell-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M0 Foundations"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:12:36.000Z"
completedAt: "2026-11-01T00:12:36.000Z"
labels: ["editor", "ui", "common"]
order: "a3"
---
# M0: Common scatter dialog shell with embedded inspector

A reusable popup in `addons/editor_tools_common/` that matches the built-in *Populate Surface* UX: opened from the 3D viewport toolbar, resizable and scrollable.

**Design**
- An `AcceptDialog` (or `ConfirmationDialog`) containing:
  - an **embedded `EditorInspector`** (`EditorInspector.CreateDefaultInspector()` and `.Edit(resource)`), which renders the settings resource with groups, sliders, resource pickers and enums for free;
  - a custom header row slot (for example node pickers);
  - a status/warning area that persists, so the dialog never hides on error.
- An OK button labelled by the caller (for example "Populate").

**Files**
- `addons/editor_tools_common/Editor/ToolDialog.cs`

**Acceptance**
- The demo plugin opens the dialog with a test resource. Groups collapse, sliders work, and the dialog resizes and scrolls.

## Result (needs a manual check)
Implemented in `addons/editor_tools_common/Editor/ToolDialog.cs`. It builds, and the editor loads it headless without errors.
**Check in the editor:** the dialog opens, can be resized, the inspector scrolls and its groups collapse, and the dialog stays open after Populate.
