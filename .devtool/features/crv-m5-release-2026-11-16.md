---
id: "crv-m5-release-2026-11-16"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:51:48.000Z"
modified: "2026-11-16T14:51:48.000Z"
completedAt: null
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
