---
id: "m1-multimesh-writer-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:17:46.000Z"
completedAt: "2026-11-01T00:17:46.000Z"
labels: ["core", "editor"]
order: "aG"
---
# M1: MultiMesh writer (buffer, preserve flags, append/replace)

- `Duplicate()` the existing MultiMesh, keeping mesh, `use_colors`, `use_custom_data` and the transform format.
- Fill a single `float[]` and set `Buffer` once (row-major 3x4 per instance).
- Append keeps the existing instances.

**Tests**
- GdUnit4: flags preserved; `Buffer` round-trips. (Headless finding from M0: `GetInstanceTransform` returns zeros under the dummy rendering server, so assert on `Buffer` instead.)

## Result
`Adapters/MultiMeshWriter.cs`: duplicates the MultiMesh (keeps mesh, colors, custom data, AABB), composes one buffer, supports append and replace, and converts an empty 2D MultiMesh. Tests in `AdapterTests`.
