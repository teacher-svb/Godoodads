---
id: "dgn-m3-layout-preview-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:52:31.000Z"
completedAt: "2026-11-15T18:52:31.000Z"
labels: ["v1.0.0", "m3", "editor"]
order: "d3"
---
# DGN M3: Preview Layout overlay

Preview Layout runs only the layout and draws rooms, doors and graph edges as an overlay that is never saved. Changing a layout setting or the seed redraws it.

Brief: §5.

## Done when
- Manual check: the overlay matches the generated rooms for the same seed and leaves nothing in the saved scene.

## Results
Branch `godoodungeon-m1`.

- `Core/DungeonPreview.cs`: `Plan(request)` runs Generate's own layout attempts, seeds and corridor router, and skips the WFC solves. The result is the layout, the corridor plan, the attempt used, the failures and the errors. Fast enough for the editor's main thread. `DungeonJob.OccupiedGrid` is now public and shared with it.
- `DungeonSettings.PreviewLayout`: a checkbox at the top, under Clear Dungeon.
- Plugin: while the dialog is open with Preview Layout on, the preview is recomputed whenever a setting it depends on changes (bounds, rooms, corridors, wander, seed, attempts). The hand-placed cells are read again on each recompute. It draws into the bounds overlay, which has no owner and is never saved:
  - **rooms** as cyan boxes of their height,
  - **doors** as small yellow boxes,
  - **corridors** as green lines along their cells on the floor.

  The status says "Preview: n rooms, d doors, c corridors. Seed s." plus layout warnings, or why no layout works.
- The card asked for graph edges. The preview draws the routed **corridors** instead. Since `dgn-m3-edge-router` they *are* the graph edges, drawn as Generate will build them.
- The Generate status now says "corridors" too, as the preview does.

Checks:
- Core tests **359 passed**. 2 new in `DungeonPreviewTests`: over 20 seeds the preview equals Generate's layout attempt, rooms, doors and corridor faces; invalid settings and a corridor blocked by a hand-placed wall explain themselves.
- Probe (headless editor, real dialog, `prototype_m2_dungeon.tscn`):
  - The bounds alone are 24 overlay vertices. With Preview Layout on: 988, with "Preview: 10 rooms, 16 doors, 10 corridors. Seed 2026." and the shortfall warning.
  - Seed 5 redraws: "10 rooms, 19 doors, 10 corridors".
  - Generate with seed 5 gives the same: "Generated 10 rooms, 19 doors and 10 …".
  - Preview off brings it back to 24 vertices. The saved scene contains no overlay.
- Hand-placed cells added while the dialog stays open show up at the next settings change. Turning Preview Layout off and on also refreshes it.

## Manual check (board)
1. Open the dialog and tick Preview Layout. The cyan rooms, yellow doors and green corridors appear inside the violet bounds.
2. Change Seed or Room Count: the drawing follows at once.
3. Press Generate. The dungeon fills exactly what the preview showed.
4. Save the scene and reopen it. No preview lines are saved.

## Board check passed (2026-11-15)
The preview appears inside the bounds, follows Seed and Room Count at once, and Generate fills exactly what it showed. After saving and reopening the scene, no preview lines or bounds box are left. The overlay colours become sRGB with the bounds-box fix.
