---
id: "wfc-1-3-path-demo-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T14:11:33.000Z"
completedAt: "2026-11-10T14:11:33.000Z"
labels: ["demo"]
order: "e4"
---
# WFC 1.3: Hillside path tiles

Path tiles for the hillside ruin: straight, corner, T, cross, end, path stairs and the cell above the stairs, plus a `hillside_paths` scene with two placed path ends. `WfcDemoAssetTests` covers the new tiles.

Brief: §13.5.

## Done when
- The demo builder makes the tiles and scene; the paths scene solves for 20 of 20 seeds at its default size and its path climbs at least once on some seeds.

## Result
Path tiles (end, straight, corner, T, cross, path stairs cut into a ramp, empty stair top) in a second tile set `ruin_paths_tileset` (the base set and its scenes are unchanged; path items are appended to the library). `hillside_paths` has two placed ends at (1,3,6) and (30,5,25). Findings on the way: (1) the router now tracks the arriving socket and the chosen variant per route cell, because stairs climbing one way next to a stair top turned another way passed the face check; (2) stairs with open sides cannot meet a hill edge, so the path stairs use ramp sockets on their sides; (3) Generate tries up to 8 routes (derived seeds, deterministic) when the solve proves impossible, with tie noise so wander 0 can also vary. User decision: keep route-first, terrain-first A* to the backlog. Demo test: 20 of 20 seeds solve and climb. Core 220, engine 61 pass. Waiting for the manual editor check with the path editor card.

## Result
Manual check passed (2026-11-10): generate on hillside_paths joins both ends with a climbing path and reports the route; seeds and wander change the route; regenerate replaces it; pinned stretches stay; an erased end gives "leads nowhere" with a red cell and no change; works after a reload.
