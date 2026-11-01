---
id: "m3-status-wording-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M3 Mask texture"
dueDate: null
created: "2026-11-01T01:18:44.000Z"
modified: "2026-11-01T01:21:12.000Z"
completedAt: "2026-11-01T01:21:12.000Z"
labels: ["ui", "core"]
order: "a6"
---
# Status line mixes instances and attempts

Board review (Density with 50% grey): `Placed 2048 / 2048 (2076 rejected by mask)` "makes no sense". The numbers are right (2076 of 4124 tried **positions** were rejected, which is 50%), but the wording reads as 2076 rejected **instances**. The brief's example in §5 has the same ambiguity.

**Fix:** new `Core/ScatterReport.cs`, unit-tested:
- **Line 1:** `Placed P / R. Seed S. T ms.`
- **When short:** one line per cause: "N did not fit at this minimum distance", "N missing: the attempt limit (T tried positions) was reached", or "no part of the surface is within the slope range".
- **Filter statistics** as info: "Mask rejected 2076 of 4124 tried positions (50%)." and "Slope filter kept 17% of the surface."

## Result (needs a manual check)
`Core/ScatterReport.cs` with `ScatterReportTests` (the board's exact case, split shortfall, slope and append text). The plugin renders the lines with their severities. Built in the editor; the log is clean.
**Check:** repeat the Density run. Expected: "Placed 2048 / 2048. Seed …" (green) plus "Mask rejected ~2076 of ~4124 tried positions (50%)."

## Board check (2026-11-01)
Confirmed: all M3 checks pass.
