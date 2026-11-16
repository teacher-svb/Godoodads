---
id: "crv-m0-skeleton-2026-11-16"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:24:14.000Z"
modified: "2026-11-16T12:24:14.000Z"
completedAt: null
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
