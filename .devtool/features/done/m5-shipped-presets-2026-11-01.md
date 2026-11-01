---
id: "m5-shipped-presets-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M5 Polish"
dueDate: null
created: "2026-11-01T01:36:45.000Z"
modified: "2026-11-01T01:39:22.000Z"
completedAt: "2026-11-01T01:39:22.000Z"
labels: ["resources"]
order: "a1"
---
# M5: Shipped presets

Brief §4.7: "Upward faces (legacy)" (exists), plus:
- **Follow surface:** smooth normals, alignment 0, no tilt.
- **Grass on terrain:** smooth normals, alignment 0.7, randomness 0.15, tilt 8°, random scale 0.3, slope 0–40°, min distance 0.15.
- **Masked scatter:** Threshold mask (0.5), upright. The user drops in a mask texture. Presets do not reference demo assets.

Generated with `ResourceSaver`. An engine test loads every preset.

## Result
`follow_surface.tres`, `grass_on_terrain.tres`, `masked_scatter.tres` (Density mode; the user adds a mask texture), plus the existing legacy preset. Only non-default values are stored, and loading replaces all settings. Engine test `Shipped_presets_load` checks names, no surface path and key values.
