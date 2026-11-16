---
id: "crv-m1-review-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:50:52.000Z"
modified: "2026-11-16T13:06:56.000Z"
completedAt: null
labels: ["v1.0.0", "m1", "review"]
order: "b4"
---
# CRV M1: Milestone review

The board moves this card to *Done* when approved. Do not start M2 before that. M1 is Core only, so the review is by test results and a picture of the shapes (sampled points rendered to an image), not in the editor.

## Review checklist for the board
1. Look at the shape sheet: every M1 shape at its defaults, drawn from its Core points.
2. Read the accuracy table and point counts on the shape cards.

## Summary
- Shape sheet: `/tmp/claude-1000/-home-syndias-Projects-TnT-Godoodads/49e7ad52-33af-41a4-9544-fd1f3cad8181/scratchpad/shapesheet/godoocurves-m1-shapes.png`. It shows all 10 M1 shapes, with Wave in two bends and Arc at two angles: points, handles and wanted up. It was drawn from Core output by a scratch exporter and is not committed.
- Accuracy tables are on `crv-m1-existing-shapes` and `crv-m1-arcs-bends`. The worst deviation is 0.043 % against the 0.1 % limit, and every shape uses 2 to 51 points.
- Board decisions taken during M1:
  - Wave starts and ends level.
  - Spiral eases in and out.
  - Loop's Length runs from start to end.
  - Parabolic leaves at its slope.
- Other M1 decisions:
  - Arc's angle must be above 0 (Bend picks the side).
  - S-bend is always two equal arcs.
- 463 Core tests pass.
- Next, after approval: M2 (track pieces with tilt, closed outlines, the `Curve3D` adapter, `CurveSettings`).
