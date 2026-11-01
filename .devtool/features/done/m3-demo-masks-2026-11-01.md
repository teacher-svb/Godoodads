---
id: "m3-demo-masks-2026-11-01"
status: "done"
priority: "low"
assignee: "claude"
epic: "M3 Mask texture"
dueDate: null
created: "2026-11-01T01:07:27.000Z"
modified: "2026-11-01T01:11:54.000Z"
completedAt: "2026-11-01T01:11:54.000Z"
labels: ["demo"]
order: "a3"
---
# M3: Demo mask textures

`demo/multimesh_scatter/assets/`:
- `mask_checker.png` (8×8 checkerboard)
- `mask_paths.png` (paths, white = grass)
- `mask_grey50.png` (flat 50% grey)

Generate them with Godot's `Image` and import them through godot-mcp `import_assets`.

## Result
Generated with Godot `Image` (256 px, L8): `mask_checker.png` (8×8), `mask_paths.png` (soft white bands along the edges of two winding paths), `mask_grey50.png`. Imported via godot-mcp `import_assets` (0 errors, 0 warnings). The engine test `Imported_demo_texture_is_readable` reads the imported checker through `FromTexture`.
