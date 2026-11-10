---
id: "wfc-1-1-docs-release-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T09:02:49.000Z"
completedAt: "2026-11-10T09:02:49.000Z"
labels: ["docs"]
order: "c6"
---
# WFC 1.1: Docs, CHANGELOG and version 1.1.0

Rewrite the README density guidance around weights (copies are no longer needed), describe regenerate and the Weight option, and update limitations. CHANGELOG 1.1.0 says that results differ from 1.0 for the same seed. `plugin.cfg` 1.1.0, suite README row, and the idea card for the Copies field closed as superseded.

Brief: §11.

## Done when
- Docs match the shipped behaviour and the versions agree. `tools/package_addon.sh wfc_generator 1.1.0` passes.

## Result
- README covers weights in place of copies, regenerate, the Weight field and preview line, How it works (weight order and weighted choice), performance (1.7 s and 2.3 s, 54 variants), and limitations (weights are global).
- CHANGELOG 1.1.0 (2026-11-10): Added for weights, preview and regenerate. Changed for results differing from 1.0 and the demo.
- `plugin.cfg` is 1.1.0, the suite README row is 1.1.0, and its backlog points to the WFC 2.0 epic. CLAUDE.md and the brief status line mention 1.1.0. The Copies idea card is closed as superseded.
- `tools/package_addon.sh wfc_generator 1.1.0` passes, run in a throwaway worktree with a temporary commit because the script reads HEAD: 62 files, 526 KB, including `GeneratedRecord.cs`. `tools/smoke_test_addon.sh` on that zip is reported on the acceptance card.
