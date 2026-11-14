---
id: "scatter-1-2-gridmap-item-wildcards-2026-11-14"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Scatter 1.2"
dueDate: null
created: "2026-11-14T10:00:00.000Z"
modified: "2026-11-14T16:50:15.720Z"
completedAt: "2026-11-14T16:50:15.720Z"
labels: ["adapters", "resources", "editor"]
order: "Zp"
---
# Scatter 1.2: Wildcards in the GridMap item filter

Entries in *GridMap → Items* may be glob patterns (`*` any run of characters, `?` one character), with Godot's `String.Match` semantics, case-sensitive. A MeshLibrary with many variants (`border_full`, `border_edge_N`, `border_outer_NE`, …) then needs one entry, `border_*`, instead of every name.

Brief: §10.1 (GridMap item filter).

## Done when
- An item counts as listed if it matches any entry; Include/Exclude, the box and "empty list = every item" work as before.
- Exact names give the same result as 1.1.0 with the same settings and seed.
- Matching happens once per MeshLibrary item id, not per cell.
- Entries that match no item in the MeshLibrary are reported as a status line (`GridMap item 'bordr_*' matches no item.`). With Include and no match at all, the "No cells of '…' match the GridMap filter and box." error stays.
- The inspector tooltip says wildcards are allowed.
- README (GridMap section, Options table) and CHANGELOG 1.2.0 updated.
- Tests: exact name, `*`, `?`, no match (Include error, Exclude uses all), overlapping patterns.
- `dotnet build` without new warnings.
- Manual: the tooltip and the unmatched-entry status line in the editor dialog.

## Notes
- The demo's MeshLibrary has a single `Paving` item, so a pattern would not be natural there; the demo stays as it is.

## Result
- `Core/NameFilter`: exact name or `String.Match` pattern per entry (`name == entry || name.Match(entry)`, so exact names, also empty ones, match as in 1.1.0), `Accepts` for Include/Exclude, and `Unmatched` for entries that match no name. Engine-free; verified that Godot's `?` does not match a `.`.
- `SurfaceSource.FromGridMap`: the include/skip decision is cached per item id; the entries that match no MeshLibrary item go out in `GridMapSurfaceStats.UnmatchedItems` (also when the "No cells …" error is returned).
- Plugin: one warning per unmatched entry, `GridMap item 'bordr_*' matches no item.`, after the cell report or after the error.
- `GridMapItems` description (inspector tooltip) mentions `*` and `?`.
- README (GridMap section, Options table) and CHANGELOG 1.2.0 (Unreleased) updated. `plugin.cfg` is still 1.1.0; it is bumped with the release.
- Tests: Core 250 pass (`NameFilterTests`, 19 cases); engine 75 pass (`GridMapSurfaceTests`: exact names, `*` equals listing every name, `?`, overlapping patterns, no match with Include error / Exclude uses all, matched against the library not the used cells). `dotnet build` 0 warnings; headless editor load clean.
- Waiting for the manual editor check: the Items tooltip, and the warning line for a typo such as `Paving*x` in the GridMap demo.

Manual check passed (2026-11-14): the Items tooltip mentions the wildcards, and a typo entry shows the "matches no item" warning in the dialog.

Release prep: `plugin.cfg` 1.2.0, CHANGELOG 1.2.0 dated 2026-11-14, suite README version and example zip, CLAUDE.md. Tagged `multimesh_scatter-v1.2.0` after merge.
