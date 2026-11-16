---
id: "crv-m0-skeleton-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:24:14.000Z"
modified: "2026-11-16T12:48:27.000Z"
completedAt: "2026-11-16T12:48:27.000Z"
labels: ["v1.0.0", "m0", "editor", "infra"]
order: "a2"
---
# CRV M0: Addon skeleton

Create `addons/godoocurves/` with the suite layout (brief §3) and an editor entry that opens an empty dialog.

## Done when
- `plugin.cfg` (name `godoocurves`, version `0.1.0`), `README.md` and `CHANGELOG.md` stubs, and `Core/`, `Resources/`, `Adapters/`, `Editor/` folders.
- `GodoocurvesPlugin` adds "Generate Curve (GDDS)" through `SpatialMenuHook` for a selected `Path3D` (button chosen from the §10.3 result) and opens a `ToolDialog` on an empty `CurveSettings`, with the target locked when it opens.
- `tests/godoocurves/` and the Core test project pick up the new folders; `tools/package_addon.sh godoocurves 0.1.0` builds a zip with the addon and `editor_tools_common`.
- `BezierPathGenerator/` stays out of the build (`Godoodads.csproj`, done in planning) and is left untouched.
- The root README lists godoocurves as in development; CLAUDE.md names sub-project 4.
- Build clean, all tests pass, the editor loads headless without errors.
- Manual: enable the plugin, select a Path3D, open the dialog; disable and re-enable after a rebuild.

## Result (commit 76af182)
- `addons/godoocurves/` 0.1.0: `plugin.cfg`, README and CHANGELOG stubs, `Core/` and `Adapters/` (empty, `.gitkeep`), `Resources/CurveSettings.cs` (empty settings with `StoredOn`/`StoreOn` on Path3D metadata `godoodads_curve_settings`), `Editor/GodoocurvesPlugin.cs`.
- The plugin adds **Generate Curve (GDDS)** through `SpatialMenuHook(this, "Path3D", "Path3D")`. Godot has no built-in Path3D menu button, so the hook makes its own (headless `--verbose`: "godoocurves: using its own Path3D toolbar button."); `_MakeVisible` shows it for a selected Path3D. The dialog is a `ToolDialog` on `CurveSettings` with the target locked when it opens, Generate disabled, and a status line saying shapes come later.
- Enabled in `project.godot`. Root README lists godoocurves as in development; CLAUDE.md names sub-project 4 and the reference folder.
- Engine tests `tests/godoocurves/CurveSettingsTests.cs`: settings survive a scene save and reload; storing null removes the entry. Build clean, 114 engine and 374 Core tests pass, headless editor load clean.
- `tools/package_addon.sh godoocurves 0.1.0` builds a zip with `addons/godoocurves` and `addons/editor_tools_common` (without `Wfc/`, since godoocurves does not use it). No script change was needed.
- `BezierPathGenerator/` untouched and out of the build.

## Manual check
1. Focus Godot so it reloads the assembly, then confirm **godoocurves** is enabled in *Project Settings → Plugins*.
2. Select a Path3D: a **Path3D** button appears in the 3D toolbar with **Generate Curve (GDDS)**. Select a GridMap: that button hides, the GridMap menu is unchanged.
3. Open the dialog: the title is "Generate Curve (GDDS)", the status names the Path3D, Generate is disabled.
4. Rebuild with the dialog closed, focus Godot, open it again: it still works.

## Manual check result (board, 2026-11-16)
All passed: the Path3D button shows for a Path3D and hides otherwise, the GridMap menu is unchanged; the dialog opens titled "Generate Curve (GDDS)" with the Path3D named and Generate disabled; it still works after a rebuild and assembly reload.
