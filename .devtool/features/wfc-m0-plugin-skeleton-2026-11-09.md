---
id: "wfc-m0-plugin-skeleton-2026-11-09"
status: "todo"
priority: "high"
assignee: "claude"
epic: "WFC M0 Foundations"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T16:23:51.000Z"
completedAt: null
labels: ["setup"]
order: "a0"
---
# WFC M0: Addon skeleton

Create `addons/wfc_generator/` with `plugin.cfg`, README and CHANGELOG stubs, the `Core/ Resources/ Adapters/ Editor/` folders and an `EditorPlugin` under `#if TOOLS`. Add `tests/wfc_generator/` and `demo/wfc_generator/`.

Brief: §3.

## Done when
- The project builds and the plugin can be enabled.
- The headless editor load shows no plugin errors.
- A placeholder Core test runs under `dotnet test tests/Core.Tests`.
