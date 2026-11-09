---
id: "wfc-m5-failure-overlay-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC M5 Animation and release"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T22:44:10.000Z"
completedAt: "2026-11-09T22:44:10.000Z"
labels: ["editor"]
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
