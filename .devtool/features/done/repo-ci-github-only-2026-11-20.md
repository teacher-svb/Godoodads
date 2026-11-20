---
id: "repo-ci-github-only-2026-11-20"
status: "done"
priority: "medium"
assignee: "claude"
epic: "Godoodads"
dueDate: null
created: "2026-11-20T22:00:00.000Z"
modified: "2026-11-20T22:00:00.000Z"
completedAt: "2026-11-20T22:00:00.000Z"
labels: ["v1.0.0", "infra", "ci", "release"]
order: "g2"
---
# Release workflow: run on GitHub only

The tag `godoodungeon-v1.1.0` started the *Release addon* workflow twice: on GitHub, where it published the release, and on the Forgejo instance (`git.svb-avh.com`, which mirrors to GitHub), where Forgejo Actions also runs `.github/workflows/`. There it failed at *Package addon* (`zip: command not found`), and its last step (`gh release create`) could not have worked from Forgejo anyway.

**Decision (board, 2026-11-20):** releases are published on GitHub only. The job now has `if: github.server_url == 'https://github.com'`, so Forgejo shows the run as skipped instead of failed.

Check: the next release tag shows a skipped run on Forgejo and a successful one on GitHub.
