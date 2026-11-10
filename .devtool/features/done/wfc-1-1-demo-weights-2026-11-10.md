---
id: "wfc-1-1-demo-weights-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-10T08:45:37.000Z"
completedAt: "2026-11-10T08:45:37.000Z"
labels: ["demo"]
order: "c2"
---
# WFC 1.1: Rebuild the demo with weights

Replace the 40 + 40 air copies with weights in `BuildWfcDemo`, and reconsider the 16 grass variants (keep a few for looks). Tune weights until the hillside looks like the 1.0 demo or better.

Brief: §11.1.

## Done when
- The variant count and a 20-seed reliability run at 100×16×100 (both scenes) are recorded here, compared with 1.0 (144 variants, 3.87 s and 5.23 s, 20 of 20).
- A windowed screenshot is compared with `docs/hillside.png`, and that image is replaced if the look changed.

## Result
- `BuildWfcDemo` uses weights instead of copies: Air and Air Above Ground 40 each, slopes, stairs, plinths, bridges, wall ends and corners 4, the three straight walls 2, plain grass 5 plus 4 grass variants (down from 16) at 3 each, so flat grass weighs 17 as in 1.0. That gives 54 variants instead of 144, one 64-bit word instead of three.
- **Finding:** with the fewest-options order, these weights gave tall rock plateaus (ground scene: rock 87k cells instead of 16.5k, 4× the bridges). The air copies had also pushed cells that could be air to the back of the order. No weight setting fixed it, and ramp counts stayed at about 3,000 whatever their weight. The board chose to order cells by remaining weight (brief §11.1 updated). The prototype and the final code are in `WfcSolver`.
- Reliability at 100×16×100, 20 seeds, default limits, 0 backtracks and 0 restarts:

| Scene | 1.0 (144 variants) | 1.1 (54 variants) |
|---|---|---|
| Rock ground layer | 20 of 20, 4.04 s average (3.87 s in the M3 run) | 20 of 20, 1.65 s average |
| No fixed cells | 20 of 20, 5.46 s average (5.23 s in the M3 run) | 20 of 20, 2.34 s average |

- Average tile counts match 1.0 within seed noise. Ground scene, 1.0 → 1.1: rock 16,565 → 16,828, ramps 2,348 → 2,380, grass 2,992 → 2,908, bridge decks 74 → 69, wall ends 137 → 127, towers 73 → 76.
- Windowed renders at 32×12×32 (seeds 1 and 2) look like `docs/hillside.png`: rolling hills with scattered ruins. The image is kept.
- Core performance tests with the new order, 1.0 → 1.1: 25 tiles 0.73 → 0.86 s (+18%), 50 tiles 1.55 → 1.77 s (+14%), 12 tiles 4.79 → 4.9 s (+3%). The weight bookkeeping costs about 7% (byte lookup tables keep it low), and the rest is the different search path on these synthetic sets. That exceeds the ticket's 10% budget for those two sets, but every run is far below the 30 s target and the demo is 2.4× faster than 1.0.
- New tests: `LightCellsAreChosenFirst` (fails under the old order) and `AWeightBehavesLikeThatManyCopies` (tile shares within 0.03). Weight and copies are not exactly equal, because a cell left with copies of one tile still needs a decision.
