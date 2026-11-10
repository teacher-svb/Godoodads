---
id: "wfc-idea-perf-regression-2026-11-10"
status: "done"
priority: "low"
assignee: null
epic: "WFC 1.2"
dueDate: null
created: "2026-11-10T09:19:39.000Z"
modified: "2026-11-10T09:29:58.000Z"
completedAt: "2026-11-10T09:29:58.000Z"
labels: ["core", "performance"]
order: "b10"
---
# WFC 1.2: Recover synthetic performance

The 1.1 cell order made the synthetic Core performance sets 18% (25 tiles) and 14% (50 tiles) slower than 1.0. About 7% is the remaining-weight bookkeeping; the rest is a different search path. The demo got faster, and every run is far below the 30 s target.

**Size:** S

## Design notes
- Profile `Narrow` and `SetCount`. Ideas: skip the mass update for words that change only bits of weight-0 variants, and keep the count and bucket in one array.
- Done when both sets are within 10% of 1.0, or the board accepts the gap.

## Result
Split into tickets for 1.2 (2026-11-10, brief §12). Decisions are recorded in the brief.
