---
id: "wfc-1-2-settings-meta-2026-11-10"
status: "todo"
priority: "low"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:29:58.000Z"
completedAt: null
labels: ["editor", "bug"]
order: "d0"
---
# WFC 1.2: No empty settings entry after undo

Write the settings metadata through do/undo methods that remove the entry when there was none, like `ApplyRecord` for the regenerate record. A scene saved with an empty entry must keep working.

Brief: §12.5.

## Done when
- Engine test: undoing a first generation leaves no settings entry, and redo puts it back.
- A GridMap with an empty settings entry opens the dialog with default settings.
