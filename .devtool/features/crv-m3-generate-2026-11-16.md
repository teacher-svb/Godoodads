---
id: "crv-m3-generate-2026-11-16"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:30:17.000Z"
modified: "2026-11-16T14:30:17.000Z"
completedAt: null
labels: ["v1.0.0", "m3", "editor", "ui"]
order: "d1"
---
# CRV M3: Generate, Replace and Append, undo

The dialog's Generate (brief §4.3, §5).

- Generate is enabled. It checks the settings (`CurveSettings.Check`), builds the shape, and writes it with `CurveWriter`: Replace gives the path a new curve from Start, Direction and Up; Append adds to a copy of its curve.
- One undo step ("Generate Curve (GDDS)"), with the target's scene as context: the do and undo set the path's `curve` and its stored `CurveSettings` (null removes them), so undo also restores the settings.
- Status after success, for example `Appended Loop: 9 points, 31.4 m. The curve is now 112.0 m.`, plus the writer's note when there is one. Errors (bad settings, closed shapes in Append, a closed curve) go to the status and the toaster (`Notify`), and nothing changes.
- The dialog stays open, so pieces can be appended one after another. The target stays locked; if it leaves the scene, Generate reports that.
- Reopening the dialog on a path loads its stored settings.

## Done when
- Build clean, tests pass, editor loads headless.
- Manual: Replace a few shapes; Append a track of pieces; undo and redo each; errors for a closed shape in Append; reopen the dialog and see the stored settings; a curve shared by two paths stays unchanged on the other path.
