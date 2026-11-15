---
id: "dgn-m3-dialog-2026-11-15"
status: "review"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T18:37:12.000Z"
completedAt: null
labels: ["v1.0.0", "m3", "editor", "ui"]
order: "d1"
---
# DGN M3: Dialog with settings and actions

The `ToolDialog` with the GridMap picker, the embedded inspector for `DungeonSettings`, Generate and Clear Dungeon buttons, and the status area.

Brief: §5.

## Done when
- Manual check: every setting is editable, hidden options follow `_ValidateProperty`, the dialog survives an assembly reload.

## Results
Branch `godoodungeon-m1`.

- `Editor/GodoodungeonPlugin.cs`: the dialog edits a copy of the GridMap's stored `DungeonSettings` (or new settings), through `DialogInspectorFilterPlugin`, so only the tool's settings show. The status starts with the target, a missing MeshLibrary warning, and the WFC version warning when it applies. **Generate** becomes **Stop** while running.
- `DungeonSettings`: Randomise Seed, Seed and a **Clear Dungeon** tool button now sit at the top, ungrouped, as in WFC Generator. Then come the groups Tile Sets, Bounds, Rooms, Corridors and Limits. Seed is read-only while Randomise Seed is on, and the Door Tile drop-down follows the room tile set.
- There is no separate GridMap picker: the target is the GridMap selected when the menu item is pressed, as in WFC Generator.

Probe (headless editor, scratch copy). It opened `prototype_m2_dungeon.tscn`, selected the GridMap, pressed **GridMap → Generate Dungeon (GDDS)**, then drove the dialog:
- Open: the dialog shows the stored settings (seed 2026, 12 rooms). Status "Target: GridMap".
- Seed 77, Generate: "Generated 12 rooms, 20 doors and 19 corridor routes in 0.1 s. Seed 77." The GridMap changed, the record holds 646 cells, and the stored settings have seed 77.
- Undo (editor history): every cell restored, stored seed back to 2026. Redo reapplies.
- Clear Dungeon: "Cleared 646 cells of the dungeon." The 30 hand-placed crates stay and the record is empty. Undo brings all 676 cells back.
- A 220×220, 150-room run, Stop pressed at once: the button is back to Generate, "Generation stopped. The GridMap was not changed.", and the cells are unchanged.
- Two engine messages at the probe's forced quit (`HotReloadAssemblyWatcher`, debugger plugin) come from Godot itself, not from godoodungeon.

## Manual check (board)
1. Build, focus Godot, select a GridMap, and open **GridMap → Generate Dungeon (GDDS)**. Every setting is editable. Door Tile lists the room tiles once a room tile set is chosen. Seed greys out with Randomise Seed on.
2. Rebuild with the dialog open, then focus Godot (assembly reload). The dialog still works, and Generate still works.

## Board check (2026-11-15)
- Passed: every setting is editable, Door Tile lists the room tiles, Seed is read-only with Randomise Seed on, and the dialog opens after an assembly reload.
- **Bug: Generate was greyed out.** The M0 skeleton disabled the Generate button on its dialog. The dialog node survives assembly reloads, and the new `OpenDialog` never enabled it again (the headless probe started a fresh editor, so it did not see this). Fixed: `OpenDialog` enables the action and resets its text. Generate needs checking again.
