---
id: "crv-m3-generate-2026-11-16"
status: "review"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:30:17.000Z"
modified: "2026-11-16T14:33:18.000Z"
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

## Result
- `GodoocurvesPlugin.OnGeneratePressed`:
  - checks the settings
  - builds the shape (`PlaceShape`, which the preview uses too)
  - Replace: `CurveWriter.Create`; Append: `CurveWriter.Append` on a copy
  - one undo step ("Generate Curve (GDDS)", with the target as context): `AddDoProperty`/`AddUndoProperty` on `curve`, and `ApplySettings` for the stored settings (null removes them)
- Status examples: "Generated Loop: 7 points, 31.4 m." and "Appended Loop: 6 new points, 31.4 m. The curve is now 112.0 m." The writer's note is added when it has one, and a line when the old curve was a saved file (that file is unchanged).
- Errors go to the status and the toaster through `Notify`: bad settings, a closed shape in Append, a closed curve, a target no longer in the scene.
- `CurveWriter.AppendFrame` now holds the end-frame fallback, shared by Generate and the preview.
- Build clean, 127 engine and 498 Core tests pass, editor loads headless without errors. UI checks are manual.

## Manual check
1. Select a Path3D, open the dialog: Generate is enabled. Replace a Circle, then an S-bend: each replaces the curve.
2. Switch to Append and add Straight, Banked Turn, Loop, Wave one after another: they chain without kinks.
3. Ctrl+Z steps back one piece at a time, including the dialog's stored settings when you reopen it; Ctrl+Shift+Z redoes.
4. Choose Circle in Append mode: Generate reports that closed shapes cannot be appended, and nothing changes.
5. Close and reopen the dialog on the path: it shows the last settings used.
6. Give two Path3Ds the same Curve3D resource, Generate on one: the other keeps its curve.
