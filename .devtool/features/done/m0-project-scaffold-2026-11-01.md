---
id: "m0-project-scaffold-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M0 Foundations"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-10-31T23:56:34.000Z"
completedAt: "2026-10-31T23:56:34.000Z"
labels: ["infra"]
order: "a0"
---
# M0: Godot 4.7 C# project scaffold

Create the demo/test Godot project at the repo root and the folder layout from brief §3.

**Files**
- `project.godot` (Godot 4.7, .NET, Forward+), `Godoodads.csproj`, `Godoodads.sln`
- `.gitignore`, `.gitattributes`, `.editorconfig`
- Empty folders with placeholders: `addons/editor_tools_common/`, `addons/multimesh_scatter/{Core,Editor}/`, `demo/multimesh_scatter/`, `tests/multimesh_scatter/`

**Acceptance**
- `dotnet build` succeeds with zero warnings.
- `godot --headless --path . --import` exits cleanly with Godot 4.7 .NET.
