---
id: "wfc-1-3-4-external-sockets-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "WFC Generator"
dueDate: null
created: "2026-11-15T19:39:57.000Z"
modified: "2026-11-15T19:39:57.000Z"
completedAt: "2026-11-15T19:39:57.000Z"
labels: ["v1.3.4", "core", "resources", "ui", "release"]
order: "e2"
---
# WFC 1.3.4: External Sockets on tile sets

Ships the External Sockets of godoodungeon's `dgn-m4-demo-tilesets` in WFC Generator's own zip, so WFC Generator users get them too. The board asked for it on 2026-11-15 ("fix both").

Brief: WFC §13.8.

## Results
- The code was built and tested on `dgn-m4-demo-tilesets`: `WfcTileSet.ExternalSockets` is parsed by `TileSetBinding` and counted as partners by `TileSetChecker.Check` (unmatched faces and the edge-only analysis) and by `SocketEditing.UnmatchedFaces` (the preview's red faces). An unparsable label is a binding error. Tests: `WfcTileSetCheckerTests.ExternalSocketsCountAsPartners` (Core) and `DungeonSettingsTests.External_sockets_…` (engine).
- `plugin.cfg` 1.3.4. CHANGELOG `[1.3.4] - 2026-11-15` (the former Unreleased entry). README (tile set check section). Brief §13.8 (with 1.3.3). Suite README and CLAUDE.md updated.
- It goes to main with godoodungeon 1.0.0 in PR #15. Tag `wfc_generator-v1.3.4` after merging.
