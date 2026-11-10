---
id: "wfc-1-2-settings-meta-2026-11-10"
status: "done"
priority: "low"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:31:27.000Z"
completedAt: "2026-11-10T09:31:27.000Z"
labels: ["editor", "bug"]
order: "d0"
---
# WFC 1.2: No empty settings entry after undo

Write the settings metadata through do/undo methods that remove the entry when there was none, like `ApplyRecord` for the regenerate record. A scene saved with an empty entry must keep working.

Brief: §12.5.

## Done when
- Engine test: undoing a first generation leaves no settings entry, and redo puts it back.
- A GridMap with an empty settings entry opens the dialog with default settings.

## Result
- Settings are written through `ApplySettings` do/undo methods (`WfcSettings.StoreOn`), which remove the entry for null. `WfcSettings.StoredOn` reads an empty entry left by 1.1 as no settings.
- Engine tests: undoing a first generation leaves no entry and redo restores it; an empty 1.1 entry reads as none.
