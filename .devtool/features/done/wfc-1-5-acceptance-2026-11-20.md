---
id: "wfc-1-5-acceptance-2026-11-20"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T03:00:00.000Z"
completedAt: "2026-11-20T03:00:00.000Z"
labels: ["v1.5.0", "acceptance"]
order: "d6"
---
# godoowave 1.5: 1.5 acceptance pass

Run the acceptance criteria (brief §8) again with pin boxes, plus the decisions of §16: empty cells stay empty, Clear never touches pin boxes, 1.4 scenes with pins load and their old pins regenerate, godoodungeon unchanged with the 1.5 shared folder.

Brief: §8, §16.

## Done when
- Every criterion has a recorded result, and any failure has its own card.

## Result
Brief §8 and §16, checked 2026-11-20:

| Criterion | Result | Evidence |
|---|---|---|
| Determinism | Pass | Solver unchanged; the 1.3.2 fixture still generates the same cell hash (`A_scene_saved_with_1_3_2_generates_the_same_cells`); `ReadFixed`/`RecordAfter` are pure functions of map, record and boxes |
| Validity, 2D and 3D | Pass | Core neighbour-pair tests unchanged and passing |
| Vertical rules | Pass | Unchanged, Core tests |
| Rotation | Pass | Unchanged, engine basis test |
| Fixed cells | Pass | Engine tests: occupied pin box cells fixed and unchanged after solves (`Cells_in_pin_boxes_…`, demo test over two seeds) |
| Unsatisfiable input | Pass | Unchanged code path and tests |
| Contradiction recovery | Pass | Unchanged solver |
| Unknown cells | Pass | Engine test; unknown items inside pin boxes are excluded as outside |
| User assets | Pass | Engine test: MeshLibrary unchanged |
| Undo | Pass | Manual (board, 2026-11-20): Add Pin Box, pin box drags on the node and with the GridMap selected; generation undo code unchanged |
| Cancel | Pass | Unchanged code path (manual in 1.4); pin box drag cancel restores the box |
| Reload | Pass | Manual (board): pin box node and gizmo survive an assembly reload (facts card) |
| Performance | Pass | 100 × 16 × 100, demo tile set, rock boundary, regenerating over a generated map: 0.57 s mean without pin boxes, 0.48 s with two (20 × 16 × 20 and 25 × 16 × 25), reading cells 14 → 21 ms; 1.3: 0.71 s |
| Reliability | Pass | 20 of 20 seeds in both runs above |
| Empty cells stay empty (§16) | Pass | Engine tests (`Cells_in_pin_boxes_…`, Clear Bounds First, demo clearing over two seeds) |
| Clear never touches pin boxes (§16) | Pass | Engine tests for Clear Bounds and Clear Bounds First, Keep Placed Cells on and off; manual (board) |
| 1.4 scenes with pins load, old pins regenerate (§16.4) | Pass | Fixture `wfc_1_4/pinned_1_4.tscn`; settings with removed properties load without warnings (facts card) |
| godoodungeon unchanged with the 1.5 shared folder (§16.5) | Pass | godoodungeon engine and Core tests pass; records without pins encode byte-identically (Core test); `BoundsGizmoPlugin`/`IBoundsBoxes` API unchanged |

Core 511, engine 142 pass. No failures. The package check is on the docs and release card.
