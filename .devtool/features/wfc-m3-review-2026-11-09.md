---
id: "wfc-m3-review-2026-11-09"
status: "review"
priority: "medium"
assignee: "claude"
epic: "WFC M3 Demo tileset"
dueDate: null
created: "2026-11-09T16:23:51.000Z"
modified: "2026-11-09T18:51:14.000Z"
completedAt: null
labels: ["review"]
order: "aM"
---
# WFC M3: Milestone review

Demo for the board. The board moves this card to *Done* when approved. Do not start the next milestone before that.

## Review checklist for the board
1. Generate both demo scenes with several seeds.
2. Judge whether results look too noisy or lean too much towards tiles with many rotations.

## Decision for the board
Should weights or boundary sockets move into v1? If so, add cards before starting M4.


## Manual results and board decision (2026-11-09)
Both demo scenes generate and look right. Stop leaves the GridMap unchanged. Rebuild during a solve was skipped by the board, relying on the M0 reload check.

**Decision:** no weights and no boundary sockets in v1. They stay in the backlog. The addon README must explain repeating empty tiles and adding variants to control density.
