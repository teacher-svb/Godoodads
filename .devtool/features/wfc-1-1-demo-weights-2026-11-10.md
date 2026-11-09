---
id: "wfc-1-1-demo-weights-2026-11-10"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-09T23:03:56.000Z"
completedAt: null
labels: ["demo"]
order: "c2"
---
# WFC 1.1: Rebuild the demo with weights

Replace the 40 + 40 air copies with weights in `BuildWfcDemo`, and reconsider the 16 grass variants (keep a few for looks). Tune weights until the hillside looks like the 1.0 demo or better.

Brief: §11.1.

## Done when
- The variant count and a 20-seed reliability run at 100×16×100 (both scenes) are recorded here, compared with 1.0 (144 variants, 3.87 s and 5.23 s, 20 of 20).
- A windowed screenshot is compared with `docs/hillside.png`, and that image is replaced if the look changed.
