---
id: "dgn-1-0-1-one-layer-height-curves-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-16T09:27:47.000Z"
modified: "2026-11-16T09:36:57.000Z"
completedAt: "2026-11-16T09:36:57.000Z"
labels: ["v1.0.1", "core", "bug", "common"]
order: "d6a"
---
# godoodungeon 1.0.1: The solver ignores height curves on one-layer grids

Review finding 1 (MAJOR) from the review of `addons/editor_tools_common` at 7d5ccba. `WfcSolver` (`Wfc/Core/WfcSolver.cs:114-128`) sets `_layers = tiles.HasHeightProfiles ? grid.Size.Y : 1`, then picks the weight with `_layers == 1 ? tiles.VariantWeights[v] : tiles.WeightAt(v, height)`. On a one-layer grid with curves, `_layers` is 1 too, so the curve is skipped. The brief (godoowave §12.2: "0 = bottom layer of the bounds") and the rest of the shared code apply the curve at relative height 0 there: `CompiledTileSet.LayersWithoutWeight` and `PathRouter.cs:154-158` both use `RelativeHeight(0, 1) = 0`. So on a one-layer grid, the solver, the router and the empty-layer check use different weights.

- A "roof" tile with curve [0, 1] (never on the bottom layer) next to a plain tile, seed 7: 16×2×16 and 16×4×16 give 0 roof tiles on the bottom layer; 16×1×16 gives 131 of 256.
- Roof tile alone on a one-layer grid: `LayersWithoutWeight(1)` reports [0], but the solver fills the grid.
- The router can refuse path variants that the solver would place, or the reverse.

Who hits it: godoodungeon's corridor solve (always one layer), godoodungeon rooms at the default Room Height 1, godoowave with Bounds Size Y = 1, and Measure in the tile set preview with Sample Y = 1. No test covers a curve on a one-layer grid: `WfcHeightWeightTests` only uses 32×4×32.

## Done when
- The solver picks the weight by `tiles.HasHeightProfiles`, not by `_layers == 1`. Tile sets without curves keep the raw `VariantWeights`, so they give the same results as now. One-layer grids with curves use `WeightAt(v, 0)`.
- Core tests on a one-layer grid: a curve-[0, 1] tile never appears beside a plain tile; alone, it makes the solve Impossible, matching `LayersWithoutWeight(1)`; a tile set without curves gives the same cells as before for a fixed seed.
- CHANGELOG notes in godoodungeon and godoowave: results change for one-layer grids whose tile set uses height curves. Both changelogs promise "same settings and seed give the same cells", so name the exception.
- The godoowave and godoodungeon demos are unaffected: the dungeon tile sets have no curves, and the godoowave demos have more than one layer. Check with the demo asset tests.
- The shared code also ships in the godoowave zip, so godoowave users get the fix with godoowave 1.4.2 (card `wave-1-4-2-review-2026-11-16`).

## Result
- `WfcSolver` picks the weight by `tiles.HasHeightProfiles`: tile sets with curves use `WeightAt(v, height)` on every grid, so a one-layer grid uses height 0; tile sets without curves keep `VariantWeights`. Results for tile sets without curves are unchanged by construction (that branch is the old code path).
- Also `DungeonTileRules.EmptyVariant` (the corridor's empty tile, used to fill door cells and as the check's "no empty tile" error) now compares `WeightAt(v, 0)` instead of the plain weight, since the corridor grid is one layer. The error text says that a curve counts at its bottom.
- Core tests in `WfcHeightWeightTests`: a curve-[0, 1] tile never appears on a 16×1×16 grid over 8 seeds; alone with a weight-0 tile it is Impossible, matching `LayersWithoutWeight(1)`; a flat curve gives the same cells as no curve on one layer. The first two fail with the old line, checked. `DungeonTileRulesTests`: a heavier empty tile with curve [0, 1] is not chosen, and a corridor set whose only empty tile has it is an error.
- CHANGELOG Unreleased entries in godoodungeon and godoowave name the exception to "same settings and seed give the same cells".
- 374 Core and 107 engine tests pass (demo asset tests included). The dungeon tile sets have no curves; the godoowave demos have more than one layer.
