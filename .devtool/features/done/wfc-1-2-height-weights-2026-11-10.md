---
id: "wfc-1-2-height-weights-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:29:58.000Z"
modified: "2026-11-10T09:38:50.000Z"
completedAt: "2026-11-10T09:38:50.000Z"
labels: ["core", "editor"]
order: "d3"
---
# WFC 1.2: Per-height weights

Optional `HeightCurve` on `WfcTile`. The compiler produces per-layer variant weights; the solver uses the cell's layer for the choice, the remaining-weight order and the generatable set. The preview shows the curve's effect (for example the weight at bottom, middle and top).

Brief: §12.2.

## Done when
- Core tests: frequencies per layer follow the curve, no curve gives the 1.1 result for a seed, a 0 multiplier is never generated at that height but accepted as a fixed cell, negative values and all-zero layers are errors.
- Performance within 10% of the follow-up baseline when no curves are used.

## Result
- `TileDefinition.HeightProfile`: evenly spaced multipliers from the bottom layer (first) to the top (last), interpolated linearly. `CompiledTileSet.WeightAt(variant, height)`, `RelativeHeight`, `LayersWithoutWeight` and `HasHeightProfiles`. Negative, NaN and infinite values are errors on the tile. The all-zero check now uses the generatable set, so a curve that is 0 everywhere counts.
- Solver: with profiles there is one weight table, start set and byte table per layer (layer = cell ÷ (X×Z)). Without profiles there is a single layer, the 1.1 path. A free cell in a layer with nothing to generate makes the solve Impossible at that cell.
- `WfcTile.HeightCurve` (Curve) is sampled at 64 points over the curve's domain on the main thread, so the solver thread never touches the Curve. The dialog stops with "At some heights every tile has weight 0" and lists the GridMap layers. The preview adds "Height curve: ×a at the bottom, ×b in the middle, ×c at the top".
- Tests: 9 Core tests (shares per layer 0, 1/4, 2/5, 1/2 within 0.05, a flat curve gives the same result as none, interpolation, 0 never generated but accepted as fixed, invalid values, an empty layer, 0 everywhere) and 1 engine test (sampling).
- Performance without curves is unchanged: 0.70, 1.09 and 3.59 s.
- The preview line and dialog message are checked manually in the acceptance pass.
