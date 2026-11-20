---
id: "scatter-1-3-0-rename-review-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godooscatter"
dueDate: null
created: "2026-11-15T20:11:45.000Z"
modified: "2026-11-15T20:25:41.000Z"
completedAt: "2026-11-15T20:25:41.000Z"
labels: ["v1.3.0", "review", "release"]
order: "b2"
---
# godooscatter 1.3.0: Release review

Rename from MultiMesh Scatter (`addons/multimesh_scatter/`) to godooscatter (brief §11). Work in card `gdd-rename-godoowave-godooscatter-2026-11-15`. The board moves this card to *Done* when approved, after which the release is tagged `godooscatter-v1.3.0`.

## Review checklist for the board
1. Focus Godot so it reloads the C# assembly. *Project Settings → Plugins* lists **godooscatter**, enabled. The MultiMesh menu shows one **Populate Surface (GDDS)** and Godot's own item as **Populate Surface (Godot)**.
2. Open `demo/godooscatter/demo.tscn`, select `Scatter` and populate. The stored settings are restored in the dialog.
3. Load a shipped preset from `addons/godooscatter/Presets/` in the dialog.
4. Read the CHANGELOG 1.3.0 entry and the README *Updating from MultiMesh Scatter* paragraph.

## Result
Approved by the board on 2026-11-15.
