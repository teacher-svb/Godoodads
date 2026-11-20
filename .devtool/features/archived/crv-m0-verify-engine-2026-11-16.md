---
id: "crv-m0-verify-engine-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:24:14.000Z"
modified: "2026-11-16T12:48:27.000Z"
completedAt: "2026-11-16T12:48:27.000Z"
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

## Results (2026-11-16, Godot 4.7, headless GDScript probes)
1. **Closed: true.** `Curve3D.closed` exists; baked length includes the closing side (square 12 → 16 m). `CSGPolygon3D` has `path_joined`; visual check in M4.
2. **Tilt: true with a condition, which changed the brief (§4.4).**
   - Hand-built 3 m vertical loop with exact arc handles (4/3·tan(22.5°)·r): Godot's up vectors are smooth (≤ 0.3° between 2,000 samples), face the centre all the way, no flip at the top. Same at bake intervals 0.05, 0.2, 0.5 and 1.0.
   - With a 1 m Side Shift the up at the exit is 19.06° off +Y: Godot's up vectors twist when the curve leaves its plane.
   - A corkscrew with tilt = roll angle faces the wrong way (dot with the axis direction from −0.9 to +0.6): tilt is added on top of Godot's twisting up vectors.
   - **Method that works:** write the points, read Godot's untilted up at each point, set tilt = signed angle (around the tangent) to the wanted up, unwrapped so neighbours differ by less than 180°. Corkscrew: within 0.04° of facing the axis at 8 points per turn, 0.00° at 16. Loop: exit up back to exactly +Y, error between points ≤ 0.78°, tilts 0 → 19.06° over the loop.
   - Sign: positive tilt turns up right-handed around the tangent (+X line: +Y → +Z).
   - `PathFollow3D` (Oriented, `tilt_enabled`) and `CSGPolygon3D` read these up vectors; seen in motion in the M4 demo.
3. **Toolbar: true (settled by `crv-m0-skeleton`: the headless editor with `--verbose` prints "godoocurves: using its own Path3D toolbar button.").** `SpatialMenuHook.FindHost` matches a built-in `MenuButton` whose icon is the Path3D icon; otherwise it adds its own button with that icon. Fallback buttons are shared only between hooks with the same icon, so no clash with the GridMap button. Whether Godot has such a button shows when the skeleton plugin runs in the editor.
4. **Metadata: true.** A C# resource (`DungeonSettings` as a stand-in) in `Path3D` metadata saves as a sub-resource (`metadata/godoodads_curve_settings = SubResource(...)`) and loads with its values. Assembly reload: same mechanism as godoowave's settings; checked by hand with the skeleton.
5. **End up: true.** `sample_baked_up_vector(length, true)` with last-point tilt 0.5 rad gives (0, 0.878, 0.479), 28.65° from +Y, equal to `sample_baked_with_rotation(...).basis.y`.

Brief updated: §4.4 (wanted up per point, tilt computed by the adapter), §6.2, §10. The probes become gdUnit tests of the adapter in M2.
