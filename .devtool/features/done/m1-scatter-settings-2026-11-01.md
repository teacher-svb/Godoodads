---
id: "m1-scatter-settings-2026-11-01"
status: "done"
priority: "medium"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T01:00:00.000Z"
modified: "2026-11-01T00:43:13.000Z"
completedAt: "2026-11-01T00:43:13.000Z"
labels: ["core", "ui"]
order: "aF"
---
# M1: ScatterSettings resource (parity options)

In `Resources/ScatterSettings.cs` (not Core, because it is an engine Resource), mapped to a plain Core options type. Exported properties grouped for the inspector (brief §5 sections):
- amount, seed (with a randomise button), sampler type
- base scale, random scale, random yaw, random tilt
- min distance, candidate factor
- output mode (Replace or Append)

Use `_ValidateProperty` to hide options that do not apply.

## Result (needs a manual check)
`Resources/ScatterSettings.cs`: Amount, Seed plus a **Randomise Seed** button (`[ExportToolButton]`), and the groups Surface (sampler), Orientation (target direction), Randomisation, Spacing (min distance, candidate factor, avoid siblings) and Output (Replace/Append). Candidate factor and avoid siblings are hidden while Min Distance is 0. `SurfacePath` is storage-only. `ToOptions()` maps to the Core `ScatterOptions`.
**Check:** the Randomise Seed button updates the field, and the spacing options appear once Min Distance > 0.

## Board check (2026-11-01)
Confirmed: Randomise Seed works, and the spacing options appear once Min Distance > 0.
