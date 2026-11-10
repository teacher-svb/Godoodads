# Project Godoodads: Godot Editor Tools

## Sub-project 1: MultiMesh Scatter

**From:** the board (Syndias)
**To:** CEO
**Status:** Ready for planning
**Reference material:** `UpFaceMultiMesh` (C# editor plugin from the board's educational game), to be committed to `reference/UpFaceMultiMesh/` as read-only

---

### 1. Context

We are starting a long-running project: a suite of Godot editor extensions (working title **Godot Editor Tools**). It will contain many independent sub-projects, each shipped as its own addon, sharing common conventions and helper code.

**Sub-project 1 is MultiMesh Scatter**: an editor tool that fills a `MultiMeshInstance3D` with instances placed on a surface mesh, with an integrated Godot editor UI. It is a generalisation of the board's existing `UpFaceMultiMesh` plugin, which only places instances on upward-facing triangles and always orients them to world-up.

Because this is the first sub-project, it also sets the conventions (repo layout, shared UI helpers, testing, documentation) that every later sub-project will follow. Treat those foundations as part of the deliverable.

---

### 2. Goal

Deliver a Godot editor addon that lets a level designer select a `MultiMeshInstance3D`, pick a surface, configure placement rules in a dialog, and press **Populate**, where placement can be driven by:

1. **Surface geometry**, with instances oriented somewhere between the surface normal and a chosen direction vector (blend slider 0 to 1, plus per-instance randomness).
2. **A black and white mask texture** on the surface that controls where instances may be placed.
3. **A normal map** on the surface that defines the normal the instances follow.
4. **Any combination of the above.**

---

### 3. Technical baseline

| Item | Decision |
|---|---|
| Engine | Godot 4.7 or later, .NET build (decided by the board). |
| Language | C#, matching the reference plugin |
| Addon type | `EditorPlugin` addon under `addons/`, all editor code wrapped in `#if TOOLS` |
| Testing | GdUnit4 (C# support) for automated tests, plus a demo scene for manual checks |
| Repo | One repository for the whole suite, one addon folder per sub-project |

#### Proposed repository layout

```
/
  README.md                      Suite overview, list of sub-projects
  CONVENTIONS.md                 Coding, UI, testing and doc conventions (written in milestone 0)
  reference/UpFaceMultiMesh/     Original plugin, read-only, do not modify
  project.godot                  Demo/test Godot project
  addons/
    editor_tools_common/         Shared helpers (UI field builders, node picker, undo helpers)
    multimesh_scatter/
      plugin.cfg
      README.md
      CHANGELOG.md
      Core/                      Pure logic: sampling, masks, normals, orientation (no UI)
      Editor/                    EditorPlugin, dialog, pickers
  demo/multimesh_scatter/        Demo scenes and test assets (meshes, masks, normal maps)
  tests/multimesh_scatter/       GdUnit4 tests
```

Keep **Core** free of editor UI code so it can be unit tested and, later, reused at runtime if the board asks for it.

---

### 4. Functional requirements

#### 4.1 Carry over from the reference plugin

Everything the reference does must keep working:

- Toolbar button in the 3D editor menu that only appears when a `MultiMeshInstance3D` is selected.
- Dialog with a scene-tree node picker for the surface (`MeshInstance3D` or `CsgShape3D`), invalid nodes greyed out.
- Options: amount, base scale, random scale, random Y rotation, random tilt, minimum distance.
- Area-weighted and uniform-per-triangle sampling.
- Minimum-distance filtering that also avoids existing sibling instances.

Plus the four open TODOs from the reference:

- **Seed field** (with a "randomise" button) so results are reproducible.
- **Slope filter slider** replacing the hard-coded 0.7 normal threshold (see 4.5).
- **Append vs Replace mode**, so several surfaces can be scattered into one MultiMesh.
- **Undo/redo** through `EditorUndoRedoManager`, so Ctrl+Z reverts a populate.

#### 4.2 Surface mode: orientation blend

Every placed instance gets an "up" direction computed as follows:

- `N` = surface normal at the sample point, in world space (source chosen in 4.4).
- `V` = target direction vector, in world space, normalised. Default `(0, 1, 0)`.
- `b` = **Alignment** slider, 0 to 1. **0 = follow surface normal, 1 = follow target vector.**
- `r` = **Alignment randomness** slider, 0 to 1. Per instance: `b_i = clamp(b + random(-r, r), 0, 1)`.
- Instance up = `slerp(N, V, b_i)`. Handle the case where `N` and `V` are (almost) opposite by rotating around any stable perpendicular axis, never produce NaN.

After that, apply in this order: random yaw around the instance's own up axis, random tilt, scale, position.

UI: the target vector is a Vector3 field with quick buttons (+Y, -Y, +X, +Z). Add an option for which local axis of the instanced mesh counts as its "up" (default +Y), since not every asset is modelled Y-up.

#### 4.3 Mask texture mode

- Texture picker for a mask (`Texture2D`), sampled at the UV coordinate of each sample point.
- Options:
  - **Channel**: Luminance (default), R, G, B, A.
  - **UV set**: UV1 (default) or UV2.
  - **Mode**: *Threshold* (place only where value >= threshold) or *Density* (place with probability equal to the value, so grey = sparser).
  - **Threshold** slider (Threshold mode only).
  - **Invert** toggle.
- Bilinear sampling of the texture.
- If the mask rejects samples, keep sampling until **Amount** is reached or a maximum attempt count is hit (for example Amount x 20). Report the shortfall in the dialog rather than silently placing fewer.
- Suggested optimisation (not required for v1): pre-weight triangles by area x average mask value so mostly-black meshes do not waste attempts.

#### 4.4 Normal source and normal map mode

A **Normal source** dropdown:

1. **Face normal** (flat, per triangle). Matches the reference behaviour.
2. **Smooth normal** (vertex normals interpolated with the sample's barycentric coordinates).
3. **Normal map** (perturbs the smooth normal using a texture).

Normal map options:

- Texture picker (`Texture2D`), UV set (UV1/UV2).
- **Space**: Tangent space (default) or Object space.
- **Flip green channel** toggle. Godot expects OpenGL-style (Y+) normal maps; DirectX-style maps need the flip.
- **Strength** slider 0 to 1 (0 = ignore map, 1 = full map normal), so the effect can be dialled in.
- Tangent space needs mesh tangents. If the mesh has none, generate them on a temporary copy (`SurfaceTool.GenerateTangents`) and show a notice; never modify the user's mesh.

#### 4.5 Slope filter

Generalises the reference's "upward faces only" logic: only place where the angle between the **geometric** normal and a reference direction (default: the target vector `V`) lies between a min and max angle (defaults 0 and 180, meaning no filtering). Ship a preset that reproduces the reference behaviour (0 to about 45 degrees from +Y).

#### 4.6 Combination

All features above are independent stages of one pipeline and can be enabled in any combination:

```
Surface data (positions, normals, UVs, tangents, all surfaces)
  > Triangle sampler (uniform or area-weighted, world-space areas)
  > Slope filter
  > Mask filter
  > Normal source (face / smooth / normal map)
  > Orientation (blend between N and V, randomness)
  > Randomisation (yaw, tilt, scale)
  > Spacing filter (minimum distance)
  > Write to MultiMesh (replace or append, with undo)
```

Example combination: a terrain where a mask limits grass to paths' edges, a normal map adds micro-tilt, and alignment 0.7 keeps grass mostly upright.

#### 4.7 Settings and presets

- Store the settings used for the last populate on the `MultiMeshInstance3D` (as a `ScatterSettings` resource in node metadata) so reopening the dialog restores them and the scatter is reproducible from the scene file.
- Save and load presets as `.tres` files.
- Ship presets: "Upward faces (legacy)", "Follow surface", "Grass on terrain", "Masked scatter".

---

### 5. UI requirements

- Keep the toolbar button + dialog pattern from the reference. The dialog must be resizable and scrollable.
- Group options into collapsible sections: **Surface**, **Placement mask**, **Normals**, **Orientation**, **Randomisation**, **Spacing**, **Output**.
- Hide or disable options that do not apply (for example the threshold slider in Density mode).
- 0 to 1 values use a slider paired with a spinbox.
- Texture fields use `EditorResourcePicker` so users can drag textures in from the FileSystem dock.
- Status line after populating, for example: `Placed 812 / 1000 (120 rejected by mask, 68 by spacing). Seed 4821.`
- Errors and warnings show in the dialog (and via the editor toaster), not only in the output console. The reference hides the dialog on error, which loses the user's context; do not do that.
- Warnings to detect up front: surface has no UVs while a texture is set; texture cannot be read (handle compressed/VRAM textures by decompressing a copy); MultiMesh has no mesh.
- Generic builders (labelled spinbox, slider row, node picker, resource picker row, collapsible section) go into `addons/editor_tools_common/` for reuse by later sub-projects.

---

### 6. Known issues in the reference to fix during the port

The team should read the reference code, but not copy these problems:

1. `Mesh.GetFaces()` returns positions only. Mask, smooth normals and normal maps need UVs, normals and tangents, so read surface arrays per surface (`SurfaceGetArrays` or `MeshDataTool`), supporting indexed and non-indexed meshes and meshes with several surfaces.
2. The face normal is negated to correct for winding order. Verify the correct sign with a unit test on a known mesh (plane, cube, sphere) instead of assuming.
3. Triangle areas are computed in local space, so non-uniformly scaled surfaces are sampled unevenly. Use world-space areas.
4. `WeightedSampler` does a linear scan per sample. Use binary search over the cumulative areas.
5. Minimum-distance filtering is O(n squared) and drops candidates without retrying, so the result has fewer instances than requested. Use a spatial hash grid and retry up to the attempt limit.
6. Instances are never aligned to the surface; rotation is always around world-up.
7. `ApplyToMultiMesh` creates a fresh MultiMesh and drops existing settings such as `use_colors` and `use_custom_data`. Preserve them.
8. Settings live only in memory and are lost when the editor restarts (solved by 4.7).

---

### 7. Milestones

Each milestone ends with a short demo (screenshots or a recorded run in the demo scene) and a board review before the next one starts.

| # | Milestone | Content |
|---|---|---|
| M0 | Foundations | Repo layout, `CONVENTIONS.md`, demo Godot project, GdUnit4 running, `editor_tools_common` with the UI builders extracted from the reference |
| M1 | Parity port | Pipeline architecture, full surface data extraction, fixed samplers, seed, undo/redo, append/replace, settings persistence. Must produce the same kind of result as the reference |
| M2 | Orientation | Normal source (face/smooth), target vector, alignment slider, alignment randomness, local up axis, slope filter |
| M3 | Mask texture | All of 4.3 |
| M4 | Normal map | All of 4.4 |
| M5 | Combination and polish | Presets, UI polish, status reporting, addon README with screenshots, demo scenes for each mode, CHANGELOG, version 1.0.0 |

#### Task sizing

Break milestones into small, self-contained tickets: one class or one UI section per ticket, each with its own acceptance check and a clear list of files to touch. Core math tickets (sampling, barycentrics, slerp, mask lookup, TBN transform) should come with unit tests written in the same ticket. The team lead reviews every merge; the board only reviews at milestone boundaries unless something in section 10 needs deciding.

---

### 8. Acceptance criteria

Automated (GdUnit4) unless noted:

- **Determinism:** same seed + same settings + same scene produces identical transforms.
- **Alignment 0** on a sphere: every instance's up axis is within 1 degree of the radial direction (smooth normals).
- **Alignment 1:** every instance's up axis is within 1 degree of `V`, for several `V` values including `-Y`.
- **Opposite vectors:** `N = -V` with alignment 0.5 produces valid (non-NaN, orthonormal) transforms.
- **Mask threshold:** checkerboard mask on a plane places 0 instances on black squares.
- **Mask density:** 50% grey mask on a plane accepts 50% +/- 3% of attempts over 10,000 samples.
- **Normal map neutral:** a flat normal map (RGB 128,128,255) gives the same result as no normal map.
- **Normal map tilt:** a constant tilted normal map tilts every instance by the expected angle (+/- 1 degree), with and without green flip.
- **Undo:** Ctrl+Z after populate restores the previous MultiMesh exactly; redo reapplies it. (Manual check.)
- **Append:** populating two surfaces in Append mode keeps the first surface's instances. (Manual check.)
- **Performance:** 50,000 instances on a 100,000-triangle mesh in under 3 seconds without minimum distance, under 6 seconds with it, on the board's dev machine. (Manual check, report timings.)
- **No regressions:** "Upward faces (legacy)" preset gives a visually equivalent result to the reference plugin on the demo terrain. (Manual check.)

### 9. Out of scope for v1

- Runtime (in-game) scattering or a scatter node that regenerates automatically.
- Brush or paint-to-scatter tools.
- LOD, culling, collision generation for instances.
- GDScript or GDExtension (C++) versions.
- Godot Asset Library publication.

These are candidates for later sub-projects; note ideas in the repo's `README.md` backlog rather than building them.

---

### 10. Release 1.1: GridMap surfaces

Decided by the board on 2026-11-10. Tracked under the epic *Scatter 1.1*. Everything is additive: presets and scenes from 1.0 keep working and give the same result.

#### 10.1 GridMap as a surface

- The node picker also accepts a `GridMap`. Its surface is the item mesh of every used cell, placed with the cell's position and orientation, the item's mesh transform, the GridMap's cell scale and its global transform. The `MeshLibrary` is never modified.
- **Covered faces are skipped.** A triangle that lies on one of its cell's six boundary faces is dropped when the neighbouring cell in that direction is occupied (by any item), so nothing is scattered inside stacked tiles. Triangles inside the cell, such as a ramp's slope, always count.
- **Item filter:** a list of item names with **Include** or **Exclude**. Empty uses every item.
- **Cell box:** an origin and size in GridMap cells. A size of 0 uses the whole map. Covered-face checks still look at neighbours outside the box.
- These options sit in a *GridMap* group of `ScatterSettings`. The dialog shows the group only when the picked surface is a GridMap, if `ToolDialog` can do that; otherwise it is shown and ignored for meshes.
- Each item mesh is read once and its triangles are reused for every cell that uses it.
- Verified on Godot 4.7 (card `scatter-1-1-gridmap-facts`): in GridMap space, a cell's mesh sits at `Transform3D(GetCellItemBasis(cell) scaled by CellScale, MapToLocal(cell)) * MeshLibrary.GetItemMeshTransform(item)`, which matches `GridMap.GetMeshes()` for every centring flag, orientation and cell scale tried. The cell itself always spans `cell * CellSize` to `(cell + 1) * CellSize`, whatever the centring flags, so a covered face is a triangle whose three vertices lie on one of those planes.

#### 10.2 Projected masks

- **Mask → Projection:** *UV* (default, as in 1.0) or *Top Down*. Top Down samples the mask at the point's world X and Z, stretched over the surface's area seen from above (the cell box for a GridMap with one), with +X to the right and +Z down in the image.
- It works for every surface kind, so one painted mask can cover a whole GridMap or several meshes. Normal maps stay per tile UV.

#### 10.3 Demo

- A scene with a small GridMap and its own tile library (blocks and ramps, built by a tool scene like the other demos), scattering grass on the open tops and slopes, with a projected mask for a path. It does not depend on the WFC demo.

#### 10.4 Not in 1.1

Viewport handles for the cell box, and scattering per item with different meshes, go to the README backlog.
