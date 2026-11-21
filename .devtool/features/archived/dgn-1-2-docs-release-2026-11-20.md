---
id: "dgn-1-2-docs-release-2026-11-20"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-20T14:00:00.000Z"
modified: "2026-11-20T17:40:00.000Z"
completedAt: "2026-11-20T17:40:00.000Z"
labels: ["v1.2.0", "docs", "release", "acceptance"]
order: "h6"
---
# godoodungeon 1.2: README, CHANGELOG, version, acceptance

README "Room boxes" section; the hand-built room limitation points at Built boxes. CHANGELOG 1.2.0, plugin.cfg 1.2.0, CLAUDE.md project line. Acceptance pass of §13.7 recorded here, including item 5 (godoowave 1.5 installed next to it).

Brief: §13.6, §13.7.

## Done when
- Docs updated; every acceptance item has a recorded result; package builds and smoke-tests locally.

## Results (2026-11-20)
- README: *Room boxes* section (adding, Generated and Built, doors, Room Count, rules and red errors, Enabled, Data into the graph, status and preview colours); *Cells you place yourself* and the Limitations line point at Built boxes; graph table lists `Box` and `IsBuilt`; demo section names `dungeon_rooms.tscn`; *With godoowave* asks for godoowave 1.5 or later when both are installed. CHANGELOG 1.2.0, `plugin.cfg` 1.2.0, root README table, download example and backlog line, CLAUDE.md.
- Package: `godoodungeon-1.2.0.zip` contains `Nodes/`, the room box gizmo, `Core/RoomBoxes.cs` and the shared `editor_tools_common/Nodes/`; `smoke_test_addon.sh` loads it cleanly.

## Acceptance pass, brief §13.7
| # | Criterion | Result |
|---|---|---|
| 1 | 50 seeds, two boxes (one per mode), 10 rooms: 10 rooms, rooms 1 and 2 at the boxes, every Built door joined, Built cells unchanged | Pass: engine `Over_50_seeds_with_10_rooms_…` at the brief's 64 × 64 (regenerating over each other). At 48 × 48 one seed placed 9 of 10 rooms with the shortfall warning, as without boxes. |
| 2 | Generate, regenerate and Clear Dungeon never change a Built box's cells; a Generated box's cells are replaced | Pass: engine `Regenerate_and_clear_dungeon_…`, demo test over three seeds, board check |
| 3 | Every check of 13.2 gives its error with the box named | Pass: Core `Every_check_names_the_box` and the blocked-lane test; engine: an invalid box fails once (Invalid, no attempts); board: missing door names the box and draws it red |
| 4 | The graph names each box room and carries its Data after regenerating | Pass: engine `The_graph_names_each_box_…` (two seeds, copy independent, rebuilt Core graph), demo test |
| 5 | godoowave 1.5 and godoodungeon 1.2 installed together | Pass: both zips in one clean project (godoodungeon's last), both plugins enabled: builds with 0 warnings and 0 errors, loads cleanly; in this repo both run together and all engine tests pass |

Core 519/519, engine 153/153.
