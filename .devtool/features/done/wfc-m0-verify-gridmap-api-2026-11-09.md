---
id: "wfc-m0-verify-gridmap-api-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M0 Foundations"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:27:25.000Z"
completedAt: "2026-11-09T16:27:25.000Z"
labels: ["spike"]
order: "a1"
---
# WFC M0: Verify GridMap assumptions

Check brief §10 items 1, 2, 3 and 5 on Godot 4.7: no per-item custom data on `MeshLibrary`, orientations are rotations only, four Y rotations map to orientation indices, and the cost of `SetCellItem` at per-frame batch sizes.

## Done when
- Findings are recorded on this card, with the four Y orientation indices and measured write costs.
- The brief is corrected if any assumption is wrong.

## Results (Godot 4.7, headless)
1. **Confirmed.** `MeshLibrary` has no per-item custom data, so the rules live in `WfcTileSet`.
2. **Confirmed.** All 24 orientation bases have determinant +1, so they are rotations only. `GetOrthogonalIndexFromBasis(Basis.FLIP_X)` returns 0 with no error: a mirrored basis silently becomes the identity, so never pass one.
3. **Confirmed.** Rotations around +Y map to orientation indices: 0 degrees = 0, 90 = 16, 180 = 10, 270 = 22. All round-trip. At 90 degrees, local +X points to world -Z and local +Z to world +X.
5. **Partly answered.** 160,000 `set_cell_item` calls took 47 ms from GDScript, plus 17 ms for the following two frames. Batches of 100, 1,000 and 5,000 per frame were all about 6.9 ms, which looks like the frame pacing floor. Headless uses a dummy renderer, so this measures only the call cost, not real mesh rebuilds. The real cost is checked on `wfc-m5-animated-solve`.
