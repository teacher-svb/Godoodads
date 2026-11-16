---
id: "crv-idea-curvature-easing-2026-11-16"
status: "backlog"
priority: "low"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:50:36.000Z"
modified: "2026-11-16T14:50:36.000Z"
completedAt: null
labels: ["v2.0.0", "core", "idea"]
order: "z1"
---
# CRV idea: Ease curvature where arcs meet straights

Where an arc meets a straight (Arc, Banked Turn, S-bend, Helix, Loop, the coaster's lift and drop), the direction is continuous but the curvature jumps from 0 to 1/r. On a ride that is an instant change of g-force. Real tracks use clothoid (Euler spiral) transitions, and clothoid loops are teardrop-shaped.

Raised at the godoocurves M4 review (2026-11-16). The board chose to fix only the Loop's sideways kink for 1.0.0 and keep curvature easing as an idea: for example an Easing setting (share of the turn used to ramp curvature) on arc-based shapes.
