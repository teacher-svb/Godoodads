---
id: "dist-release-workflow-2026-11-09"
status: "review"
priority: "medium"
assignee: "claude"
epic: "Distribution"
dueDate: null
created: "2026-11-09T11:35:00.000Z"
modified: "2026-11-09T11:41:37.000Z"
completedAt: null
labels: ["release", "ci"]
order: "a0"
---
# Distribution: per-addon release workflow

Build one installable zip per addon from a tag, prove it works in a clean C# project, and attach it to a GitHub Release. The zip is also the payload for a later Godot Asset Store listing.

- Tag format: `<addon>-v<version>`, for example `multimesh_scatter-v1.0.0`.
- The zip holds `addons/<addon>/` and `addons/editor_tools_common/`, each with a copy of `LICENSE`. Nothing else from the repo.
- The tag version must match `plugin.cfg` and the latest CHANGELOG entry (CONVENTIONS.md, Documentation).
- Gate: Core tests, then `dotnet build` and a headless editor load of the zip in a fresh project with default C# settings.
- Release notes come from the matching CHANGELOG section.

**Out of this card (follow-ups)**
- README install step: a project without a C# solution has nothing to build. Add "Project → Tools → C# → Create C# Solution".
- `editor_tools_common` version file and an additive-only API rule, because every addon zip installs it at the same path.
- The Asset Store listing itself, including the mandatory AI usage disclosure. The store takes a direct file upload, so the release zip is the payload and no per-addon dist branch is needed. Its docs also ask for a copyright statement in the license file; the root `LICENSE` is the plain CC0 text, so check that before submitting.

## Result
Added `.github/workflows/release-addon.yml`, `tools/package_addon.sh` and `tools/smoke_test_addon.sh`. `dist/` is git-ignored.

Verified locally with Godot 4.7 .NET:
- Package for `multimesh_scatter` 1.0.0: 81 files, all under `addons/`, LICENSE in both addon folders, release notes taken from the CHANGELOG.
- A version that does not match `plugin.cfg` is refused.
- Clean project with a default csproj builds and loads in the headless editor with no `ERROR:` lines.
- Smoke test fails as it should for a missing plugin script, an exception in `_EnterTree`, and a missing `editor_tools_common`. Godot exits 0 for the first two, so the script checks the log for `ERROR:`.
- Core tests: 64 passed.

**Still to verify:** the workflow has not run on GitHub. Check the first tagged run (`multimesh_scatter-v1.0.0`), then move this card to done. The Godot download URL returned 200.
