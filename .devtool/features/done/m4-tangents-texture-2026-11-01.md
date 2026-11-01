---
id: "m4-tangents-texture-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M4 Normal map"
dueDate: null
created: "2026-11-01T01:22:52.000Z"
modified: "2026-11-01T01:27:30.000Z"
completedAt: "2026-11-01T01:27:30.000Z"
labels: ["adapters"]
order: "a1"
---
# M4: Tangent generation and normal map texture reading

- `SurfaceSource`: when a tangent-space normal map is used and a surface has no tangents, run `SurfaceTool.GenerateTangents` on a **copy** of its arrays and report it, so the dialog can show a notice. The user's mesh is never modified.
- Shared texture reading (copy, decompress, convert) moves out of `MaskSource` so `NormalMapSource` can reuse it. Object-space maps need all three channels: an RG-only format (for example BC5, Godot's default for normal maps) is an error.

**Engine tests**
- Tangents are generated without changing the source mesh.
- On Godot's PlaneMesh the binormal is `cross(N, T) * w = -Z` (green up points to the top of the image).
- An RG8 normal map works in tangent space and is refused in object space.

## Result
- `Adapters/TextureReader.cs` (shared copy, decompress and convert; `IsTwoChannel`); `MaskSource` refactored onto it.
- `Adapters/NormalMapSource.cs` (object space refuses RG-only formats).
- `SurfaceSource.FromNode/FromMesh(..., ensureTangents, out generatedTangents)` runs `SurfaceTool.GenerateTangents` on a copy.
- **Finding:** Godot 4.2+ stores *placeholder* tangents for meshes with normals but no tangents (`rendering_server.cpp:565`: `normalize(cross((n.z, -n.x, n.y), n))`, w = 1), and `SurfaceGetArrays` returns them. `SurfaceSource.HasRealTangents` detects them and treats them as missing.
- Engine tests in `NormalMapAdapterTests`: PlaneMesh frame (T = +X, B = -Z), placeholder detection and replacement on a copy (user mesh unchanged), generated tangents follow the convention, RG8 works in tangent space and is refused in object space, end-to-end green-up map leans toward -Z by 20°.
