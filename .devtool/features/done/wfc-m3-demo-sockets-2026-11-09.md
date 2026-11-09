---
id: "wfc-m3-demo-sockets-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M3 Demo tileset"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:51:14.000Z"
completedAt: "2026-11-09T18:51:14.000Z"
labels: ["demo"]
order: "aJ"
---
# WFC M3: Demo socket set

Author `WfcTileSet` for the demo modules plus air, using sockets to keep ground low and air high (for example, air accepts only air above it).

Brief: §4.5, §7.

## Done when
- The tile set solves at small sizes with sensible vertical structure.

## Result
Socket set in `BuildWfcDemo`. `ground` is a buildable surface and only a special air tile may rest on it. Bridges are a fixed head, deck, deck, head using `span+`/`span-`, because unbounded decks made full-size solves run out of attempts. Without weights, the demo needs 40 copies of each empty tile and 16 grass variants to look open: 141 variants in total. An asset test covers validity and a solve.
