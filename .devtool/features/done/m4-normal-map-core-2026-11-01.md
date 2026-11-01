---
id: "m4-normal-map-core-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M4 Normal map"
dueDate: null
created: "2026-11-01T01:22:52.000Z"
modified: "2026-11-01T01:24:41.000Z"
completedAt: "2026-11-01T01:24:41.000Z"
labels: ["core"]
order: "a0"
---
# M4: Normal map normal source (Core)

Brief §4.4, item 3. Uses exactly the formulas of Godot's 4.7 forward shader (`scene_forward_clustered.glsl`), so instances follow the normal Godot renders:
- **Tangent space:** `xy = rgb.xy * 2 - 1`, `z = sqrt(1 - x² - y²)` (blue is always ignored, so RG-packed maps work); `B = normalize(cross(N, T) * sign(tangent.w))`; `n = normalize(mix(N, T·x + B·y + N·z, strength))` (line 1442).
- **Object space:** `xyz = rgb * 2 - 1`, transformed with the surface's normal matrix, then the same mix.
- **Flip green** negates y (DirectX-style maps).
- `N` is the smooth normal; `T` is the interpolated tangent, made orthogonal to `N`.

**Tests (§8)**
- A neutral map (128, 128, 255) gives the same result as no map (within 0.5°).
- A constant tilted map tilts every instance by the expected angle (±1°), with and without green flip, in the expected direction.
- Strength 0 equals the smooth normal; object space with a rotated surface.

## Result
`Core/NormalMap.cs` (`NormalMap`, `NormalMapOptions.Apply`) with the Godot shader formulas; `SurfaceData.TangentAt` (Gram-Schmidt against N, binormal sign; fixed perpendicular fallback) and `SurfaceData.NormalBasis`; `NormalSource.NormalMap`. Tests in `NormalMapTests`: neutral map within 0.5° of no map, red 20° tilt toward +T, green 20° toward the Godot binormal (-Z) and toward +Z when flipped (±1°), strength 0 and 0.5, object space with a rotated mesh, Face ignores the map, fallback without tangents.
