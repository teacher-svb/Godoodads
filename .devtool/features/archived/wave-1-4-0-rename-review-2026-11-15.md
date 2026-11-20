---
id: "wave-1-4-0-rename-review-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoowave"
dueDate: null
created: "2026-11-15T20:11:45.000Z"
modified: "2026-11-15T20:25:41.000Z"
completedAt: "2026-11-15T20:25:41.000Z"
labels: ["v1.4.0", "review", "release"]
order: "fG"
---
# godoowave 1.4.0: Release review

Rename from WFC Generator (`addons/wfc_generator/`) to godoowave (brief §15); External Sockets already shipped in 1.3.4. Work in card `gdd-rename-godoowave-godooscatter-2026-11-15`. The board moves this card to *Done* when approved, after which the release is tagged `godoowave-v1.4.0`.

## Review checklist for the board
1. Focus Godot so it reloads the C# assembly. *Project Settings → Plugins* lists **godoowave**, enabled. The GridMap menu shows one **Generate WFC (GDDS)**.
2. Open `demo/godoowave/hillside_ground.tscn`, generate, drag the handles, regenerate. Same result as 1.3.3 for the same seed.
3. With godoodungeon enabled too, both items share the GridMap menu and a tile set shows one preview.
4. Read the CHANGELOG 1.4.0 entry and the README *Updating from WFC Generator* paragraph.

## Result
Approved by the board on 2026-11-15.
