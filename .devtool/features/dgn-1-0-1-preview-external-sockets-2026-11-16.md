---
id: "dgn-1-0-1-preview-external-sockets-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-16T09:27:47.000Z"
modified: "2026-11-16T09:37:35.000Z"
completedAt: null
labels: ["v1.0.1", "editor", "bug", "common"]
order: "d6b"
---
# godoodungeon 1.0.1: The tile set preview ignores newly added External Sockets

Review finding 2 (MINOR) from the review of `addons/editor_tools_common` at 7d5ccba. `TileSetPreview` (`Wfc/Editor/TileSetPreview.cs:329-341`) rebuilds only when `Signature()` changes. The signature covers the library and each tile's key, but not `WfcTileSet.ExternalSockets`. Editing an array element doesn't rebuild the inspector either, so `_binding.External` stays stale. The red face labels and the "has no partner" warnings then ignore the External Sockets just added.

- Follow godoodungeon's README advice: add `outside-` to the room tile set's External Sockets. Every `outside+` label stays red and the check still lists those faces, until a tile is edited or the tile set is selected again. It looks as if External Sockets does nothing.

## Done when
- `Signature()` includes the External Sockets entries, so adding, editing or removing one rebuilds the preview within one refresh.
- Manual: the case above in the editor, on `demo/godoodungeon/assets/dungeon_room_tileset.tres`. Labels turn white and the warnings go away without reselecting the tile set. Removing the entry brings them back.
- godoowave users get the fix with godoowave 1.4.2 (card `wave-1-4-2-review-2026-11-16`).

## Result
- `TileSetPreview.Signature()` now ends with the External Sockets entries, so adding, editing or removing one rebuilds the binding within one refresh interval: the face labels and the problem list use the new `_binding.External`.
- Side effect: like any other tile set change, editing External Sockets during a Measure stops it ("The tile set changed. Measure again."), although Measure ignores them. Rare, left as is.
- CHANGELOG Unreleased entries in godoodungeon and godoowave. Build clean. The preview is editor UI and cannot be driven headless.

## Manual check
1. Select `demo/godoodungeon/assets/dungeon_room_tileset.tres`, preview open. Remove `outside-` from External Sockets: the `outside+` labels turn red and the problem list names them, without reselecting the tile set.
2. Add `outside-` back: labels turn white and the warnings go away.
