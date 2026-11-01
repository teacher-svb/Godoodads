---
id: "m4-settings-ui-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M4 Normal map"
dueDate: null
created: "2026-11-01T01:22:52.000Z"
modified: "2026-11-01T01:35:11.000Z"
completedAt: "2026-11-01T01:35:11.000Z"
labels: ["ui", "editor"]
order: "a2"
---
# M4: Normal map settings and checks

**Normals** group: the normal source gains a **Normal Map** option, which shows texture, UV set, space, flip green and strength (0–1, default 1).

Checks:
- Normal Map selected without a texture: error.
- The UV set is missing on the surface: error.
- Tangent space with UV2: error, because mesh tangents follow UV1.
- Tangents were generated: notice.

## Result (needs a manual check)
Normals group: Normal Source gains **Normal Map**, which shows texture, UV set, space, flip green and strength; these are hidden otherwise. Errors for: no texture, missing UV set, tangent space with UV2, unreadable texture, RG-only map in object space. Info notice when tangents were generated. Face-normal fallback warning also for Normal Map. Checks run up front when the dialog opens. Built in the editor; the log is clean.

## Board check (2026-11-01)
Confirmed: all M4 checks pass. The tangent notice cannot be shown with the demo surfaces (all have real tangents); the engine test covers it.
