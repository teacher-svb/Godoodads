---
id: "wfc-1-5-docs-release-2026-11-20"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-20T13:00:00.000Z"
modified: "2026-11-21T03:30:00.000Z"
completedAt: "2026-11-21T03:30:00.000Z"
labels: ["v1.5.0", "docs", "release"]
order: "d7"
---
# godoowave 1.5: 1.5 docs and release prep

README: replace the pin box section with pin box nodes and the update note (old pins are dropped: put a pin box over them before generating). CHANGELOG 1.5.0 with that breaking change; `plugin.cfg` 1.5.0; packaging check including `addons/godoowave/Nodes/`.

Brief: §16.

## Done when
- Docs updated; package builds and smoke-tests locally.

## Results (2026-11-21)
- README: *Pin boxes* section (adding, what stays, Enabled/delete, child rules, status line) with **Updating from 1.2–1.4** (old pins are ordinary generated cells; add a pin box before generating); *Boxes in the viewport*, Clear, paths, Options and Performance updated; demo section names `hillside_pins.tscn`.
- CHANGELOG 1.5.0 (Added, Removed, Updating; dated 2026-11-21, change it if tagged later), `plugin.cfg` 1.5.0, root README table, download example and backlog line, CLAUDE.md project paragraph.
- Package: `tools/package_addon.sh godoowave 1.5.0` builds the zip with `addons/godoowave/Nodes/`, `editor_tools_common/Nodes/`, the gizmo plugins and `CellBox`; `tools/smoke_test_addon.sh` loads it cleanly in a fresh project.
