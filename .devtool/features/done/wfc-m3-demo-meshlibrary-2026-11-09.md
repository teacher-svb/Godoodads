---
id: "wfc-m3-demo-meshlibrary-2026-11-09"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC M3 Demo tileset"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:51:14.000Z"
completedAt: "2026-11-09T18:51:14.000Z"
labels: ["demo"]
order: "aI"
---
# WFC M3: Demo modules and MeshLibrary

A build script that creates the stacked hillside ruin modules with simple generated meshes: ground, walls, floors, stairs, ramps, bridges. Output is a `MeshLibrary`.

Brief: §7.

## Done when
- The script rebuilds the library headless and every module is visually distinct in the editor.

## Result
`demo/wfc_generator/tools/BuildWfcDemo.cs` builds `assets/ruin_library.tres` headless from generated meshes: rock, 16 grass variants, paving, ramp, outer and inner corners, wall set, tower, pillar, stairs, plinth, bridge head and deck.
