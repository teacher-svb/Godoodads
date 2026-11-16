---
id: "crv-m0-verify-engine-2026-11-16"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:24:14.000Z"
modified: "2026-11-16T12:24:14.000Z"
completedAt: null
labels: ["v1.0.0", "m0", "adapters", "spike"]
order: "a1"
---
# CRV M0: Verify the engine facts in brief §10

Check the five assumptions in `docs/briefs/godoocurves.md` §10 with Godot 4.7, headless where possible and in the editor where not. Record each result on this card and correct the brief where an assumption is wrong.

1. `Curve3D` closed curves: the property, and whether `Path3D`, `PathFollow3D` and `CSGPolygon3D` (path mode) honour it.
2. Tilt with up vectors through a hand-built vertical loop and a corkscrew: `PathFollow3D` (rotation mode Oriented) and `CSGPolygon3D` path mode, no flip at the top of the loop. Note the `bake_interval` needed for a 6 m loop.
3. The 3D toolbar for a selected `Path3D`: is there a menu button `SpatialMenuHook` can find by icon? If not, check that the hook's own button does not clash with the shared GridMap button of godoowave and godoodungeon.
4. A `Resource` in a Path3D's metadata saves and loads with the scene, and survives an assembly reload.
5. `Curve3D.SampleBakedUpVector` at the end of a curve, with a tilted last point: the up vector Append needs.

## Done when
- Each of the five has a result on this card (true, false, or true with conditions), with the test or scene used.
- The brief is corrected where needed, and the board sees the changes at the M0 review.
