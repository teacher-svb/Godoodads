---
id: "crv-m2-track-shapes-2026-11-16"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T13:23:07.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "core"]
order: "c2"
---
# CRV M2: Helix, Corkscrew and Banked Turn

The track pieces of brief §4.1, with their wanted up per point (§4.4).

- **Helix:** Radius, Turns, Rise, Bend Left or Right. It turns around the up axis and climbs Rise at a constant slope (board decision), so it leaves at its slope. Wanted up is the frame's up.
- **Corkscrew:** Spiral's path, eased ends included. Wanted up faces the spiral's axis inside the turns and is the frame's up at the ends.
- **Banked Turn:** Radius, Angle, Bank (degrees), Bend Left or Right. A horizontal exact arc. Wanted up is the frame's up rolled toward the inside of the turn by the bank, which eases in over the first quarter, holds, and eases out over the last quarter (board decision).

## Done when
- Core tests: accuracy within 0.1 % against exact formulas; Corkscrew's and Banked Turn's wanted up as defined; Helix's end height equals Rise; impossible settings give messages.
