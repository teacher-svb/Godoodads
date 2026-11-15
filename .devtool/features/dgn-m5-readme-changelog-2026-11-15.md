---
id: "dgn-m5-readme-changelog-2026-11-15"
status: "in-progress"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T19:32:27.000Z"
completedAt: null
labels: ["v1.0.0", "m5", "docs", "release"]
order: "f3"
---
# DGN M5: README, screenshots, CHANGELOG and 1.0.0

Addon README (install, tile set rules, settings, troubleshooting) with screenshots, CHANGELOG 1.0.0, `plugin.cfg` version, the root README table and backlog, and CLAUDE.md.

Brief: §7.

## Done when
- `tools/package_addon.sh godoodungeon 1.0.0` builds a zip that passes `tools/smoke_test_addon.sh`.

## Results
Branch `godoodungeon-m1`, commit `d4f7881`.

- `addons/godoodungeon/README.md`, written for people installing the addon. Sections:
  - Install, including how it works with WFC Generator, updating, and a troubleshooting table (first-run compile, old WFC Generator).
  - Use (Preview Layout, bounds handles, hand-placed cells, markers) and an options table with every setting and its default.
  - Tile set rules for the room tile set (sockets, one-sided edge, External Sockets `outside-`, door weight 0) and the corridor tile set (straight, bend, T, cross, empty).
  - When generation fails, how it works (8 steps), performance, the demo, and limitations.
- `CHANGELOG.md` `[1.0.0] - 2026-11-15`. `plugin.cfg` 1.0.0. `docs/.gdignore`; the screenshots `docs/dungeon.png` and `docs/dialog.png` are referenced and come from the board.
- Root README: godoodungeon 1.0.0 in the table, in the install example, and a godoodungeon backlog section. CLAUDE.md: the sub-project 3 overview (router, External Sockets).
- Packaging (scratch clone of the branch): `tools/package_addon.sh godoodungeon 1.0.0` builds the zip (addon with README, CHANGELOG and docs, plus `editor_tools_common` with `Wfc/`) and the notes from the CHANGELOG. `tools/smoke_test_addon.sh`: **"godoodungeon loaded cleanly"** in a clean C# project without WFC Generator.
- Open: the two screenshots.
