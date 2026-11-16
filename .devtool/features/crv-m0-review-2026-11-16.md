---
id: "crv-m0-review-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:24:14.000Z"
modified: "2026-11-16T12:33:11.000Z"
completedAt: null
labels: ["v1.0.0", "m0", "review"]
order: "a3"
---
# CRV M0: Milestone review

Demo for the board. The board moves this card to *Done* when approved. Do not start M1 before that.

## Review checklist for the board
1. Read the results on `crv-m0-verify-engine-2026-11-16` and the brief changes they caused.
2. Check brief §11: the board decisions recorded so far.
3. Enable **godoocurves**, select a Path3D and open "Generate Curve (GDDS)".
4. Run the manual checks on `crv-m0-skeleton-2026-11-16`.

## Summary
- §10 all checked. The important finding: Godot's tilt is relative to up vectors that twist on curves leaving their plane, so the adapter computes tilt from the engine's untilted up vectors (brief §4.4 rewritten, method proven on a loop and a corkscrew).
- Skeleton addon 0.1.0 with its own Path3D toolbar button and an empty dialog.
- Next, after approval: M1 (shape core) cards.
