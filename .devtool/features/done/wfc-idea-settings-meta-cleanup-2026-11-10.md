---
id: "wfc-idea-settings-meta-cleanup-2026-11-10"
status: "done"
priority: "low"
assignee: null
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:19:39.000Z"
modified: "2026-11-10T09:29:58.000Z"
completedAt: "2026-11-10T09:29:58.000Z"
labels: ["bug", "editor"]
order: "b9"
---
# WFC 1.2: No empty settings entry after undo

Undoing a first generation leaves an empty `godoodads_wfc_settings` metadata entry on the GridMap. Setting a `metadata/…` property to null does not remove it in Godot 4.7. Found in 1.1 (`wfc-1-1-generated-record`). Harmless, but it is saved in the scene.

**Size:** S

## Design notes
- Use do/undo methods that remove the entry, like `ApplyRecord` for the regenerate record.
- The dialog already ignores a settings entry that is not a `WfcSettings`, so scenes saved with an empty entry keep working.

## Result
Split into tickets for 1.2 (2026-11-10, brief §12). Decisions are recorded in the brief.
