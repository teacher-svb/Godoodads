# godoodungeon

Generate dungeons on a `GridMap` from the Godot editor: a layout places rooms and their doors, wave function collapse fills every room from a room tile set, and corridors join all rooms through a corridor tile set.

**Status:** in development. This version is a skeleton: the **GridMap → Generate Dungeon (GDDS)** menu item opens an empty dialog. The plan is in [docs/briefs/dungeon_generator.md](../../docs/briefs/dungeon_generator.md).

**Requirements:** Godot 4.7 or later, the **.NET** edition, and the [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or later.

## Works with WFC Generator

godoodungeon uses the WFC solver and tile set resources in `addons/editor_tools_common/Wfc/`, which it shares with [WFC Generator](../wfc_generator/README.md). Both addons can be enabled at once: their items share the **GridMap** menu, and a tile set shows one preview. If WFC Generator is installed too, it must be **1.3.3 or later**. Older versions keep their own copy of the tile set classes, so tile sets would load as types godoodungeon cannot use. The plugin warns when it finds an older version.
