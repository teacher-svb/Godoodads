---
id: "wfc-1-6-socket-lists-2026-11-24"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-24T10:00:00.000Z"
modified: "2026-11-24T15:00:00.000Z"
completedAt: null
labels: ["v1.6.0", "core", "common"]
order: "eB"
---
# godoowave 1.6: several socket labels on one face

Today each face holds exactly one socket, and `Socket.TryParse` rejects anything but letters, digits and `_`. To let a face meet two kinds of neighbour (say `wall` and `window`), users have to duplicate the `WfcTile` with the same Item and split the weight. Allow a simple comma-separated list of labels on one face, e.g. `wall, window` (board decision 2026-11-24: commas; `wall, window` and `wall,window` both work): two faces match when any label on one matches any label on the other, by the existing rules (`a`/`a`, `a+`/`a-`, `a`/`a`, `a@n`/`a@n`).

The solver lives in `addons/editor_tools_common/Wfc/`, so godoodungeon gets this too. The compatibility bitsets are built once in `CompiledTileSet`, so solve speed does not change.

## Scope
- **Core:** a socket set type (or `Socket` holding several labels) in `Socket.cs`. Split on `,`, trim spaces, reject empty entries (`wall,,window`, trailing comma) and duplicates, and allow mixing kinds in one list (`door+, wall`). Commas are already rejected by today's parser, so no existing label changes meaning. `RotatedY` turns each `@n` label. The 1.7 Mirror (card `wfc-1-4-2d-mirror`) swaps `+`/`-` on each label.
- **Users of single sockets:** `CompiledTileSet` (match table), `WfcSolver` (boundary sockets, fixed cells), `PathRouter` (guided paths pick tiles by one incoming socket: decide whether `PathSocket` matches any label), `TileSetChecker` and External Sockets, `SocketEditing` (viewport socket editing and the socket preview labels).
- **Editor text:** explain the syntax where sockets are typed.
  - Inspector: the six `Socket*` properties on `WfcTile` (and the four edges on the 1.7 `WfcTile2D`) get `PropertyHint.PlaceholderText` with `wall, window`, so the empty field shows the format. Add a short description of the rule (one or more labels separated by commas; faces fit when any label matches) as a tooltip if Godot 4.7 shows C# property descriptions, otherwise in the group/README; check this first.
  - Viewport socket editing (`TileSetPreview.BuildEditPopup`): the `LineEdit` placeholder becomes `labels, separated by commas`, plus a small hint label under it with the same rule. Suggestions append to the list instead of replacing it.
  - The same wording in the tile set check messages that print a face's socket.
- **Brief:** add a short section under godoowave §14 with the syntax and the matching rule. Update the README's socket section and the CHANGELOG.

## Done when
- Core tests: parsing (valid lists, empty entries, bad characters, mixed kinds); matching any pair; rotation and mirror applied per label; a single label behaves exactly as in 1.4 (same seed gives the same cells on the demo tile sets).
- The tile set checker counts a face as matched when any of its labels is matched.
- Guided paths and boundary sockets work with a face that has a list.
- The placeholder and hint text show in the inspector and the viewport edit popup (manual check, so the card goes to review).
- Old tile sets load and generate unchanged.

## Result
Implemented on branch `godoowave-socket-lists`, then rebuilt on the Forgejo main as `godoowave-1.6-socket-lists` on 2026-11-24: main had meanwhile released 1.5.0 (pin box nodes) and 1.5.1, so this ships as **1.6.0** and the 2D cards move to 1.7.0. The code applied without conflicts.

- **Core:** new `SocketSet` in `Socket.cs` (one face: one or more `Socket` labels). `TryParse` splits on `,` and trims, so `wall, window`, `wall,window` and ` wall ,  window ` are the same; shown as `wall, window`. Errors: empty label, empty entry (`a,,b`, `a,`, `,a`), a label listed twice, and the per-label errors of `Socket.TryParse`. `Matches` is any pair; `HasName` is any label with that name; `RotatedY` turns each `@n`. `Socket` itself is unchanged and still used for boundary sockets and External Sockets (an implicit conversion lets a `Socket` stand where a `SocketSet` is expected).
- `TileVariant.Sockets` is now `SocketSet[]`. Updated `CompiledTileSet`, `PathRouter` (path faces = any label with the path name; route states keyed by the whole set), `TileSetChecker.MeetsExternal`, `SocketEditing`, and in godoodungeon `DungeonTileRules` (checks each label of a list), `DungeonConnectivity` and the prototype demo builder. `WfcSolver` needed no change.
- **Editor text:** the six socket properties of `WfcTile` have `PropertyHint.PlaceholderText` "label, or labels separated by commas" (`WfcTile.SocketPlaceholder`). The preview's edit popup uses the same placeholder, a tooltip and a dimmed hint label with `SocketSet.Help` ("One or more socket labels separated by commas, such as 'wall, window'. Faces fit when any label on one matches any label on the other."). Suggestions filter on the entry after the last comma (`SocketEditing.EntryBeingTyped`), and a clicked suggestion replaces that entry (`SocketEditing.PickSuggestion`): `wall, ` + `window` → `wall, window`; without a comma it replaces the text as in 1.4.
- **Docs:** brief §17, its own release 1.6 (2D moved to 1.7 on 2026-11-24; §14 is now 1.7), godoowave README (tile table, Sockets section, preview editing), Unreleased entries in the godoowave and godoodungeon CHANGELOGs.

**Tests:** Core 536/536 pass (35 new in `WfcSocketListTests`: parsing with and without spaces, errors, mixed kinds, matching, rotation, adjacency equal to duplicated tiles, checker, External Sockets, boundary, path faces, a guided path through list faces, suggestions, picking). Engine (gdUnit4) 129/129 pass (2 new: placeholder hint on all six socket properties, a list face in a compiled `WfcTile` set). All existing tests pass unchanged apart from three dungeon tests that compared `.Name`/`.Kind` of a face and now compare the face itself; the demo asset tests (same tile sets, same seeds) pass. Headless editor load shows no errors.

**Decisions / deviations:**
- The tile set check message wording was not changed: it prints the face's labels as written (`socket 'lonely, alone' has no partner`), which reads fine.
- Boundary sockets, External Sockets entries, Room Edge Socket and Path Socket stay single labels; a comma there gives the existing "not a valid socket" error.
- C# doc-comment tooltips in the inspector were **not** checked; instead of relying on them the rule is in the placeholder, the popup hint and the README.
- The Mirror interaction is not implemented here (the Mirror card is not done yet); `SocketSet` needs a per-label `+`/`-` swap when it is.

**Not verified (manual check in the editor):** the placeholder in empty socket fields in the inspector; the popup's placeholder, hint label (wrapping in the 220 px popup, dimmed look) and suggestion picking; that the preview shows list labels legibly on faces; behaviour after an assembly reload.

**Retest on the rebuilt branch (2026-11-24):** Core 554/554, engine 156/156, headless editor load without errors.
