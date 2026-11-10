---
id: "wfc-1-2-docs-release-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T10:01:20.000Z"
completedAt: "2026-11-10T10:01:20.000Z"
labels: ["docs"]
order: "d8"
---
# WFC 1.2: Docs, CHANGELOG and version 1.2.0

README: boundary sockets, height curves, viewport handles and pin box, marker tiles; update limitations. CHANGELOG 1.2.0. `plugin.cfg` 1.2.0, suite README row, close the idea cards.

Brief: §12.

## Done when
- Docs match the shipped behaviour and the versions agree. `tools/package_addon.sh wfc_generator 1.2.0` and the smoke test pass.

## Result
- README: pinning, viewport boxes, the Pin Box and Boundary options, the Height Curve and Marker tile fields, design tips (boundary floor, shaping by height), failure messages, How it works, performance (0.7 s and 0.8 s), the demo, and limitations.
- CHANGELOG 1.2.0: Added, Changed (faster propagation, results unchanged) and Fixed (settings entry). `plugin.cfg` is 1.2.0, and so are the suite README row and backlog, CLAUDE.md and the brief status. The 1.2 idea cards were already closed as split into tickets.
- `tools/package_addon.sh wfc_generator 1.2.0` passes (throwaway worktree with a temporary commit), and so does `tools/smoke_test_addon.sh` on the zip.
