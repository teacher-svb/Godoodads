---
id: "scatter-1-3-1-error-reporting-2026-11-16"
status: "review"
priority: "medium"
assignee: "claude"
epic: "godooscatter"
dueDate: null
created: "2026-11-16T09:00:00.000Z"
modified: "2026-11-16T10:00:00.000Z"
completedAt: null
labels: ["v1.3.1", "editor", "bug"]
order: "b5"
---
# Scatter 1.3.1: Populate errors only reach the Output panel

Review finding 3 (MINOR). `MultiMeshWriter.Build` throws in Append mode on a MultiMesh with 2D transforms and instances, and `ScatterPipeline.Run` throws `OverflowException` for huge Amount × Candidate Factor. `OnPopulatePressed` has already cleared the status and has no try/catch, so the dialog shows nothing (CONVENTIONS.md: errors go to the status area and the toaster).

## Done when
- The 2D-plus-append case is checked before the run and reported with `Notify.Report`.
- Exceptions from the run and the write are caught and reported the same way; nothing is written.
- Manual: Append on a MultiMesh with Transform Format 2D and instances shows the error in the dialog.

## Result
`OnPopulatePressed` checks Append on a MultiMesh with instances and a non-3D transform format before the run and reports it. The run and the write sit in a try/catch: `OverflowException`/`OutOfMemoryException` report "Amount … is too large to run …", anything else "Populate failed, nothing was changed: …" (plus the full exception in the Output panel). Nothing is written in either case.

## Manual check
1. Set a MultiMesh's Transform Format to 2D, give it an instance count, choose Append and Populate: the error shows in the dialog and as a toast.
2. Set Amount to 2,000,000,000 with Min Distance > 0 and Populate: the "too large" error shows.
