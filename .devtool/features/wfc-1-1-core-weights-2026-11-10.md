---
id: "wfc-1-1-core-weights-2026-11-10"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC 1.1"
dueDate: null
created: "2026-11-09T23:03:56.000Z"
modified: "2026-11-09T23:03:56.000Z"
completedAt: null
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
