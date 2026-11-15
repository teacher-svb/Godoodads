---
id: "dgn-m2-review-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:13:50.000Z"
completedAt: "2026-11-15T18:13:50.000Z"
labels: ["v1.0.0", "m2", "review"]
order: "c7"
---
# DGN M2: Milestone review

Demo for the board. The board moves this card to *Done* when approved. Do not start the next milestone before that.

## Review checklist for the board
1. Run the prototype tile sets through a test scene that calls the pipeline (no dialog yet) and look at the result.
2. Read the test results.

## Material for the board (2026-11-15)

All M2 work cards are done: settings resource, room inputs, corridor inputs, run order, connectivity check, GridMap write. Core tests 354 passed, engine tests 98 passed.

**Review scene:** `demo/godoodungeon/prototype/prototype_m2_dungeon.tscn`, built by `build_m2_dungeon.tscn` (`BuildM2Dungeon.cs`). It runs the whole pipeline without the dialog (settings → check → hand-placed cells → `DungeonJob` → write → record) on the M0 prototype tile sets. Settings: 48×48, 12 rooms, seed 2026, defaults otherwise. Two blocks of crates were placed by hand first (`X` below).

Run log:
```
M2 dungeon: Solved in 70 ms, layout attempt 1, 10 rooms, 16 doors, 15 routes over 153 cells, 0 backtracks, 0 restarts.
M2 dungeon: wrote 622 cells; 30 hand-placed cells kept.
```

Layout (`#` wall, `.` floor, `D` door, `X` hand-placed; corridors are not drawn here, they are in the scene):
```
                                                
  #####                                         
  #...#   #####                           ##### 
  #...#   #...#        #########          #...# 
  #...D   #...#        D.......#          #...# 
  #...#   D...#        #.......D          #...# 
  #...#   #...D        #.......#          #...# 
  #...#   #####        #.......#          #...# 
  #...#                #.......#          #...# 
  #####                #########          #D### 
                                                
                                                
                                                
                                                
                                                
       #####D###                                
       #.......#                                
       #.......#                                
       #.......#                                
       #.......#                                
       #.......#    XXXXXX        ######D#      
       #.......#    XXXXXX        #......#      
       #D#######    XXXXXX        #......#      
                                  #......#      
                                  #D######      
                                                
 ####D#                                         
 #....#                                         
 #....#                                         
 #....#                                         
 #....#                                         
 ######                                         
                  #########                     
                  #.......#     #####D##        
                  #.......#     D......#        
                  #.......D     #......#        
      XX          #.......#     #......#        
      XX          #########     #......#        
      XX                        ######D#        
      XX                                        
      XX                                        
      XX                                        
                                        #D###   
                                        #...#   
                                        #...#   
                                        #...#   
                                        #####   
                                                

```

### Findings during M2
- **Door lane rule** (`dgn-m2-corridor-inputs`): doors may not share a lane or entry cell, or guided paths deadlocks. Routing now fails in at most 2 of 100 layouts, and the layout attempts cover it.
- **T pieces** (`dgn-m2-gridmap-write`): the corridor tile set needs T pieces in all four ways, or only two doors can be joined. This is a §4.2 rule now.
- The defaults place 10 of 12 rooms here (48×48 is tight with gap 3). The warning reports it.

## Board review (2026-11-15): approved
- `prototype_m2_dungeon.tscn`: the rooms are closed and every door is joined by corridors. Corridors keep one cell off room walls except at doors.
- The two crate blocks are the hand-placed obstacles placed before generating. They are untouched, and rooms and corridors go around them. At first they looked like rooms without walls. The board keeps them as crates now that this is explained.
- Confirmed the M2 brief rules: doors never share a lane or entry cell (§4.3, §6.2), and the corridor tile set needs T pieces in all four ways (§4.2).
- M2 approved. M3 (editor) can start.
