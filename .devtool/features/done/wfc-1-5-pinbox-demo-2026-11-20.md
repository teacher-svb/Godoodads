---
id: "wfc-1-5-pinbox-demo-2026-11-20"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-20T02:40:00.000Z"
completedAt: "2026-11-20T02:40:00.000Z"
labels: ["v1.5.0", "demo"]
order: "d5"
---
# godoowave 1.5: Pin box nodes: hillside demo

The hillside ruin demo gets two pin boxes, one around the ruin and one around an empty clearing, built by `demo/godoowave/tools/build_demo.tscn`.

Brief: §16.7.

## Done when
- The builder produces the scene headless; Generate keeps the ruin and leaves the clearing empty.

## Results (2026-11-20)
- The hillside scenes ship empty, so the pins go in a new scene, `demo/godoowave/hillside_pins.tscn`, built by `BuildWfcDemo.SavePinScene` (part of `build_demo.tscn`): a 32 × 12 × 32 hillside on rock generated at build time (first seed from 4 with a ruin of at least 20 pieces and a clearing: seed 5) and saved with its cells, record and tower light markers, plus two full-height 8 × 8 pin boxes: **RuinPin** around the window with the most ruin pieces (32, at (14, 0, 20)) and **ClearingPin** over a window without any, as far from it as possible ((22, 0, 0)).
- Rebuilding changed only random ids in the other demo files (and dropped their editor uids), so they were left as committed.
- Engine test `The_pin_box_demo_keeps_its_ruin_and_its_clearing_for_any_seed`: two Generates with seeds 101 and 102 leave every cell in both boxes as it was, the clearing without ruin pieces, and change over 100 cells outside. Engine 142/142, editor load clean.
