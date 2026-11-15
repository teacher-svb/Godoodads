---
id: "dist-root-readme-install-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Godoodads"
dueDate: null
created: "2026-11-09T12:00:00.000Z"
modified: "2026-11-15T12:32:30.000Z"
completedAt: "2026-11-09T12:00:00.000Z"
labels: ["docs", "distribution"]
order: "a3"
---
# Distribution: install instructions in the root README

The root README only had setup steps for working on this repository, so visitors had no instructions for installing a released addon.

## Result
Added "Install an addon" with the Releases zip and the C# solution step, and renamed "Getting started" to "Working on this repository". Also removed the untracked `godot_mcp` addon from the enabled plugins in `project.godot`, so a fresh clone does not try to load it.
