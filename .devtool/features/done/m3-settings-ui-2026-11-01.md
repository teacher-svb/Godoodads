---
id: "m3-settings-ui-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M3 Mask texture"
dueDate: null
created: "2026-11-01T01:07:27.000Z"
modified: "2026-11-01T01:21:12.000Z"
completedAt: "2026-11-01T01:21:12.000Z"
labels: ["ui", "editor"]
order: "a2"
---
# M3: Placement mask settings and warnings

New **Placement Mask** group (after Surface, as in §5):
- texture (`Texture2D`; the inspector's resource picker supports drag and drop from the FileSystem dock)
- channel, UV set, mode, threshold (hidden in Density mode), invert

All options except the texture are hidden while no texture is set.

Up-front checks:
- A texture is set but the surface has no UV1 (or UV2): error.
- The texture cannot be read: error.

The status line shows "N rejected by mask" and a shortfall hint when the attempt limit is reached.

## Result (needs a manual check)
**Placement Mask** group: texture, channel, UV set, mode, threshold (hidden in Density), invert; the rest is hidden while no texture is set. `BuildFilters` in the plugin: error if the chosen UV set is missing or the texture is unreadable. Also checked up front when the dialog opens with a surface already chosen. Rejections appear as "N rejected by mask" in the status line, and the existing attempt-limit hint covers the shortfall.

## Board check (2026-11-01)
Confirmed: all M3 checks pass.
