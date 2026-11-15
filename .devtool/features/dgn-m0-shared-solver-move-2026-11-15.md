---
id: "dgn-m0-shared-solver-move-2026-11-15"
status: "review"
priority: "high"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T12:39:43.000Z"
completedAt: null
labels: ["v1.0.0", "m0", "refactor", "common"]
order: "a4"
---
# DGN M0: Move the WFC solver into editor_tools_common

Move `addons/wfc_generator/Core`, `Resources` and `Adapters` to `addons/editor_tools_common/Wfc/`, with their `.uid` files, using the steps from `dgn-m0-verify-uid-move`. Namespaces become `Godoodads.Common.Wfc`, `.Core` and `.Adapters`. `tests/Core.Tests` compiles `addons/**/Core/**`. Update CONVENTIONS.md (layout) and CLAUDE.md (architecture).

Brief: §3 Layout, §4.1.

## Done when
- `dotnet build`, Core tests and engine tests pass; WFC test changes are namespaces only.
- The headless editor load shows no plugin errors.
- All WFC demo scenes open and generate as before (manual check in the editor).

## Steps from `dgn-m0-verify-uid-move`
1. `git mv` each script **with** its `.uid` file; change the namespace.
2. Add the three legacy stubs in `addons/wfc_generator/Resources/` (namespace `Godoodads.WfcGenerator.Legacy`, new `.uid`, no `[GlobalClass]`), brief §4.1.
3. Open the project in the editor once (or `--headless -e --quit-after`) so the uid cache follows the move.
4. Rerun `demo/wfc_generator/tools/build_demo.tscn`, so the demo files reference the new paths.

## Results
Branch `godoodungeon-m0` (not committed yet).

- `git mv` of `addons/wfc_generator/{Core,Resources,Adapters}` to `addons/editor_tools_common/Wfc/`, with every `.uid` file. Namespaces are now `Godoodads.Common.Wfc`, `Godoodads.Common.Wfc.Core` and `Godoodads.Common.Wfc.Adapters`. WFC's `Editor/` keeps `Godoodads.WfcGenerator.Editor` and gained `using Godoodads.Common.Wfc;` where it used the resources through the old parent namespace.
- Legacy stubs `addons/wfc_generator/Resources/{WfcTile,WfcTileSet,WfcSettings}.cs`: empty `[Tool]` subclasses in `Godoodads.WfcGenerator.Legacy`, without `[GlobalClass]`, with new uids (`bu5gpb3t1gw2f`, `c47gw2abwolly`, `6q5g6kennjxq`; the moved scripts keep `xrobwlr825xt`, `rb5vled2paii`, `bp15dtwmw2ped`).
- `tests/Core.Tests` compiles `addons/**/Core/**`.
- Demo files: the script paths in the 7 WFC demo files and the prototype files were rewritten to the new paths (a 15-line diff), instead of rerunning the build scenes, which would rewrite every sub-resource id.
- Docs: CONVENTIONS.md (layout, namespaces, the legacy-stub rule, test glob), CLAUDE.md (architecture), WFC brief §3 (note on 1.3.3).

Checks:
- `dotnet build`: 0 errors, 0 warnings.
- Core tests: **250 passed**, the same as before the move (checked in a copy of the old tree).
- Engine tests: **83 passed**, the same as before. `Tiles_saved_by_1_0_load_with_weight_one` loads a tile file that names the old path, so it now also covers the legacy stub.
- Headless editor load: no errors or warnings.
- The load check from `dgn-m0-verify-uid-move`: all 7 WFC demo files resolve to the moved scripts, including `WfcSettings` in metadata.
- The prototype builder still runs: 3 of 3 rooms and 6 of 6 doors reached.

## Manual check (board)
1. Focus the editor so it reloads the assembly, then disable and enable WFC Generator.
2. Open `hillside_free`, `hillside_ground` and `hillside_paths`, open **GridMap > Generate WFC (GDDS)** and Generate with the stored seeds. The result should look as before.
3. Open a tile set in the inspector: the preview shows as before.
