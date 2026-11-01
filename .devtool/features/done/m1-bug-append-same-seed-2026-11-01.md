---
id: "m1-bug-append-same-seed-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T14:00:00.000Z"
modified: "2026-11-01T00:52:25.000Z"
completedAt: "2026-11-01T00:52:25.000Z"
labels: ["bug", "core", "editor"]
order: "a2"
---
# Bug: Append only works after changing the seed

Board review: "Append … checked, but requires a new random seed to work."

**Cause:** Append replays the same random sequence as the previous populate when the seed is unchanged. On the same surface, every new position lands on an existing instance. With spacing on, they all conflict with the fixed points and are removed; with spacing off, they overlap exactly.

**Fix:** a new `ScatterOptions.SeedSalt`. In Append mode the plugin sets it to the current instance count, and the pipeline mixes it into the seed with SplitMix64. Salt 0 (Replace mode) leaves results unchanged. Each append gets a new sequence, and the same scene state plus the same settings still reproduces the result.

## Result (needs a manual check)
Core test `Seed_salt_changes_positions_but_stays_deterministic` passes: salt 0 gives the old results, and the same salt gives the same result. The status line now adds ", appended after N existing". Built in the editor through godot-mcp.
**Check:** Append twice in a row onto the same surface without changing the seed. Both runs should place their full amount.

## Board check (2026-11-01)
Confirmed: append works as intended without changing the seed.
