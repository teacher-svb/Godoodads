---
id: "crv-m2-outlines-2026-11-16"
status: "todo"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T13:23:07.000Z"
completedAt: null
labels: ["v1.0.0", "m2", "core"]
order: "c3"
---
# CRV M2: Rounded Rectangle, Polygon and Figure Eight

The closed outlines of brief §4.1. All are closed and start at the origin heading along Direction, in the plane of Direction and Bend.

- **Rounded Rectangle:** Width (along Direction), Depth (toward Bend), Corner Radius from 0 up to half the shorter side. It starts in the middle of the first side. Corner Radius 0 gives sharp corners (zero handles).
- **Polygon:** Sides ≥ 3, Radius (circumradius), Corner Radius. It starts in the middle of a side.
- **Figure Eight:** Length (toward Bend), Width (along Direction), flat (board decision). It starts at the tip of one lobe and is fitted within the accuracy.

## Done when
- Core tests: accuracy, closed, the start tangent along Direction, the corner-radius limits, a square with corner radius 0 has 4 corner points, and a Polygon with large Corner Radius stays inside its circumcircle.
