---
id: "dgn-m0-review-2026-11-15"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T11:00:00.000Z"
modified: "2026-11-15T13:32:21.000Z"
completedAt: "2026-11-15T13:32:21.000Z"
labels: ["v1.0.0", "m0", "review"]
order: "a7"
---
# DGN M0: Milestone review

Demo for the board. The board moves this card to *Done* when approved. Do not start the next milestone before that.

## Review checklist for the board
1. Read the findings on the three verification cards and confirm the brief changes they caused.
2. Open the prototype scene and judge the rooms and corridors.
3. Open the WFC demo scenes after the move and generate.
4. Approve tagging `wfc_generator-v1.3.3`.
5. Enable godoodungeon next to WFC Generator: the GridMap menu has both items, and a tile set shows one preview. Disable and re-enable each addon in turn.

## Board review (2026-11-15): approved
1. Brief changes from the verification cards confirmed:
   - Legacy stubs and the WFC Generator version warning (§4.1, §10.1–2).
   - The shared GridMap button and a single tile set preview, with the preview, inspector plugin and gizmo in `editor_tools_common/Wfc/Editor` (§5, §10.3–4).
   - The one-sided room edge socket, a door tile with weight 0, and Wall Clearance (§4.2–4.4, §10.5).
2. Prototype: closed rooms, and regenerating changes only the corridors. Corridors along walls → Wall Clearance. Corridors side by side → `dgn-idea-route-spacing`.
3. hillside_free, hillside_ground and hillside_paths generate as before the move.
4. `wfc_generator-v1.3.3` tagged and released (PR #14, workflow run 43719258826).
5. godoodungeon next to WFC Generator: one menu with both items, one preview, and no errors after a reload or after toggling either addon.
