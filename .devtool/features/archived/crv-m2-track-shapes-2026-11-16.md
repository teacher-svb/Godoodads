---
id: "crv-m2-track-shapes-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T13:59:14.000Z"
completedAt: "2026-11-16T13:59:14.000Z"
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

## Result
- **Helix:** an exact horizontal arc with a vertical drift. Bend must be Left or Right. It leaves at its exact slope. That needed a fix in `Arcs.Add`: the drift's handle now uses the circle's factor per radian (4/3·tan(Δ/4)), not Δ/3. The old factor left the start direction 0.24° flat and cost accuracy, and the Loop with Side Shift improved with the fix too (0.018 → 0.014 %).
- **Corkscrew:** Spiral's path with up = frame up rolled around forward by the angle, so it faces the axis and is upright at both ends. Turns must be whole, and it has at least 8 points per turn.
- **Banked Turn:** an exact arc with at least 16 segments, since Godot interpolates tilt linearly. Its wanted up is cos β·up + sin β·(into the turn); β eases in over the first quarter (smoothstep), holds, and eases out over the last. Bank must be between −90° and 90°.

| Case | Points | Deviation |
|---|---|---|
| Helix r 5, 2 turns, rise 5 / r 3, 3.5 turns, rise −6 Left | 9 / 15 | 0.014 / 0.015 % |
| Corkscrew 10, r 5, 2 turns / 16, r 1.5, 1 turn | 25 / 9 | 0.043 / 0.039 % |
| Banked Turn r 5 90° / r 20 180° −45° Left | 17 | 0.000 % |

Further tests: the Corkscrew faces its axis wherever the radius is at least 0.2·r and is upright at its ends; the Helix ends at Rise with the same slope it starts with; the Banked Turn is upright at its ends and banked 30° at the middle, leaning into the turn across the tangent; impossible settings give messages.
