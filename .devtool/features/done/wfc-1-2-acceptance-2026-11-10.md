---
id: "wfc-1-2-acceptance-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T10:01:20.000Z"
completedAt: "2026-11-10T10:01:20.000Z"
labels: ["acceptance"]
order: "d9"
---
# WFC 1.2: 1.2 acceptance pass

Run the 1.0 acceptance criteria again (brief §8) plus the 1.2 checks: boundary sockets, height weights, handles, pin box, markers.

Brief: §8, §12.

## Done when
- Every criterion has a recorded result, and any failure has its own card.

## Result
Brief §8 and §12, checked 2026-11-10:

| Criterion | Result | Evidence |
|---|---|---|
| Determinism | Pass | Core seed tests; propagation classes give identical results per seed (demo and synthetic sets) |
| Validity, 2D and 3D | Pass | Core neighbour-pair tests, including boundary solves |
| Vertical rules | Pass | Rotation-specific adjacency test |
| Rotation | Pass | Engine basis test; marker basis per rotation |
| Fixed cells | Pass | Core and engine tests, including pinned cells |
| Unsatisfiable input | Pass | Core tests; boundary conflicts reported with the outside position (manual: message and red cell) |
| Contradiction recovery | Pass | Contradiction-prone set and brute-force comparison |
| Unknown cells | Pass | Engine test |
| User assets | Pass | Engine test: MeshLibrary unchanged |
| Undo | Pass | Manual: generation with markers, handle drags, pin and unpin |
| Cancel | Pass | Manual: Stop at 250 cells per frame, 100×16×100; cells and markers unchanged |
| Reload during a solve | Pass in 1.1; handles survive a reload (manual, 1.2) | Reload code unchanged |
| Performance | Pass | 100×16×100 demo: 0.69 s with the rock boundary, 0.81 s without (1.1: 1.65 s and 2.34 s) |
| Reliability | Pass | 20 of 20 for both setups, 0 backtracks |
| Boundary sockets (§12.1) | Pass | 7 Core tests and 1 engine test; manual messages |
| Height weights (§12.2) | Pass | 9 Core tests and 1 engine test; manual preview, low towers and empty-layer error |
| Handles and pin box (§12.3) | Pass | Engine pin tests; manual drag, undo, cancel, reload and pin |
| Marker tiles (§12.4) | Pass | 4 engine tests and the demo test; manual placement, undo and redo, replace, save and reopen |
| Follow-ups (§12.5) | Pass | Settings entry removed on undo (engine tests); synthetic performance now better than 1.0 |

Totals: 195 Core tests (including 5 performance tests) and 52 engine tests pass, the build has 0 warnings, the headless editor load is clean, and the packaged 1.2.0 zip passes the smoke test. No failures, so no new cards.
