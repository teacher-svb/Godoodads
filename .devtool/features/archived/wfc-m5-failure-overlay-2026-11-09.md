---
id: "wfc-m5-failure-overlay-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-09T22:44:10.000Z"
labels: ["v1.0.0", "m5", "editor"]
order: "aS"
---
# WFC M5: Failure overlay

Highlight the failing cell in the viewport and show its neighbours and sockets in the status area. The overlay is never saved into the scene.

Brief: §4.8.

## Done when
- A forced failure highlights the correct cell and reports the correct failure kind.

## Result
Failing cells and conflicting placed cells are drawn as red boxes in the bounds overlay, never saved. The message says whether the failing cell was placed by the user. Needs the manual check.

## Result
Manual check passed (2026-11-09): red boxes mark the conflicting cells and disappear when the dialog closes.
