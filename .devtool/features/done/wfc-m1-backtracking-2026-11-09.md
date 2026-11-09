---
id: "wfc-m1-backtracking-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M1 Core solver"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:45:45.000Z"
completedAt: "2026-11-09T16:45:45.000Z"
labels: ["core"]
order: "a6"
---
# WFC M1: Backtracking and restarts

A trail of domain changes for backtracking, not grid copies. Limits are counted in backtracks and restarts, never in time. Restarts use a seed derived from the original.

Brief: §4.6, §6 item 7.

## Done when
- A tile set that causes contradictions still solves.
- Results stay deterministic with backtracking and restarts.

## Result
Chronological backtracking on a trail of word changes, plus restarts with a derived seed. Defaults: 2,000 backtracks per attempt, 20 restarts. On 3,000 random small tile sets, solved-or-impossible agrees with brute-force enumeration. A contradiction-prone set found by random search covers recovery and determinism with backtracks and restarts.
