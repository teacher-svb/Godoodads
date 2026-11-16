# Project Godoodads: Godot Editor Tools

## Sub-project 4: godoocurves

**Decisions by:** the board (Syndias)
**Status:** Approved for M0 on 2026-11-16. Planning decisions below were made by the board on 2026-11-16. Milestones M0–M5 in §7, tracked in the epic `godoocurves` with the labels `v1.0.0` and `m0` to `m5`.
**Reference material:** `BezierPathGenerator/` at the repo root (untracked, from the board): an editor plugin that adds a *Generate Curve* button for a selected `Path3D` and writes one of seven preset shapes into its `Curve3D`. Its curve math (`Beziers`) is missing, so it does not build; godoocurves writes the math fresh (§3).

---

### 1. Context

Building a `Path3D` by hand in the editor is slow for regular shapes. A circle, a loop or a sine wave needs points and handles placed exactly, and a track made of several pieces needs every join to keep its direction. `BezierPathGenerator` shows the idea: pick a shape, set a few numbers, and get the curve. It has a hand-built dialog, seven shapes, a plane drop-down and no memory of what was generated.

godoocurves turns that into a suite addon: the suite's menu and dialog (`CONVENTIONS.md`), exact Bezier math in a tested Core, more shapes, placement by a start point and direction, appending pieces so a track can be built from parts, and tilt so banked turns and corkscrews lean.

---

### 2. Goal

Deliver a Godot editor addon that lets a level designer select a `Path3D`, choose a shape and its numbers, and press **Generate** to replace the path's curve with that shape, or to append the shape at the curve's end so that pieces join smoothly. The settings are stored on the node, so reopening the dialog continues where the designer left off.

---

### 3. Technical baseline

| Item | Decision |
|---|---|
| Engine | Godot 4.7 or later, .NET build |
| Language | C# |
| Addon | `addons/godoocurves/`, namespace `Godoodads.Curves`, release tag `godoocurves-v<version>` |
| Target node | `Path3D` only in v1 (decided 2026-11-16). `Path2D` is in §9 |
| Curve math | Written fresh in `Core/` as pure C# with GodotSharp value types, unit-tested (decided 2026-11-16). `BezierPathGenerator`'s missing `Beziers` class is not recovered |
| Workflow | Dialog with settings stored on the node (decided 2026-11-16), like godoowave and godooscatter |
| Usage | Editor-only |
| Testing | Core unit tests without the engine, gdUnit4 for the `Curve3D` adapter, demo scene for manual checks |

#### Layout

```
addons/godoocurves/
  plugin.cfg, README.md, CHANGELOG.md
  Core/        Frames, cubic segments, the shape library, append frame, accuracy helpers
  Resources/   CurveSettings
  Adapters/    Curve3D read (end frame) and write (points, handles, tilt, closed)
  Editor/      Plugin, dialog, preview overlay
demo/godoocurves/
tests/godoocurves/
```

---

### 4. Functional requirements

#### 4.1 Shapes

Every shape is built in a local frame: **forward** (the direction the path starts in), **up**, and **side** = forward × up. Lengths are in the Path3D's local units. Flat shapes have a **Bend** setting (Left, Right, Up, Down) that picks the plane they lie in and the way they turn; it replaces `BezierPathGenerator`'s Plane drop-down.

| Group | Shape | Settings | Notes |
|---|---|---|---|
| Existing | Straight | Length | Two points, no handles |
| Existing | Parabolic | Length, Height | Exact: a quadratic Bezier raised to a cubic |
| Existing | Circle | Radius, Bend | Closed. Starts at the start point, tangent to forward |
| Existing | Semicircle | Radius, Bend | Open, ends facing back |
| Existing | Loop | Length, Diameter, Side Shift | Vertical loop between a straight run-in and run-out. Side Shift moves the exit sideways, so the track does not cross itself (0 allowed) |
| Existing | Wave | Length, Amplitude, Cycles, Bend | Sine wave |
| Existing | Spiral | Length, Radius, Turns | Helix around the forward axis |
| Arcs and bends | Arc | Radius, Angle, Bend | Any angle; Circle and Semicircle are its presets |
| Arcs and bends | S-bend | Length, Offset, Bend | Two opposite arcs; ends parallel to the start |
| Arcs and bends | Ellipse | Radius Forward, Radius Side, Bend | Closed |
| Track pieces | Helix | Radius, Turns, Rise, Bend | Turns around the up axis while climbing (Rise may be negative) |
| Track pieces | Corkscrew | Length, Radius, Turns | Spiral path plus tilt, so the track rolls with the turn (§4.4) |
| Track pieces | Banked Turn | Radius, Angle, Bank, Bend | Horizontal arc with tilt that eases from 0 to Bank and back (§4.4) |
| Closed outlines | Rounded Rectangle | Width, Depth, Corner Radius, Bend | Closed; Corner Radius 0 gives sharp corners |
| Closed outlines | Polygon | Sides, Radius, Corner Radius, Bend | Closed regular polygon |
| Closed outlines | Figure Eight | Length, Width, Bend | Closed, crossing at its middle |

- Arcs are split into pieces of at most 90°, with handle length 4/3·tan(θ/4)·r. Other shapes use the fewest cubic segments that keep the curve within **0.1 % of the shape's size** of the exact shape (§8).
- Ranges in the inspector stop impossible values (radius > 0, sides ≥ 3, cycles ≥ 1, corner radius ≤ half the shorter side).

#### 4.2 Placement

- **Replace mode:** the shape starts at **Start** (default origin), facing **Direction** (default +X), with **Up** (default +Y). Direction and Up need not be exactly perpendicular: Up is made perpendicular to Direction, and parallel vectors are an error.
- **Append mode:** start, direction and up come from the end of the existing curve (§4.3). The Start, Direction and Up settings are hidden.
- Closed shapes (Circle, Ellipse, Rounded Rectangle, Polygon, Figure Eight) are Replace-only; with Append selected they report an error.

#### 4.3 Writing the curve

- **Replace** assigns a new `Curve3D` to the path. **Append** duplicates the existing curve and adds the shape to the copy. The path's original `Curve3D` is never modified, so a curve resource shared by several paths, or saved as a file, stays as it was (CONVENTIONS: never modify user assets). The README says that Generate gives the path its own curve.
- **Append join:** the end frame is the last point's position, its tangent (the in-handle reversed, or the direction from the previous point when the handle is zero) and its up vector including tilt. The shape's first point merges with the last point: it keeps its position and in-handle, and gets the shape's out-handle, so the join keeps its direction. An empty curve appends as Replace with the default frame.
- Closed shapes set `Curve3D` closed (§10.1).
- **Settings on the node:** a `CurveSettings` resource is stored in the Path3D's metadata (`godoodads_curve_settings`), as in godoowave. In Append mode it holds the last piece appended.
- **Undo:** each Generate is one undo step that restores the previous curve and settings.

#### 4.4 Tilt

- Godot's tilt is a roll **on top of** the curve's own up vectors, which Godot computes by turning the start's up as little as possible along the curve (§10.2). Those up vectors twist on any curve that leaves its plane: after a loop with a Side Shift of 1 m the up at the exit is 19° off, and a corkscrew whose tilt is simply its roll angle faces the wrong way. So a shape does not set tilt directly. **Core gives the wanted up direction at each point**; the adapter writes the points, reads Godot's untilted up at each one, and sets the tilt to the signed angle between them around the tangent, unwrapped so neighbouring tilts never differ by more than 180° (checked in M0: corkscrew within 0.04° at 8 points per turn, loop exit back to exact).
- Wanted up: Banked Turn eases from the frame's up to Bank and back over the turn; Corkscrew faces the axis; Loop faces the loop's centre and returns to the frame's up on the run-out; every other shape keeps the frame's up. So each shape ends with the up of its own frame, and the next appended piece continues from it.
- Up vectors stay enabled on the curve. A `PathFollow3D` and a `CSGPolygon3D` in path mode must follow the tilt through loops and corkscrews without flips (§10.2).

---

### 5. UI requirements

- "Generate Curve (GDDS)" (decided 2026-11-16) in the 3D viewport toolbar when a `Path3D` is selected, through `SpatialMenuHook`. Godot shows no menu button for Path3D, so this is likely the hook's own button (§10.3).
- A `ToolDialog` with the `CurveSettings` inspector: Mode (Replace, Append), Shape, the shape's settings (others hidden with `_ValidateProperty`), Start, Direction and Up (Replace only), and a status area. Groups follow the table in §4.1.
- The target is locked when the dialog opens, as in godooscatter 1.3.1.
- **Preview** (in v1, decided 2026-11-16): while the dialog is open, the shape is drawn as an overlay where Generate would put it, also in Append mode. The overlay is never saved.
- The status shows the result, for example `Appended Loop: 9 points, 31.4 m. The curve is now 112.0 m.`

---

### 6. Known pitfalls

1. A `Curve3D` can be shared by several paths or saved as a `.tres`. Editing it in place would change all of them; §4.3 works on a copy.
2. Path3D up vectors twist on curves that leave their plane, and tilt is relative to them (§4.4). Setting tilt from the shape alone gives wrong rolls; the adapter has to measure Godot's up vectors. The bake interval made no difference to the up vectors of a 3 m loop between 0.05 and 1.0 (M0).
3. Points are in the Path3D's local space. A scaled or rotated Path3D scales and rotates the shape with it, which is expected but worth stating.
4. A loop without Side Shift crosses itself at its bottom. That is allowed (some uses want it) but CSG geometry along it overlaps.
5. Appending to a curve whose last point has no handles and no previous point has no direction: it uses the default Direction and says so.

---

### 7. Milestones

Each milestone ends with a board review. Cards use the epic `godoocurves` with the labels `v1.0.0` and the milestone, for example `m1`.

| # | Milestone | Content |
|---|---|---|
| M0 | Foundations | §10 checks, addon skeleton (plugin, menu entry, empty dialog), `BezierPathGenerator/` kept out of the build |
| M1 | Shape core | Frames, cubic segments and arcs, the seven existing shapes and Arcs and bends, accuracy tests |
| M2 | Track, outlines, writing | Helix, Corkscrew, Banked Turn with tilt, closed outlines, append frame, `CurveSettings`, `Curve3D` adapter |
| M3 | Editor | Dialog, Replace and Append, settings on the node, undo, preview overlay, status and errors |
| M4 | Demo | A track built from appended pieces with a CSG profile and a `PathFollow3D` cart, a fence from closed outlines. **Board check on result quality** |
| M5 | Release | Acceptance pass, README with screenshots, CHANGELOG, version 1.0.0, delete `BezierPathGenerator/` |

---

### 8. Acceptance criteria

Automated unless noted:

- **Accuracy:** every shape stays within 0.1 % of its size of the exact shape, sampled densely, for the default settings and a spread of others.
- **Joins:** after Append, the join keeps its position exactly and its direction within 0.01°; the up vector and tilt carry over.
- **Exact handles:** a quarter arc has handles of 4/3·tan(22.5°)·r = 0.5523·r.
- **Closed shapes:** closed, and their first and last points agree.
- **User assets:** the path's original `Curve3D` is unchanged after Replace and Append.
- **Settings:** the stored settings survive a save and reload of the scene, and reopening the dialog shows them.
- **Undo:** undo restores the previous curve and settings, and redo reapplies them. (Manual check.)
- **Tilt:** a `PathFollow3D` runs through the demo loop and corkscrew without flipping. (Manual check.)

---

### 9. Out of scope for v1

- `Path2D` and `Curve2D`.
- Generating meshes along the path (use `CSGPolygon3D` or other tools).
- Editing or refitting existing points, and joining pieces anywhere but the curve's end.
- A library of saved user presets.
- Runtime (in-game) generation.

Note further ideas as idea cards in the epic `godoocurves` with the labels `v2.0.0` and `idea`, or in the repo's `README.md` backlog.

---

### 10. Assumptions to verify in M0

Results from card `crv-m0-verify-engine` (2026-11-16), checked headless with Godot 4.7.

1. **True.** `Curve3D` has `closed` (`set_closed`/`is_closed`), and the baked curve includes the closing side (a 4 m square: 12 m open, 16 m closed), which `PathFollow3D` follows. `CSGPolygon3D` has its own `path_joined`; whether it needs it with a closed curve is checked visually in the M4 demo.
2. **True, with a condition.** Godot's up vectors through a vertical loop are smooth (at most 0.3° between samples) and face the loop's centre without a flip at the top, at any bake interval from 0.05 to 1.0. But tilt is added on top of those up vectors, which twist: see §4.4 for the method that resolved it. Positive tilt turns up right-handed around the tangent (on a +X line, +Y turns toward +Z). `PathFollow3D` (Oriented, `tilt_enabled`) and `CSGPolygon3D` (path follow) read the same up vectors; seen in motion in M4.
3. **True.** Godot 4.7 has no built-in menu button with the Path3D icon: the skeleton plugin reports "using its own Path3D toolbar button" (`crv-m0-skeleton`, headless editor with `--verbose`). `SpatialMenuHook` adds that button with the Path3D icon, and hooks share a fallback button only when their icons match, so it cannot clash with the GridMap button.
4. **True.** A C# `Resource` in a Path3D's metadata saves as a sub-resource of the scene and loads with its values. Surviving an assembly reload uses the same mechanism as `WfcSettings` on a GridMap; checked by hand with the skeleton.
5. **True.** `sample_baked_up_vector(length, apply_tilt: true)` at the end gives the tilted up (last point tilt 0.5 rad: up 28.65° from +Y), the same as the basis of `sample_baked_with_rotation`.

---

### 11. Board decisions (2026-11-16)

Planning: Path3D only; curve math written fresh; dialog with settings stored on the node; Replace or Append; the seven existing shapes plus arcs and bends, track pieces and closed outlines; Start, Direction and Up placement; tilt written per point; `BezierPathGenerator/` kept out of the build and deleted in M5. After the draft: the preview overlay is in v1 (§5), and the menu item is "Generate Curve (GDDS)". The brief was approved for M0.
