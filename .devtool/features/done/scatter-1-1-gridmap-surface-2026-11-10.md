---
id: "scatter-1-1-gridmap-surface-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "Scatter 1.1"
dueDate: null
created: "2026-11-10T16:00:00.000Z"
modified: "2026-11-10T16:50:00.000Z"
completedAt: "2026-11-10T16:50:00.000Z"
labels: ["adapters", "core"]
order: "g1"
---
# Scatter 1.1: GridMap surface source

`SurfaceSource` reads a GridMap: per used cell, the item mesh's triangles (read once per item) with the full cell transform; covered faces dropped by a Core check (triangle on a cell boundary face whose neighbour is occupied); item filter and cell box.

Brief: §10.1.

## Done when
- Core tests: covered-face detection for blocks, ramps and partial shapes.
- Engine tests: positions match the rendered cells for every orientation and a non-default cell scale; stacked blocks give only outer faces; filter and box; the MeshLibrary is unchanged.

## Result
- `SurfaceSource.FromGridMap(map, GridMapSelection, ...)`: every used cell in the box and item filter adds its item's surfaces (read once per item) with the cell placement as a new `SurfaceArrays.Local`. Which triangles lie on a cell face is cached per (item, orientation); a triangle on a face with an occupied neighbour (any item, also outside the box) is dropped. `FromNode` handles GridMap, and `IsSupported` accepts it.
- `SurfaceData.Build` applies `toWorld * Local` per surface, with the normal basis and mirroring sign per surface; `NormalBasisOf(triangle)` is used for object-space normal maps. Meshes and CSG give the same data as before.
- Core `GridFaces.BoundaryFace` with 8 Core tests (six faces, slopes, partial shapes, tolerance).
- Engine tests (`GridMapSurfaceTests`, 6): positions match `GridMap.GetMeshes()` within 2e-6 for rotated, scaled and uncentred cells and a moved GridMap; two blocks side by side plus one on top lose exactly their 8 shared triangles; a post on a block hides the block top and the post bottom; box with an outside neighbour; include and exclude by name; per-cell normal bases; the MeshLibrary is unchanged.
- Core 228, engine 67 pass.
