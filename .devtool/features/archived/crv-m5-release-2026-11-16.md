---
id: "crv-m5-release-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:51:48.000Z"
modified: "2026-11-16T14:57:48.000Z"
completedAt: "2026-11-16T14:57:48.000Z"
labels: ["v1.0.0", "m5", "docs", "release"]
order: "f2"
---
# CRV M5: Docs and release 1.0.0

- README with screenshots of the demo scenes.
- CHANGELOG 1.0.0 entry (what the first release contains); `plugin.cfg` version 1.0.0; root README status 1.0.0 and the download example; CLAUDE.md.
- Delete the board's `BezierPathGenerator/` reference folder (brief §7, board decision) and its build exclusion.
- `tools/package_addon.sh godoocurves 1.0.0` builds the zip.

## Done when
- The above is in place; build clean, all tests pass, editor loads headless.

## Result (commit 2586f7c)
- **README:** four screenshots in `addons/godoocurves/docs/` (with `.gdignore`, as godoodungeon does): the coaster, an onboard view leaving a banked turn with the loop ahead, the shape gallery, and the outlines. They were rendered at 1600 × 900 in a SubViewport from the demo scenes and saved at 1280 × 720. The README also gains a Demo section and the eased Side Shift.
- **Versions:** CHANGELOG 1.0.0, `plugin.cfg` 1.0.0, the root README (status 1.0.0 and the download example), CLAUDE.md, and the brief's status.
- **`BezierPathGenerator/`:** deleted (untracked, board decision on 2026-11-16), and its `Compile Remove` taken out of `Godoodads.csproj`.
- The demo gallery's labels are larger, so they can be read from the overview.
- **Checks:** the build is clean with no warnings, 501 Core and 127 engine tests pass, and the editor loads headless without errors. `tools/package_addon.sh godoocurves 1.0.0` builds `godoocurves-1.0.0.zip` (the addon with its docs, plus `editor_tools_common` without `Wfc/`), with the CHANGELOG entry as release notes.
