---
id: "dgn-m0-shared-menu-inspector-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T12:20:07.000Z"
modified: "2026-11-15T13:10:17.000Z"
completedAt: "2026-11-15T13:10:17.000Z"
labels: ["v1.0.0", "m0", "common", "editor", "ui"]
order: "a45"
---
# DGN M0: Shared GridMap menu and tile set inspector

After the solver move: make `SpatialMenuHook` share one fallback button between addons, and move `TileSetPreview`, `TileSetInspectorPlugin` (with the elected-instance guard) and `BoundsGizmoPlugin` to `editor_tools_common/Wfc/Editor/`. Design and probe results are on `dgn-m0-verify-toolbar-inspector`.

Brief: §3 Layout, §5, §6.5, §10.3–4.

## Done when
- The public API of `SpatialMenuHook` is unchanged. MultiMesh Scatter still hooks into Godot's MultiMesh menu and renames its item.
- Engine or probe tests cover two hooks on one fallback button in both enable orders: unique ids, the right callback per item, removing either hook leaves the other's item, and the last one frees the button.
- With two inspector plugin instances, one preview shows, and still one after either is removed.
- Manual check in the editor: with WFC Generator and the godoodungeon skeleton both enabled, rebuild and focus the editor (assembly reload). The menu still has both items and the tile set shows one preview.

## Results
Branch `godoodungeon-m0` (not committed yet).

- **`SpatialMenuHook`** (`editor_tools_common/Editor/`). The fallback button carries `godoodads_menu_users` metadata (hook instance id → wanted visibility). A hook that finds a button with this metadata joins it instead of taking it as a built-in host. Items get the first free id from 7300. `SetFallbackVisible` records the hook's wish, and the button shows while any live user asks. `Remove` removes only the hook's own items, and the last user takes the button out and frees it. Users freed without `Remove` are pruned. The public API is unchanged, and built-in hosting (MultiMesh) works as before.
- **Moved with `.uid`** to `editor_tools_common/Wfc/Editor/` (namespace `Godoodads.Common.Wfc.Editor`): `TileSetPreview`, `TileSetInspectorPlugin`, `BoundsGizmoPlugin`. Editor scripts are not referenced by saved resources, so they need no legacy stubs.
- **`TileSetInspectorPlugin.Register(plugin)` / `Unregister(plugin, inspector)`.** The instances sit in a list kept as metadata on the editor's base control. `_CanHandle` is true only for the first live instance.
- **`BoundsGizmoPlugin.Owner`** is now an `EditorPlugin` that implements the new `IBoundsBoxes` interface, and `GizmoName` is settable. WFC Generator implements `IBoundsBoxes` and calls itself "WFC Generator bounds" as before.
- CONVENTIONS.md: layout (`Wfc/Editor/`), namespaces, and UI pattern item 1 (shared button and shared inspector registration).

### Probe (headless editor, scratch copy, MultiMesh Scatter + WFC Generator + a probe plugin with two hooks and one inspector instance), both enable orders

| Step | WFC first | Probe first |
|---|---|---|
| All three on one button | `[7300] WFC`, `[7301] One`, `[7302] Two` | `[7300] One`, `[7301] WFC`, `[7302] Two` |
| Press One, press Two | only its own callback each | the same |
| P1 asks visible / nobody asks | visible / hidden | the same |
| Disable WFC | One and Two stay | the same |
| Enable WFC again | WFC appended, no duplicates | the same |
| Remove P1, press Two | Two and WFC stay, Two's callback runs | the same |
| Remove P2 | WFC only | the same |
| Disable WFC (no users) | button gone | the same |
| MultiMesh menu | `[0] Populate Surface (Godot)`, `---`, `[7300] Populate Surface (GDDS)` | the same |
| Previews (WFC + probe / WFC off / WFC back / WFC only / none) | 1 / 1 / 1 / 1 / 0 | the same |

Items that come back are added at the end of the menu, so the order can change after re-enabling an addon. This is cosmetic.

### Tests
- `dotnet build`: 0 errors, 0 warnings. Core tests 250 passed. Engine tests 83 passed. Headless editor load: no errors or warnings.

## Manual check (once `dgn-m0-plugin-skeleton` exists)
With WFC Generator and godoodungeon both enabled: rebuild, then focus the editor (assembly reload). The GridMap menu should still have both items, each item should open its own dialog, and a tile set should show one preview. Then disable and enable each addon in turn.

## Board check passed (2026-11-15)
After a rebuild and an assembly reload, both GridMap menu items open their own dialog and a tile set shows one preview. Toggling godoodungeon and WFC Generator in turn leaves no errors.
