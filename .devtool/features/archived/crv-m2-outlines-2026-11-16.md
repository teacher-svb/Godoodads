---
id: "crv-m2-outlines-2026-11-16"
status: "done"
priority: "medium"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T13:23:07.000Z"
modified: "2026-11-16T13:59:14.000Z"
completedAt: "2026-11-16T13:59:14.000Z"
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

## Result
- **Rounded Rectangle and Polygon** share `RoundedPolygon`: straight sides plus exact corner arcs, cut back by r·tan(turn/2). Corner Radius 0 gives sharp corners with zero handles. The wanted up turns with the heading, for outlines in a vertical plane. The Polygon's Corner Radius goes up to its inner radius, at which point it is a circle.
- **Figure Eight:** a lemniscate of Gerono (W·sin t·cos t, L/2·(1 − cos t)), fitted, closed, starting at a lobe tip heading along Direction. In a vertical plane its wanted up is across the tangent.

Tests:
- Rectangles and polygons are checked against the ideal rounded outline: every sample's signed distance to the inner polygon widened by the corner radius stays within 0.1 %, and the length is within 0.1 % of the exact perimeter. Cases: three rectangles (one with sharp corners, one at the largest corner radius in a vertical plane) and three polygons (a sharp triangle, a hexagon, and a 12-gon with Bend Down).
- A square with sharp corners has 5 points with no handles.
- A hexagon rounded to its inner radius is a circle.
- Figure Eight 10×10: 16 points, 0.034 %. Figure Eight 20×4 with Bend Up: 11 points, 0.043 %.
- `Shapes.IsClosed` matches the built curves.

495 Core tests pass.
