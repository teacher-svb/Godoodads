---
id: "repo-ci-release-fixes-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "Godoodads"
dueDate: null
created: "2026-11-16T15:08:16.000Z"
modified: "2026-11-16T15:08:16.000Z"
completedAt: null
labels: ["v1.0.0", "infra", "ci", "release"]
order: "g1"
---
# Release workflow failures (2026-11-16)

The *Release addon* runs for `godoowave-v1.4.1`, `godoowave-v1.4.2`, `godoodungeon-v1.0.1` and `godoocurves-v1.0.0` failed.

- **1.4.1, 1.4.2, 1.0.1:** the tests, packaging and smoke test passed, but "Publish GitHub Release" failed with "a release with the same tag name already exists". Claude had created each release by hand right after pushing the tag. The releases themselves are valid.
- **godoocurves 1.0.0:** two godooscatter timing tests (`PerformanceTests`) went over their budgets on the shared runner (6317 ms against 6000, and 5492 ms against 4000), so the run stopped before the smoke test. The published zip was smoke-tested locally with `tools/smoke_test_addon.sh` afterwards: "godoocurves loaded cleanly". The board left the release as it is.

## Changes (board decisions)
- Releases go through the workflow only: push the tag and nothing else. CLAUDE.md now says so.
- The workflow runs Core tests with `--filter "Category!=Performance"`. The timing tests (already tagged `Category=Performance` in `PerformanceTests`, `WfcPerformanceTests` and `DungeonAcceptanceTests`) still run locally. The filtered suite passes locally: 492 tests.

## Check
The next tag push runs green.
