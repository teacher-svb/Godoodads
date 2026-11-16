---
id: "crv-m4-review-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:38:18.000Z"
modified: "2026-11-16T14:50:36.000Z"
completedAt: null
labels: ["v1.0.0", "m4", "review"]
order: "e2"
---
# CRV M4: Milestone review

**Board check on result quality** (brief §7). The board moves this card to *Done* when approved. Do not start M5 before that.

## Review checklist for the board
1. Open each demo scene; run coaster.tscn and ride it.
2. Judge the result quality of the shapes, the joins and the tilt.

## Review round 1 (board, 2026-11-16): not yet approved
- **Coaster:** a small sideways jolt entering and leaving the loop. The cause was the Loop's Side Shift, which drifted at a constant rate from its first point, so the loop left the straight about 4° sideways (3 m shift on a 14 m loop).
- **Fix:** the drift now eases in and out (smoothstep, through `Arcs.Add(easeDrift)`), so the loop enters and leaves along Direction. A shifted loop has 11 points (deviation ≤ 0.003 %). New Core test: a shifted loop's entry and exit handles have no sideways part. Brief §4.1 updated. Demo rebuilt.
- The curvature step where arcs meet straights is kept as an idea card: `crv-idea-curvature-easing-2026-11-16`.
- Outlines and continuing the coaster in Append mode: passed.
- 501 Core and 127 engine tests pass.
