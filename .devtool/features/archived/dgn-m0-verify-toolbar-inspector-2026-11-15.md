---
id: "dgn-m0-verify-toolbar-inspector-2026-11-15"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-15T12:20:07.000Z"
labels: ["v1.0.0", "m0", "spike"]
order: "a2"
---
# DGN M0: Verify toolbar sharing and the tile set inspector

Check brief §10 items 3 and 4: can two plugins add items to one fallback button through `SpatialMenuHook`, and how can `TileSetInspectorPlugin` be shared so it is registered once when both addons are enabled, and still present when only one is.

Brief: §5, §6.5, §10.

## Done when
- Findings are recorded on this card with a proposal for both questions.
- The brief is updated with the decision.

## Results (Godot 4.7, headless editor, scratch copy with a probe plugin)

### Toolbar (brief §10.3)
A probe plugin used `SpatialMenuHook(this, "GridMap", "GridMap")` next to WFC Generator, enabled in both orders. It printed the GridMap buttons and their items, pressed items, and disabled and re-enabled WFC Generator.

| Step | WFC first | Probe first |
|---|---|---|
| Both enabled | one button: `[7300] Generate WFC`, separator, `[7300] Probe Dungeon` | the same, in the other order |
| Press either item | **both callbacks run** (same id 7300) | the same |
| Disable WFC | **button gone**, the probe's item with it | WFC removes the first id 7300, the **probe's item**, and leaves its own |
| Re-enable WFC | new button with only WFC's item | **duplicate** WFC items, no probe item |

Cause: `FindHost` matches any button with the host icon, so the second addon takes the first one's fallback button as the host. Ids are numbered per hook from 7300, and the button belongs to whichever hook made it.

**Proposal (in the brief):** a shared fallback button. Its metadata lists its users (the hook objects) and the visibility each one wants. Items get the first free id from 7300 up. The button is visible when any user wants it, and the last user to leave removes and frees it. A separator only goes in front of our items in built-in menus. Metadata on a native button survives assembly reloads. The public API (`constructor`, `AddItem`, `AppendToHostItems`, `SetFallbackVisible`, `Remove`, `IsHooked`) stays the same, so MultiMesh Scatter 1.2.0 works with the newer common folder.

### Tile set inspector (brief §10.4)
The probe opened `hills_tileset.tres` in an `EditorInspector` and counted `TileSetPreview` controls a few frames later.
- WFC's inspector plugin plus a second plain instance: **2 previews**.
- Prototype `ElectedTileSetInspector`, two instances in a list kept as metadata on the editor's base control, with `_CanHandle` true only for the first live instance: **1 preview**. After removing the first instance: **1 preview** (the second took over).

**Proposal (in the brief):** move `TileSetPreview`, `TileSetInspectorPlugin` (with the elected guard) and `BoundsGizmoPlugin` to `editor_tools_common/Wfc/Editor/`. Each addon registers its own instance.

Not checkable headless: whether the metadata list and the shared button come through an assembly reload intact. That is a manual check on the implementing card.

### Also noted
- Both addons set `SetInputEventForwardingAlwaysEnabled`. That is fine as long as each one only consumes drags on its own handles, as WFC Generator does now.

## Brief changes
- §3 layout gets `Wfc/Editor/`. §5 describes the shared menu and the single preview. §6.5 says the hook must keep its API. §10.3–4 hold these results and decisions.
- New card `dgn-m0-shared-menu-inspector`.
