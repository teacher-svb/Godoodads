---
id: "scatter-1-3-1-spacing-blowup-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godooscatter"
dueDate: null
created: "2026-11-16T09:00:00.000Z"
modified: "2026-11-16T10:00:00.000Z"
completedAt: "2026-11-16T10:00:00.000Z"
labels: ["v1.3.1", "core", "bug", "performance"]
order: "b3"
---
# Scatter 1.3.1: Large Min Distance with a large Amount freezes the editor

Review finding 1 (MAJOR). `SampleElimination` uses radius `max(2·rMax, minDistance)` and stores every neighbour inside it. When Min Distance is far above the natural spacing for Amount, the neighbour lists grow with (minDistance/2rMax)². Reviewer's measurements, 100×100 m plane, Amount 100000: 1 m 12 s / 1.6 GB, 2 m 40 s / 5.9 GB, 3 m 80 s / 12 GB, ~5 m OOM-kills the editor (main thread).

## Done when
- When Min Distance limits the result (minDistance > 2·rMax), the candidate set is thinned to a uniform prefix so the average neighbour count stays bounded. Candidates are independent samples, so a prefix is an unbiased subset.
- Results are unchanged when Min Distance does not dominate (same settings and seed give the same instances as 1.3.0).
- Min Distance still holds between all kept instances and fixed points.
- Core test: Amount 100000, Min Distance 5 m on 100×100 m stays within a time and allocation budget.

## Result
`SampleElimination.Eliminate`: when `minDistance > 2·rMax`, the average neighbour count within the radius is measured on the first candidates (at most 2048 queries, stops after 4 M distance checks). Above `10 × candidates per target` (50 at Candidate Factor 5) the candidates are cut to a prefix that brings the average down to that budget, and the grid is rebuilt. Dropped candidates count in `RemovedForSpacing`. An area-based cap was rejected: it would starve thin domains (a strip narrower than Min Distance holds more than its area suggests).

Scratch harness, 100×100 m plane, seed 7 (before → after):

| Amount | Min Distance | Time | Peak memory | Placed |
|---|---|---|---|---|
| 128 / 5000 / 50000 | 0.5 / 0.5 / 0.3 | unchanged | unchanged | identical (same transform hash) |
| 100000 | 0.5 | 6.8 s → 7.7 s (noise; not thinned) | 0.48 GB | 17691, identical |
| 100000 | 1 | 14.8 s → 1.7 s | 1.74 → 0.23 GB | 4293 → 4213 |
| 100000 | 2 | 40 s (review) → 0.58 s | 5.9 → 0.12 GB | 1072 → 1034 |
| 100000 | 3 | 80 s (review) → 0.38 s | 12 → 0.09 GB | 504 → 499 |
| 100000 | 5 / 20 / 100 | OOM → 0.26 / 0.17 / 0.18 s | < 0.1 GB | 181 / 16 / 1 |

Min Distance holds in every run. Amount 100000 at 0.5 m stays at about 7 s: the neighbour count there (about 39) is under the budget, so the result is kept identical to 1.3.0.

Tests: `SampleEliminationTests.Min_distance_far_above_the_natural_spacing_still_fills_the_area`, `A_thin_strip_keeps_its_candidates`, `PerformanceTests.Large_min_distance_with_a_large_amount_stays_bounded` (1 m and 5 m, < 4 s, < 600 MB allocated). Core 370/370, engine 105/105.
