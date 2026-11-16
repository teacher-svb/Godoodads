---
id: "crv-m2-review-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T14:08:25.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "review"]
order: "c5"
---
# CRV M2: Milestone review

The board moves this card to *Done* when approved. Do not start M3 before that. M2 has no dialog yet, so the review uses a scene built by a script: a Path3D for each shape and a track appended from pieces, with a `PathFollow3D` and a `CSGPolygon3D` profile, opened in the editor.

## Review checklist for the board
1. Open the review scene and judge the shapes and the track: joins smooth, banks and corkscrew leaning the right way.
2. Read the test results on the M2 cards.

## Summary
- Review scene `demo/godoocurves/m2_review.tscn`, built by `demo/godoocurves/tools/build_review.tscn` (BuildCurvesReview.cs), which uses only the M2 code:
  - **Shapes:** all 16 shapes at the dialog's defaults, four per row, each with a U-shaped channel (`CSGPolygon3D`, path follow) open toward its up, a name label, and its `CurveSettings` stored.
  - **Track:** appended with `CurveWriter.Append` from Straight → Banked Turn (35°, left) → Loop (Side Shift 3) → Wave (up) → Corkscrew (1 turn) → Helix (1 turn, rise 10, left) → S-bend → Straight. A `PathFollow3D` cart with an up-fin runs along it (`CartPlayer`, autoplay, 20 s per lap, shown when the scene runs).
  - The Helix climbs at a constant slope (board decision), so its join to the Corkscrew has a visible kink, as decided.
- Cards: `crv-m2-track-shapes`, `crv-m2-outlines`, `crv-m2-curve-adapter` and `crv-m2-settings` are done, with tables and test results.
- Totals: 495 Core and 127 engine tests pass, and the editor loads headless without errors.
- Next, after approval: M3 (dialog, Replace and Append, undo, preview overlay).
