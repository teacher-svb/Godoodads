---
id: "m5-demo-scenes-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M5 Polish"
dueDate: null
created: "2026-11-01T01:36:45.000Z"
modified: "2026-11-01T01:40:50.000Z"
completedAt: "2026-11-01T01:40:50.000Z"
labels: ["demo"]
order: "a3"
---
# M5: Demo scene per mode

`demo/multimesh_scatter/`:
- `demo_follow_surface.tscn` (sphere, alignment 0)
- `demo_mask.tscn` (terrain and the paths mask)
- `demo_normal_map.tscn` (terrain and the bumps map)
- `demo_combined.tscn` (the §4.6 example: mask, normal map and alignment 0.7)
- `demo_legacy.tscn` (cube and terrain with the legacy preset)

Each is pre-populated and stores its settings, so the dialog reopens with them. Built by a C# generator scene run headless, so they can be rebuilt after changes.

## Result
Scenes `demo_follow_surface`, `demo_mask`, `demo_normal_map`, `demo_combined` (brief §4.6 example: grass preset + paths mask + bumps normal map) and `demo_legacy` (plateau with a steep cone and the legacy preset). Each stores its settings in the `Scatter` metadata. Rebuilt by `demo/multimesh_scatter/tools/build_demos.tscn` (`DemoBuilder.cs`, same steps as the dialog): run headless, all placed in full, saves Ok. The metadata key moved to `ScatterSettings.MetaKey`.
