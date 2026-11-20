---
id: "dgn-m0-plugin-skeleton-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T13:10:17.000Z"
completedAt: "2026-11-15T13:10:17.000Z"
labels: ["v1.0.0", "m0", "editor"]
order: "a6"
---
# DGN M0: Dungeon Generator addon skeleton

`addons/godoodungeon/` with `plugin.cfg`, README and CHANGELOG stubs, and the folder layout from §3. The plugin loads, and a toolbar entry "Generate Dungeon (GDDS)" appears for a selected GridMap, as an item in the shared GridMap menu (`dgn-m0-shared-menu-inspector`). The entry opens an empty `ToolDialog`. The plugin warns (status and toaster) when `addons/wfc_generator/plugin.cfg` is older than 1.3.3 (brief §4.1).

Brief: §3, §5.

## Done when
- The headless editor load shows no plugin errors.
- Enabling and disabling the plugin and rebuilding the assembly leave no errors (manual check).

## Results
Branch `godoodungeon-m0` (not committed yet).

- `addons/godoodungeon/`: `plugin.cfg` (version 0.1.0), README (status, and how it works with WFC Generator), and a CHANGELOG with an Unreleased entry.
- `Editor/GodoodungeonPlugin.cs` (`Godoodads.Dungeon.Editor`):
  - "Generate Dungeon (GDDS)" in the shared GridMap menu, and `TileSetInspectorPlugin.Register`.
  - A `ToolDialog` that shows the target, says generation is not built yet, and has Generate disabled.
  - Reload-safe: method-name callables, Variant-only state, no lambdas.
- `Core/WfcGeneratorVersion.cs` (`Godoodads.Dungeon.Core`): `NeedsUpdate(version)` against the minimum 1.3.3. The plugin reads `res://addons/wfc_generator/plugin.cfg` and warns through the toaster on load, and in the dialog's status. In this repo the warning shows until `dgn-m0-wfc-1-3-3` bumps WFC Generator to 1.3.3.
- Enabled in `project.godot`.
- `Resources/` and `Adapters/` folders come with their first files (M2). Git keeps no empty folders.

Checks:
- `dotnet build`: 0 errors, 0 warnings. Core tests **259 passed** (9 new in `DungeonWfcVersionTests`). Engine tests 83 passed.
- Headless editor load: no errors.
- Probe (headless editor, scratch copy): after opening `hillside_free.tscn` and selecting its GridMap, the shared button is visible with `[7300] Generate WFC (GDDS) | [7301] Generate Dungeon (GDDS)`. Pressing the dungeon item opens its dialog (Generate disabled). A tile set shows 1 preview. Disabling godoodungeon removes its item and dialog, and enabling it again adds the item back once.

## Manual check (board)
1. Build, focus the editor, and check that the plugin list shows godoodungeon enabled. A toast warns that WFC Generator 1.3.2 is installed (expected until 1.3.3).
2. Select a GridMap: **GridMap → Generate Dungeon (GDDS)** opens the empty dialog.
3. Rebuild with the dialog closed, focus the editor (assembly reload), and open both menu items again. Then disable and enable godoodungeon and WFC Generator in turn. Expect no errors in the Output panel. This also covers the manual check on `dgn-m0-shared-menu-inspector`.

## Board check (2026-11-15)
- Step 2 confirmed by screenshot: the dialog opens on a GridMap with the target, the not-built-yet note and the WFC 1.3.2 warning, and Generate is disabled.
- Steps 1 and 3 (reload, toggling the addons) not reported yet.

## Board check passed (2026-11-15)
After a rebuild and an assembly reload, both GridMap menu items open their own dialog and a tile set shows one preview. Toggling godoodungeon and WFC Generator in turn leaves no errors.
