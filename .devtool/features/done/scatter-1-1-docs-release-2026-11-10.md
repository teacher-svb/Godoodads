---
id: "scatter-1-1-docs-release-2026-11-10"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Scatter 1.1"
dueDate: null
created: "2026-11-10T16:00:00.000Z"
modified: "2026-11-10T18:10:00.000Z"
completedAt: "2026-11-10T18:10:00.000Z"
labels: ["docs"]
order: "g5"
---
# Scatter 1.1: 1.1 docs and release prep

README sections for GridMap surfaces and projected masks; CHANGELOG 1.1.0; `plugin.cfg` 1.1.0; suite README; packaging and smoke test.

Brief: §10.

## Done when
- Docs updated; package builds and smoke-tests locally.

## Result
- README: Install from the release zip (with the two common problems and updating), "GridMap surfaces" and "One mask over the whole surface" sections, options rows for Projection and the GridMap group, How it works, the GridMap demo, and two limitations (whole-face covering, Top Down along -Y).
- CHANGELOG 1.1.0, `plugin.cfg` 1.1.0, suite README version and example zip, suite backlog (cell box handles, mesh per item), CLAUDE.md.
- Packaged in a throwaway worktree: multimesh_scatter-1.1.0.zip, 2,558,649 bytes (mostly the docs images, as in 1.0.1), 73 files, `GridFaces.cs` included, no demo; smoke test "multimesh_scatter loaded cleanly".
