---
id: "wfc-1-1-core-weights-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T08:18:23.000Z"
completedAt: "2026-11-10T08:18:23.000Z"
labels: ["core"]
order: "c0"
---
# WFC 1.1: Weighted variant choice in the solver

Add weights to the Core: `TileDefinition.Weight`, each variant gets weight ÷ its tile's variant count (after merging), and `PickVariant` chooses in proportion to weight with the seeded random. Variants of weight-0 tiles are removed from the starting options of free cells, but stay valid for fixed cells.

Brief: §11.1.

## Done when
- Core tests: chosen frequencies follow the weights over many cells (statistical tolerance), shared weights make a 4-way tile as likely as a 1-way tile of equal weight, results are deterministic, weight-0 tiles are never generated but are accepted as fixed cells.
- Negative weights and an all-zero tile set are compile errors.
- The M1 performance baseline does not get slower by more than 10%.

## Result
- `TileDefinition.Weight` (default 1). `CompiledTileSet.VariantWeights` holds weight ÷ variant count after merging, and an internal `Generatable` bitset. Negative, NaN and infinite weights are errors on the tile (direction -1). A set where every tile has weight 0 is an error.
- Free cells start from the `Generatable` bitset, and fixed cells from the full set, so a hand-placed weight-0 tile is accepted. `PickVariant` sums the weights of the cell's options and picks with `WfcRandom.NextDouble()` (53 bits). The fewest-options cell order is unchanged.
- `TileSetBinding` formats tile errors without a direction as "Tile N: message".
- Tests: `WfcWeightTests`, 12 tests covering frequencies within 0.03 of 3:1 over 4096 cells and three seeds, a 4-way tile as likely as a 1-way tile of equal weight, merged rotations, determinism, weight 0 never generated, weight 0 accepted as fixed, weight 0 still constraining its neighbours, invalid weights and all-zero sets. All 1.0 tests still pass (167 Core).
- Performance, three runs each, 1.0 → 1.1: 25 tiles 0.73 → 0.75 s (+3%), 50 tiles 1.55 → 1.59 s (+2%), 12 tiles (gives up) 4.79 → 5.16 s (+8%). Within the 10% budget.
- Later the same day, on `wfc-1-1-demo-weights`, the board changed the cell order to least remaining weight. The performance figures above are for the fewest-options order; see that card for the final ones.
