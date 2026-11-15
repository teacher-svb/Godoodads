---
id: "dgn-idea-route-spacing-2026-11-15"
status: "backlog"
priority: "low"
assignee: null
epic: "godoodungeon"
dueDate: null
created: "2026-11-15T13:24:58.000Z"
modified: "2026-11-15T13:24:58.000Z"
completedAt: null
labels: ["v2.0.0", "core", "idea"]
order: "c4"
---
# godoodungeon idea: Route spacing for corridors

Guided paths can lay two routes in adjacent cells, so two corridors run side by side with a double wall between them (M0 prototype, x 6–7; see `dgn-m0-prototype`).

## Design notes
- An option in the shared `PathRouter` (`editor_tools_common/Wfc/Core`): cells next to a route laid earlier are blocked or cost more for later routes, except where a route joins the network.
- Additive and off by default, so WFC Generator results do not change.
- The board decided on 2026-11-15 (M0 review) that v1 accepts adjacent corridors.
