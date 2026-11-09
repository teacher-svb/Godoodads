---
id: "dist-readme-csharp-solution-2026-11-09"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Distribution"
dueDate: null
created: "2026-11-09T11:50:00.000Z"
modified: "2026-11-09T11:50:00.000Z"
completedAt: "2026-11-09T11:50:00.000Z"
labels: ["docs"]
order: "a1"
---
# Distribution: C# solution step in the install instructions

A project without a C# solution has nothing to build, so the MultiMesh Scatter install steps stopped working for GDScript-only projects at the build step.

## Result
Added the step to `addons/multimesh_scatter/README.md`. The label "Create C# solution" comes from Godot 4.7's `GodotTools.dll`. The already published 1.0.0 zip still has the old README; the fix ships with the next release.
