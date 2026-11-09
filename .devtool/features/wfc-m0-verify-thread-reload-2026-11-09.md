---
id: "wfc-m0-verify-thread-reload-2026-11-09"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC M0 Foundations"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["spike"]
order: "a3"
---
# WFC M0: Verify background thread and reload

Check brief §10 item 6: can a C# background thread be reliably stopped before an assembly reload? Build a throwaway long-running thread started from the plugin and find the right shutdown hook.

Headless runs cannot trigger a reload, so this needs a manual check in the editor. Move to `review` for that.

## Done when
- The shutdown approach is recorded here.
- Rebuilding while the thread runs neither crashes nor hangs the editor.
