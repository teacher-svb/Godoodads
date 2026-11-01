---
id: "m0-core-unit-tests-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M0 Foundations"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-10-31T23:56:59.000Z"
completedAt: "2026-10-31T23:56:59.000Z"
labels: ["infra", "test"]
order: "a1"
---
# M0: Fast unit tests for Core (dotnet test)

Spike plus setup: can `Core` (which only uses Godot math structs such as `Vector3`, `Basis` and `Quaternion`) be tested with plain `dotnet test`, without the engine running?

**Files**
- `tests/Core.Tests/Core.Tests.csproj` (xUnit or NUnit) that compiles `addons/multimesh_scatter/Core/**`
- One sample test

**Acceptance**
- `dotnet test tests/Core.Tests` runs green in a few seconds.
- If GodotSharp structs cannot be used outside the engine, record that here and fall back to GdUnit4 only.

## Result
Works. GodotSharp value types run without the engine, and `new Quaternion(Vector3.Up, Vector3.Down)` gives a valid rotation (no NaN).
Command: `dotnet test tests/Core.Tests` (about 1 s).
