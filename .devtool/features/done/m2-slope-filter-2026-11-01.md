---
id: "m2-slope-filter-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M2 Orientation"
dueDate: null
created: "2026-11-01T00:55:04.000Z"
modified: "2026-11-01T00:57:39.000Z"
completedAt: "2026-11-01T00:57:39.000Z"
labels: ["core"]
order: "a2"
---
# M2: Slope filter

Brief §4.5. Place only where the angle between the **geometric** (face) normal and a reference direction is between min and max (defaults 0 and 180, meaning no filtering). The reference defaults to the target vector V, with an optional custom direction.

- Implemented as per-triangle sampler weights (the face normal is constant per triangle), so no attempts are wasted. The result reports the fraction of surface area kept, for the status line.

**Tests**
- Cube with 0–45° from +Y: only the top face receives instances.
- A tilted plane is excluded or included by angle.
- 0–180 changes nothing.

## Result
`OrientationMath.SlopeWeights` gives per-triangle sampler weights, combined with caller weights; `ScatterResult.SlopeAreaFraction`. Reference = `ScatterOptions.SlopeReference` or the target direction. Tests: cube top only for 0–45° (area fraction 1/6), custom reference -Y, tilted plane in and out of range, 0–180 is a no-op.
