---
id: "dgn-m3-edge-router-2026-11-15"
status: "review"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T18:22:42.000Z"
modified: "2026-11-15T18:33:50.000Z"
completedAt: null
labels: ["v1.0.0", "m3", "core"]
order: "c0"
---
# DGN M3: Route corridors room to room

The board saw corridors wrapping around rooms in `prototype_m2_dungeon.tscn` (screenshot, 2026-11-15). Guided paths joins every door to the *nearest* corridor, not to the room the door was made for, so a room's second door gets a corridor that runs round the room into the first one. Decision (2026-11-15): rooms are nodes of the path network, and godoodungeon routes its own corridors along the room graph, **keeping corridors apart**.

- For each graph edge, one corridor from a room's door lane to the neighbour's door lane: A* over the layout grid around rooms, clearance rings and hand-placed cells, with seeded noise from Path Wander. Corridors that meet share cells (T or cross). Running right next to another corridor without joining it costs extra, so parallel corridors (the M0 double walls) are avoided.
- The routed path faces become cell restrictions for the corridor solve: each routed cell may only use corridor variants with exactly its path faces, and every other cell only variants without path faces. WFC still chooses among variants (weights, props), and guided paths is no longer used in the corridor pass. WFC Generator keeps it unchanged.
- The doors of each edge are recorded, so every corridor joins the two doors it was made for.
- The corridor tile set needs every piece a route can make: straights, bends, T pieces and a cross, in all rotations. Corridor Height leaves the settings: v1 corridors stay on the bottom layer.

Brief: §4.2, §4.4, §6.1–2.

## Done when
- Core tests: every edge's two doors are joined; no path face dangles; one network; with clearance 1 no corridor touches a room's ring; corridors seldom run side by side without joining (measured); the same seed gives the same routes.
- The M2 review scene, rebuilt, shows no corridors wrapping around rooms (board check).

## Results
Branch `godoodungeon-m1`.

- `Core/CorridorRouter.cs`: `Route(layout, settings, occupied, wander, seed)` gives a `CorridorPlan`, a path face mask per layout cell in WFC directions.
  - **Blocked:** room footprints with their Wall Clearance, hand-placed cells, and all door lanes. The lanes get straight faces through the door.
  - **Search:** for each edge, in graph order, A* (with direction state) from door A's entry cell to door B's entry cell.
  - **Costs:** a new cell is 1 + seeded noise × Path Wander × 2, a shared corridor cell 0.3, +4 beside a corridor it does not join, +0.2 per turn.
  - A blocked edge gives an `Error` naming both doors.
- `DungeonLayout.EdgeDoors`: the door each room uses for each edge, recorded by `DoorPlacer`.
- `SolveInputs.Corridors(layout, plan, tiles, pathSocket, handPlaced)`: one layer, rooms and hand-placed cells excluded, and `CellRestrictions` per path mask (mask 0 for every other cell). A mask without variants gives `Error`. `SolveInput.Paths` became `Restrictions`.
- `DungeonJob`: route, then solve with restrictions (no guided paths). A routing error goes to the next layout attempt as "Corridors: …". `DungeonResult.CorridorFaces` was added.
- `DungeonTileRules`: bends (`HasBends`) and a cross (`HasCross`) are now required, next to straights, T pieces and the empty tile.
- **Corridor Height** was removed from `DungeonSettings` and `DungeonRequest`. Corridors stay on the bottom layer.
- `LayoutText` draws corridors (`-`, `|`, `+`). The review builder prints them.
- Brief: §1, §3, §4.2, §4.4 (new Corridor routing), §4.8, §6.1–2, §11.

Checks: Core tests **357 passed**. `DungeonCorridorTests` replaces the guided-paths corridor tests:
- Over 30 seeds: every edge's two doors are joined, no path face dangles, the solved faces equal the plan, the rooms join everything into one network (connectivity check), and every corridor cell leads to a door.
- Clearance 1 keeps the rings free, and clearance 0 lets corridors run along walls.
- **4 side-by-side pairs over 2,813 corridor cells in 50 dungeons.**
- Routes are fixed for a seed, and wander changes them. Hand-placed cells are avoided. A hand-placed wall across the bounds is reported. A missing cross is reported.

Also: new tile rule tests for bends and the cross; a job test where a hand-placed wall makes every attempt fail with "Corridors: No corridor could join room …". Engine tests 98 passed.

**Rebuilt review scene** (`prototype_m2_dungeon.tscn`, seed 2026): 10 corridors over 84 cells, where guided paths made 15 routes over 153 cells.
```
                                                
  #####                                         
  #...#   #####                           ##### 
  #...#   #...#        #########          #...# 
  #...D-+ #...#    +---D.......#          #...# 
  #...# +-D...# +--+   #.......D-+        #...# 
  #...# | #...D-+      #.......# |        #...# 
  #...# | #####        #.......# |        #...# 
  #...# |              #.......# +-+      #...# 
  ##### +---+          #########   |      #D### 
            |                      |       |    
            |                      +------++    
            |                             |     
            |                             |     
            |                             |     
       #####D###                          |     
       #.......#                        +-+     
       #.......#                        |       
       #.......#                        |       
       #.......#                        |       
       #.......#    XXXXXX        ######D#      
       #.......#    XXXXXX        #......#      
       #D#######    XXXXXX        #......#      
        |                         #......#      
     +--+                         #D######      
     |                             |            
 ####D#                            +-+          
 #....#                              |          
 #....#                              |          
 #....#                              |          
 #....#                              |          
 ######                              |          
                  #########          |          
                  #.......#     #####D##        
                  #.......# +---D......#        
                  #.......D-+   #......#        
      XX          #.......#     #......#        
      XX          #########     #......#        
      XX                        ######D#        
      XX                              |         
      XX                              +--+      
      XX                                 |      
                                        #D###   
                                        #...#   
                                        #...#   
                                        #...#   
                                        #####   
                                                

```

## Manual check (board)
Open `demo/godoodungeon/prototype/prototype_m2_dungeon.tscn`. Corridors run from door to door and no longer wrap around rooms.
