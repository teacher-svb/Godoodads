#if TOOLS
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godoodads.Common;
using Godoodads.WfcGenerator.Adapters;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Editor;

[Tool]
public partial class WfcGeneratorPlugin : EditorPlugin, ISerializationListener
{
    /// <summary>Menu item, dialog title and undo action name. GDDS = Godoodads, this suite.</summary>
    private const string ToolName = "Generate WFC (GDDS)";
    private const string ActionText = "Generate";
    private const string StopText = "Stop";
    private const double ProgressInterval = 0.1;
    private const int MaxReportedErrors = 12;
    private const int MinCell = short.MinValue;
    private const int MaxCell = short.MaxValue;
    private static readonly Color BoundsColour = new(1f, 0.6f, 0.1f);
    private static readonly Color FailureColour = new(1f, 0.15f, 0.15f);
    private static readonly Color PinColour = new(0.2f, 0.75f, 1f);

    /// <summary>
    /// One running solve. Never survives an assembly reload, which cancels it first. While the solve
    /// is shown, Preview holds the original item and orientation of every cell written so far.
    /// </summary>
    private sealed record ActiveRun(SolveJob Job, TileSetBinding Binding, GridTopology Grid, FixedCells Cells, Vector3I Origin,
        long Seed, int CellsPerFrame, Stopwatch Clock, List<string> Warnings, Dictionary<int, (int Item, int Orientation)> Preview,
        GeneratedRecord Record);

    private SpatialMenuHook _menu;
    private DialogInspectorFilterPlugin _dialogFilter;
    private TileSetInspectorPlugin _tileSetInspector;
    private ToolDialog _dialog;
    private GridMap _selected;
    private GridMap _target;
    private WfcSettings _settings = new();
    private BoundsGizmoPlugin _gizmo;
    private MeshInstance3D _overlay;
    private string _overlayState;
    private Godot.Collections.Array<Vector3I> _failedCells = new();

    private ActiveRun _run;
    private double _sinceProgress;

    public override void _EnterTree()
    {
        _menu = new SpatialMenuHook(this, "GridMap", "GridMap");
        _menu.AddItem(ToolName, new Callable(this, MethodName.OpenDialog));

        _dialogFilter = new DialogInspectorFilterPlugin();
        AddInspectorPlugin(_dialogFilter);
        _tileSetInspector = new TileSetInspectorPlugin();
        AddInspectorPlugin(_tileSetInspector);

        _gizmo = new BoundsGizmoPlugin { Owner = this };
        AddNode3DGizmoPlugin(_gizmo);
        // Forwarded input reaches this plugin before the GridMap editor, which would take handle drags.
        SetInputEventForwardingAlwaysEnabled();

        _dialog = new ToolDialog(ToolName, ActionText, "wfc_generator_dialog_size") { Filter = _dialogFilter };
        _dialog.ActionPressed += OnActionPressed;
        _dialog.Canceled += OnDialogClosed;
        EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);
    }

    public override void _ExitTree()
    {
        StopRun();
        RemoveOverlay();
        _menu?.Remove();
        _menu = null;
        if (_gizmo != null)
        {
            _gizmo.Owner = null;
            RemoveNode3DGizmoPlugin(_gizmo);
            _gizmo = null;
        }
        if (_dialogFilter != null)
        {
            _dialogFilter.Target = null;
            RemoveInspectorPlugin(_dialogFilter);
            _dialogFilter = null;
        }
        if (_tileSetInspector != null)
        {
            RemoveInspectorPlugin(_tileSetInspector);
            _tileSetInspector = null;
        }
        if (_dialog != null)
        {
            _dialog.Inspector.Edit(null);
            _dialog.QueueFree();
            _dialog = null;
        }
        _selected = null;
        _target = null;
    }

    public override bool _Handles(GodotObject @object) => @object is GridMap;

    public override void _Edit(GodotObject @object) => _selected = @object as GridMap;

    public override void _MakeVisible(bool visible) => _menu?.SetFallbackVisible(visible);

    /// <summary>Godot calls this before unloading the assembly. A live thread would block the unload.</summary>
    public void OnBeforeSerialize()
    {
        if (StopRun())
        {
            SetRunning(false);
            _dialog?.SetStatus("Generation was cancelled because the project was rebuilt. The GridMap was not changed.", ToolDialog.Severity.Warning);
        }
    }

    public void OnAfterDeserialize() { }

    public override void _Process(double delta)
    {
        if (_dialog == null)
            return;
        UpdateOverlay();
        if (_run == null)
            return;

        if (!TargetIsAlive)
        {
            StopRun();
            SetRunning(false);
            Notify.Report(_dialog, "Generation was cancelled because the GridMap is no longer in the open scene.", ToolDialog.Severity.Warning);
            return;
        }

        if (_run.CellsPerFrame > 0)
            ShowEvents(_run, _run.CellsPerFrame);

        if (_run.Job.IsFinished && !_run.Job.HasPendingEvents)
        {
            var finished = _run;
            _run = null;
            SetRunning(false);
            RestorePreview(finished);
            Finish(finished);
            return;
        }

        _sinceProgress += delta;
        if (_sinceProgress < ProgressInterval)
            return;
        _sinceProgress = 0;
        string phase = _run.Job.IsFinished ? $"Solved. Showing the result: {_run.Preview.Count:N0} cells shown" : $"Solving: {_run.Job.ResolvedCells:N0} of {_run.Grid.CellCount:N0} cells";
        int restarts = _run.Job.Restarts;
        _dialog.SetStatus(phase + (restarts > 0 ? $", {restarts} restarts" : "") + ".");
    }

    private bool TargetIsAlive => _target != null && IsInstanceValid(_target) && _target.IsInsideTree();

    private void OpenDialog()
    {
        if (_run != null)
        {
            _dialog.PopupForEditing();
            return;
        }
        if (_selected == null)
            return;

        RemoveOverlay();
        _failedCells.Clear();
        _target = _selected;
        if (WfcSettings.StoredOn(_target) is { } stored)
            _settings = (WfcSettings)stored.Duplicate();

        var onPin = new Callable(this, MethodName.OnPinRequested);
        if (!_settings.IsConnected(WfcSettings.SignalName.PinRequested, onPin))
            _settings.Connect(WfcSettings.SignalName.PinRequested, onPin);
        _dialog.Edit(_settings);
        _dialog.SetStatus($"Target: {_target.Name}");
        if (_target.MeshLibrary == null)
            _dialog.AddStatus("The GridMap has no MeshLibrary. Assign the one your tile set uses.", ToolDialog.Severity.Warning);
        _dialog.PopupForEditing();
    }

    private void OnDialogClosed()
    {
        if (StopRun())
            SetRunning(false);
        _failedCells.Clear();
    }

    private void OnActionPressed()
    {
        if (_run != null)
        {
            StopRun();
            SetRunning(false);
            _dialog.SetStatus("Generation stopped. The GridMap was not changed.", ToolDialog.Severity.Warning);
            return;
        }

        _dialog.ClearStatus();
        _failedCells.Clear();
        if (!TargetIsAlive)
        {
            Notify.Report(_dialog, "The target GridMap is no longer in the scene.", ToolDialog.Severity.Error);
            return;
        }
        if (_settings.TileSet == null)
        {
            Notify.Report(_dialog, "Choose a tile set first.", ToolDialog.Severity.Warning);
            return;
        }
        if (!IsSameLibrary(_target.MeshLibrary, _settings.TileSet.Library))
        {
            Notify.Report(_dialog, "The GridMap and the tile set use different MeshLibrary resources "
                + $"(GridMap: {DescribeLibrary(_target.MeshLibrary)}, tile set: {DescribeLibrary(_settings.TileSet.Library)}).", ToolDialog.Severity.Error);
            return;
        }
        if (BoundsProblem() is { } problem)
        {
            Notify.Report(_dialog, problem, ToolDialog.Severity.Error);
            return;
        }

        GridTopology grid;
        try
        {
            grid = GridTopology.Create3D(_settings.BoundsSize);
        }
        catch (System.ArgumentOutOfRangeException)
        {
            Notify.Report(_dialog, "The bounds contain too many cells.", ToolDialog.Severity.Error);
            return;
        }

        var binding = TileSetBinding.Create(_settings.TileSet, grid.ActiveDirections);
        if (!binding.IsValid)
        {
            ReportTileSetErrors(binding.Errors);
            return;
        }

        var emptyLayers = binding.Compiled.LayersWithoutWeight(grid.Size.Y);
        if (emptyLayers.Count > 0)
        {
            Notify.Report(_dialog, "At some heights every tile has weight 0, so nothing can be generated there.", ToolDialog.Severity.Error);
            _dialog.AddStatus("GridMap layers: " + string.Join(", ", emptyLayers.Take(MaxReportedErrors).Select(y => (y + _settings.BoundsOrigin.Y).ToString()))
                + ". Check the tiles' height curves.", ToolDialog.Severity.Error);
            return;
        }

        var boundaryErrors = new List<string>();
        var boundary = _settings.ParseBoundary(boundaryErrors);
        if (boundaryErrors.Count > 0)
        {
            Notify.Report(_dialog, "A boundary socket is not valid.", ToolDialog.Severity.Error);
            foreach (string error in boundaryErrors)
                _dialog.AddStatus(error, ToolDialog.Severity.Error);
            return;
        }

        var origin = _settings.BoundsOrigin;
        // Cells the last generation wrote are free again unless they were edited since (brief §11.2).
        var record = GridMapCells.ReadRecord(_target);
        var cells = _settings.ClearBoundsFirst ? GridMapCells.AllFree(grid) : GridMapCells.ReadFixed(_target, origin, grid, binding, record);
        var options = new SolveOptions { Seed = _settings.Seed, MaxBacktracks = _settings.MaxBacktracks, MaxRestarts = _settings.MaxRestarts, Boundary = boundary };
        int cellsPerFrame = Mathf.Max(_settings.CellsPerFrame, 0);
        var job = new SolveJob(grid, binding.Compiled, cells.Initial, options, recordEvents: cellsPerFrame > 0);
        _run = new ActiveRun(job, binding, grid, cells, origin, _settings.Seed, cellsPerFrame, Stopwatch.StartNew(),
            binding.Warnings(), new Dictionary<int, (int, int)>(), record);
        _sinceProgress = ProgressInterval;
        SetRunning(true);
        job.Start();
    }

    // Two loaded copies of one file count as the same library.
    private static bool IsSameLibrary(MeshLibrary a, MeshLibrary b) =>
        a != null && b != null && (a == b || (!string.IsNullOrEmpty(a.ResourcePath) && a.ResourcePath == b.ResourcePath));

    private static string DescribeLibrary(MeshLibrary library)
    {
        if (library == null)
            return "none";
        return string.IsNullOrEmpty(library.ResourcePath) ? "built-in library" : library.ResourcePath;
    }

    private string BoundsProblem()
    {
        var size = _settings.BoundsSize;
        var origin = _settings.BoundsOrigin;
        if (size.X <= 0 || size.Y <= 0 || size.Z <= 0)
            return "Every bounds size must be at least 1.";
        var last = origin + size - Vector3I.One;
        if (origin.X < MinCell || origin.Y < MinCell || origin.Z < MinCell || last.X > MaxCell || last.Y > MaxCell || last.Z > MaxCell)
            return $"The bounds must stay within GridMap cell coordinates {MinCell} to {MaxCell}.";
        return null;
    }

    private void ReportTileSetErrors(List<string> errors)
    {
        Notify.Report(_dialog, $"The tile set has {errors.Count} problem(s).", ToolDialog.Severity.Error);
        for (int i = 0; i < errors.Count && i < MaxReportedErrors; i++)
            _dialog.AddStatus(errors[i], ToolDialog.Severity.Error);
        if (errors.Count > MaxReportedErrors)
            _dialog.AddStatus($"And {errors.Count - MaxReportedErrors} more.", ToolDialog.Severity.Error);
    }

    /// <summary>Writes up to <paramref name="limit"/> solver changes to the GridMap as a preview.</summary>
    private void ShowEvents(ActiveRun run, int limit)
    {
        for (int i = 0; i < limit && run.Job.TryTakeEvent(out var change); i++)
        {
            if (change.IsRestart)
            {
                RestorePreview(run);
                continue;
            }
            if (run.Cells.Initial[change.Cell] != WfcSolver.Free)
                continue;

            var position = run.Origin + run.Grid.Coordinates(change.Cell);
            if (!run.Preview.ContainsKey(change.Cell))
                run.Preview[change.Cell] = (_target.GetCellItem(position), _target.GetCellItemOrientation(position));

            if (change.Variant < 0)
            {
                var original = run.Preview[change.Cell];
                _target.SetCellItem(position, original.Item, Mathf.Max(original.Orientation, 0));
                continue;
            }
            var variant = run.Binding.Compiled.Variants[change.Variant];
            int item = run.Binding.Items[variant.TileIndex];
            _target.SetCellItem(position, item, item == WfcTile.EmptyItem ? 0 : GridMapOrientation.FromQuarterTurns(variant.QuarterTurns));
        }
    }

    /// <summary>Puts back every cell the preview changed, so the GridMap is as it was before Generate.</summary>
    private void RestorePreview(ActiveRun run)
    {
        if (run.Preview.Count > 0 && _target != null && IsInstanceValid(_target))
            foreach (var (cell, original) in run.Preview)
                _target.SetCellItem(run.Origin + run.Grid.Coordinates(cell), original.Item, Mathf.Max(original.Orientation, 0));
        run.Preview.Clear();
    }

    private void Finish(ActiveRun run)
    {
        run.Clock.Stop();
        double seconds = run.Clock.Elapsed.TotalSeconds;
        _dialog.ClearStatus();

        if (run.Job.Error != null)
        {
            GD.PushError(run.Job.Error.ToString());
            Notify.Report(_dialog, $"The solver failed: {run.Job.Error.Message}", ToolDialog.Severity.Error);
            return;
        }

        var result = run.Job.Result;
        switch (result.Status)
        {
            case SolveStatus.Solved:
                Commit(run, result, seconds);
                break;
            case SolveStatus.Cancelled:
                _dialog.SetStatus("Generation stopped. The GridMap was not changed.", ToolDialog.Severity.Warning);
                break;
            case SolveStatus.Impossible:
                Notify.Report(_dialog, ImpossibleMessage(result), ToolDialog.Severity.Error);
                ReportDetails(run, result);
                break;
            case SolveStatus.OutOfAttempts:
                Notify.Report(_dialog, $"Ran out of attempts after {result.Backtracks:N0} backtracks and {result.Restarts} restarts. "
                    + "A solution may still exist: try another seed or raise the limits. "
                    + $"Seed {run.Seed}. The GridMap was not changed.", ToolDialog.Severity.Error);
                ReportDetails(run, result);
                break;
        }

        for (int i = 0; i < run.Warnings.Count && i < MaxReportedErrors; i++)
            _dialog.AddStatus(run.Warnings[i], ToolDialog.Severity.Warning);
        if (run.Warnings.Count > MaxReportedErrors)
            _dialog.AddStatus($"And {run.Warnings.Count - MaxReportedErrors} more tile set warnings.", ToolDialog.Severity.Warning);
    }

    private static string ImpossibleMessage(SolveResult result) => result.FixedConflicts.Count > 0 || result.Failure?.HasFixedCells == true
        ? "No solution exists with the cells already in the bounds. Clear the bounds or change those cells. The GridMap was not changed."
        : "This tile set has no solution for these bounds and boundary sockets. The GridMap was not changed.";

    private void ReportDetails(ActiveRun run, SolveResult result)
    {
        for (int i = 0; i < result.FixedConflicts.Count && i < MaxReportedErrors; i++)
        {
            var conflict = result.FixedConflicts[i];
            if (!run.Grid.Contains(conflict.CellB))
            {
                _failedCells.Add(run.Origin + conflict.CellA);
                _dialog.AddStatus($"The placed cell at {run.Origin + conflict.CellA} cannot face the {Directions.Name(conflict.Direction)} boundary: "
                    + $"'{conflict.SocketA}' faces '{conflict.SocketB}'.", ToolDialog.Severity.Error);
                continue;
            }
            _failedCells.Add(run.Origin + conflict.CellA);
            _failedCells.Add(run.Origin + conflict.CellB);
            _dialog.AddStatus($"Cells {run.Origin + conflict.CellA} and {run.Origin + conflict.CellB} do not fit: "
                + $"'{conflict.SocketA}' faces '{conflict.SocketB}'.", ToolDialog.Severity.Error);
        }
        if (result.FixedConflicts.Count > MaxReportedErrors)
            _dialog.AddStatus($"And {result.FixedConflicts.Count - MaxReportedErrors} more.", ToolDialog.Severity.Error);

        if (result.Failure == null)
            return;
        var cell = run.Origin + result.Failure.Cell;
        _failedCells.Add(cell);
        bool placed = run.Cells.Initial[run.Grid.Index(result.Failure.Cell)] >= 0;
        _dialog.AddStatus(placed
            ? $"The placed cell at {cell} does not fit its neighbours (marked red in the viewport)."
            : $"No tile fits at cell {cell} (marked red in the viewport).", ToolDialog.Severity.Error);
        foreach (var neighbour in result.Failure.Neighbours)
            _dialog.AddStatus($"{Directions.Name(neighbour.Direction)} neighbour {run.Origin + neighbour.Cell} offers: {string.Join(", ", neighbour.Sockets)}", ToolDialog.Severity.Info);
    }

    private void Commit(ActiveRun run, SolveResult result, double seconds)
    {
        var changes = GridMapCells.Diff(_target, run.Origin, run.Grid, run.Binding, run.Cells.Initial, result.Cells);
        var stored = (WfcSettings)_settings.Duplicate();
        byte[] written = GridMapCells.RecordAfter(run.Record, run.Origin, run.Grid, run.Binding, run.Cells.Initial, result.Cells).Encode();

        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction(ToolName, UndoRedo.MergeMode.Disable, _target);
        undoRedo.AddDoMethod(this, MethodName.ApplyCells, _target, changes.Do);
        undoRedo.AddUndoMethod(this, MethodName.ApplyCells, _target, changes.Undo);
        undoRedo.AddDoMethod(this, MethodName.ApplySettings, _target, stored);
        undoRedo.AddUndoMethod(this, MethodName.ApplySettings, _target, WfcSettings.StoredOn(_target));
        undoRedo.AddDoMethod(this, MethodName.ApplyRecord, _target, written);
        undoRedo.AddUndoMethod(this, MethodName.ApplyRecord, _target, GridMapCells.StoredRecord(_target));
        var markers = AddMarkerChanges(undoRedo, run, result);
        undoRedo.CommitAction();

        _dialog.SetStatus($"Solved {run.Grid.CellCount:N0} cells in {seconds:F1} s "
            + $"({result.Backtracks:N0} backtracks, {result.Restarts} restarts). Seed {run.Seed}.", ToolDialog.Severity.Success);
        if (run.Cells.Replaced > 0)
            _dialog.AddStatus($"Replaced {run.Cells.Replaced:N0} cells from the previous generation.");
        if (markers.Placed > 0 || markers.Removed > 0)
            _dialog.AddStatus($"Placed {markers.Placed:N0} markers in {GridMapMarkers.ContainerName}"
                + (markers.Removed > 0 ? $", replacing {markers.Removed:N0} from the previous generation." : "."));
        if (markers.Skipped > 0)
            _dialog.AddStatus($"Skipped {markers.Skipped:N0} markers whose scene root is not a Node3D.", ToolDialog.Severity.Warning);
        if (run.Cells.Kept > 0)
        {
            var parts = new List<string>();
            if (run.Cells.Edited > 0)
                parts.Add($"{run.Cells.Edited:N0} generated cells you edited since");
            if (run.Cells.Pinned > 0)
                parts.Add($"{run.Cells.Pinned:N0} pinned cells");
            _dialog.AddStatus(parts.Count > 0
                ? $"Kept {run.Cells.Kept:N0} existing cells, including {string.Join(" and ", parts)}."
                : $"Kept {run.Cells.Kept:N0} existing cells.");
        }
        var compiled = run.Binding.Compiled;
        int unused = Enumerable.Range(0, compiled.Variants.Count).Where(v => compiled.VariantWeights[v] == 0)
            .Select(v => compiled.Variants[v].TileIndex).Distinct().Count();
        if (unused > 0)
            _dialog.AddStatus($"{unused} tile(s) have weight 0: they are only used where you place them by hand.");
        if (run.Cells.Ignored > 0)
            _dialog.AddStatus($"Left {run.Cells.Ignored:N0} cells alone because no tile matches their item or rotation.", ToolDialog.Severity.Warning);
    }

    /// <summary>
    /// Adds the marker changes to the open undo action (brief §12.4): the previous generation's
    /// markers on regenerated cells go, new ones are instanced in the WfcMarkers container.
    /// </summary>
    private (int Placed, int Removed, int Skipped) AddMarkerChanges(EditorUndoRedoManager undoRedo, ActiveRun run, SolveResult result)
    {
        var placements = GridMapMarkers.Plan(_target, run.Origin, run.Grid, run.Binding, run.Cells.Initial, result.Cells);
        var replaced = GridMapMarkers.Replaced(_target, run.Origin, run.Grid, run.Cells.Initial);
        if (placements.Count == 0 && replaced.Count == 0)
            return (0, 0, 0);

        var root = EditorInterface.Singleton.GetEditedSceneRoot() ?? _target;
        var container = GridMapMarkers.Container(_target);
        if (container == null)
        {
            container = new Node3D { Name = GridMapMarkers.ContainerName };
            undoRedo.AddDoMethod(_target, Node.MethodName.AddChild, container, true);
            undoRedo.AddDoMethod(container, Node.MethodName.SetOwner, root);
            undoRedo.AddDoReference(container);
            undoRedo.AddUndoMethod(_target, Node.MethodName.RemoveChild, container);
        }
        foreach (var old in replaced)
        {
            undoRedo.AddDoMethod(container, Node.MethodName.RemoveChild, old);
            undoRedo.AddUndoMethod(container, Node.MethodName.AddChild, old, true);
            undoRedo.AddUndoMethod(old, Node.MethodName.SetOwner, root);
            undoRedo.AddUndoReference(old);
        }
        int placed = 0, skipped = 0;
        foreach (var placement in placements)
        {
            var marker = GridMapMarkers.Instantiate(placement);
            if (marker == null)
            {
                skipped++;
                continue;
            }
            undoRedo.AddDoMethod(container, Node.MethodName.AddChild, marker, true);
            undoRedo.AddDoMethod(marker, Node.MethodName.SetOwner, root);
            undoRedo.AddDoReference(marker);
            undoRedo.AddUndoMethod(container, Node.MethodName.RemoveChild, marker);
            placed++;
        }
        return (placed, replaced.Count, skipped);
    }

    /// <summary>Do and undo target of a generation.</summary>
    public void ApplyCells(GridMap map, int[] changes)
    {
        if (IsInstanceValid(map))
            GridMapCells.Apply(map, changes);
    }

    /// <summary>Pins or unpins the generated cells in the pin box, as one undo step (brief §12.3).</summary>
    public void OnPinRequested(bool pin)
    {
        if (!TargetIsAlive || _run != null)
            return;
        if (!_settings.HasPinBox)
        {
            _dialog.SetStatus("Place the pin box first (Pin Box > Place Around Bounds, then drag its handles).", ToolDialog.Severity.Warning);
            return;
        }
        var (record, changed) = GridMapCells.Pin(_target, _settings.PinOrigin, _settings.PinSize, pin);
        if (changed == 0)
        {
            _dialog.SetStatus(pin ? "The pin box has no unpinned generated cells." : "The pin box has no pinned cells.");
            return;
        }

        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction(pin ? "Pin WFC Cells" : "Unpin WFC Cells", UndoRedo.MergeMode.Disable, _target);
        undoRedo.AddDoMethod(this, MethodName.ApplyRecord, _target, record.Encode());
        undoRedo.AddUndoMethod(this, MethodName.ApplyRecord, _target, GridMapCells.StoredRecord(_target));
        undoRedo.CommitAction();
        _dialog.SetStatus(pin
            ? $"Pinned {changed:N0} generated cells. Generate keeps them until you unpin them."
            : $"Unpinned {changed:N0} cells. The next Generate replaces them.", ToolDialog.Severity.Success);
    }

    /// <summary>Do and undo target for the settings stored on the GridMap. Null removes them.</summary>
    public void ApplySettings(GridMap map, WfcSettings settings)
    {
        if (IsInstanceValid(map))
            WfcSettings.StoreOn(map, settings);
    }

    /// <summary>Do and undo target for the record of what a generation wrote.</summary>
    public void ApplyRecord(GridMap map, byte[] record)
    {
        if (IsInstanceValid(map))
            GridMapCells.WriteRecord(map, record);
    }

    /// <summary>Cancels a running solve, waits for its thread and removes its preview. Returns true if one was running.</summary>
    private bool StopRun()
    {
        if (_run == null)
            return false;
        _run.Job.Cancel();
        if (!_run.Job.Wait(5000))
            GD.PushError("WFC Generator: the solver thread did not stop in time.");
        RestorePreview(_run);
        _run = null;
        return true;
    }

    private void SetRunning(bool running)
    {
        if (_dialog != null)
            _dialog.OkButtonText = running ? StopText : ActionText;
    }

    private void UpdateOverlay()
    {
        if (!_dialog.Visible || !TargetIsAlive)
        {
            RemoveOverlay();
            return;
        }

        var origin = _settings.BoundsOrigin;
        var size = _settings.BoundsSize;
        var half = _target.CellSize * 0.5f;
        var min = _target.MapToLocal(origin) - half;
        var max = _target.MapToLocal(origin + size - Vector3I.One) + half;
        string state = $"{min}|{max}|{_settings.PinOrigin}|{_settings.PinSize}|{string.Join(";", _failedCells)}";
        if (state != _overlayState)
            _target.UpdateGizmos();

        if (_overlay == null || !IsInstanceValid(_overlay))
        {
            _overlay = new MeshInstance3D
            {
                Mesh = new ImmediateMesh(),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                MaterialOverride = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    NoDepthTest = true,
                    VertexColorUseAsAlbedo = true,
                },
            };
            // No owner, so the overlay is never saved with the scene.
            _target.AddChild(_overlay);
            _overlayState = null;
        }

        if (state == _overlayState)
            return;
        _overlayState = state;

        var mesh = (ImmediateMesh)_overlay.Mesh;
        mesh.ClearSurfaces();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        AddBox(mesh, min, max, BoundsColour);
        if (_settings.HasPinBox)
        {
            var pin = BoundsGizmoPlugin.Extents(_target, _settings.PinOrigin, _settings.PinSize);
            AddBox(mesh, pin.Min, pin.Max, PinColour);
        }
        foreach (var cell in _failedCells)
        {
            // Slightly larger than the cell, so the box stays visible around a placed module.
            var centre = _target.MapToLocal(cell);
            AddBox(mesh, centre - half * 1.08f, centre + half * 1.08f, FailureColour);
        }
        mesh.SurfaceEnd();
    }

    private void RemoveOverlay()
    {
        if (_overlay != null && IsInstanceValid(_overlay))
        {
            _overlay.QueueFree();
            if (TargetIsAlive)
                _target.UpdateGizmos();   // hides the handles
        }
        _overlay = null;
        _overlayState = null;
    }

    // Boxes with viewport handles (BoundsGizmoPlugin): 0 is the bounds, 1 the pin box.
    private const float HandleRadius = 12f;
    private int _dragHandle = -1;
    private Vector3I _dragOrigin;
    private Vector3I _dragSize;

    public override int _Forward3DGuiInput(Camera3D viewportCamera, InputEvent @event)
    {
        const int pass = (int)AfterGuiInput.Pass, stop = (int)AfterGuiInput.Stop;
        var map = HandleTarget;
        if (map == null || !EditorInterface.Singleton.GetSelection().GetSelectedNodes().Contains(map))
        {
            _dragHandle = -1;
            return pass;
        }

        if (_dragHandle >= 0)
        {
            int box = _dragHandle / Directions.Count;
            switch (@event)
            {
                case InputEventMouseMotion motion:
                    var (origin, size) = BoundsGizmoPlugin.DragFace(map, viewportCamera, motion.Position,
                        _dragHandle % Directions.Count, BoxOrigin(box), BoxSize(box));
                    SetBox(box, origin, size);
                    return stop;
                case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                    CommitBox(box, _dragOrigin, _dragSize, false);
                    _dragHandle = -1;
                    return stop;
                case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                case InputEventKey { Keycode: Key.Escape, Pressed: true }:
                    CommitBox(box, _dragOrigin, _dragSize, true);
                    _dragHandle = -1;
                    return stop;
            }
            return pass;
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press)
        {
            int handle = HandleAt(map, viewportCamera, press.Position);
            if (handle < 0)
                return pass;
            _dragHandle = handle;
            _dragOrigin = BoxOrigin(handle / Directions.Count);
            _dragSize = BoxSize(handle / Directions.Count);
            return stop;
        }
        return pass;
    }

    /// <summary>The handle id under the mouse, or -1. The nearest one wins when handles overlap.</summary>
    private int HandleAt(GridMap map, Camera3D camera, Vector2 mouse)
    {
        int best = -1;
        float bestDistance = HandleRadius;
        for (int box = 0; box < BoxCount; box++)
        {
            if (!BoxVisible(box))
                continue;
            var positions = BoundsGizmoPlugin.HandlePositions(map, BoxOrigin(box), BoxSize(box));
            for (int direction = 0; direction < Directions.Count; direction++)
            {
                var world = map.GlobalTransform * positions[direction];
                if (camera.IsPositionBehind(world))
                    continue;
                float distance = camera.UnprojectPosition(world).DistanceTo(mouse);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = box * Directions.Count + direction;
                }
            }
        }
        return best;
    }

    /// <summary>The GridMap whose boxes have handles: the dialog's target while the dialog is open.</summary>
    public GridMap HandleTarget => _dialog != null && _dialog.Visible && TargetIsAlive ? _target : null;

    public int BoxCount => 2;

    public bool BoxVisible(int box) => box == 0 || _settings.HasPinBox;

    public Vector3I BoxOrigin(int box) => box == 0 ? _settings.BoundsOrigin : _settings.PinOrigin;

    public Vector3I BoxSize(int box) => box == 0 ? _settings.BoundsSize : _settings.PinSize;

    /// <summary>Changes a box while its handle is dragged. Undo is added in <see cref="CommitBox"/>.</summary>
    public void SetBox(int box, Vector3I origin, Vector3I size)
    {
        if (box == 0)
        {
            _settings.BoundsOrigin = origin;
            _settings.BoundsSize = size;
        }
        else
        {
            _settings.PinOrigin = origin;
            _settings.PinSize = size;
        }
        _settings.NotifyPropertyListChanged();
        if (TargetIsAlive)
            _target.UpdateGizmos();
    }

    /// <summary>Ends a handle drag: puts the box back when cancelled, otherwise adds one undo step.</summary>
    public void CommitBox(int box, Vector3I origin, Vector3I size, bool cancel)
    {
        if (cancel)
        {
            SetBox(box, origin, size);
            return;
        }
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction(box == 0 ? "Resize WFC Bounds" : "Resize WFC Pin Box");
        undoRedo.AddDoMethod(this, MethodName.SetBox, box, BoxOrigin(box), BoxSize(box));
        undoRedo.AddUndoMethod(this, MethodName.SetBox, box, origin, size);
        undoRedo.CommitAction(false);
    }

    private static void AddBox(ImmediateMesh mesh, Vector3 min, Vector3 max, Color colour)
    {
        for (int corner = 0; corner < 8; corner++)
        {
            var from = Corner(min, max, corner);
            for (int axis = 1; axis <= 4; axis <<= 1)
            {
                if ((corner & axis) != 0)
                    continue;
                mesh.SurfaceSetColor(colour);
                mesh.SurfaceAddVertex(from);
                mesh.SurfaceSetColor(colour);
                mesh.SurfaceAddVertex(Corner(min, max, corner | axis));
            }
        }
    }

    private static Vector3 Corner(Vector3 min, Vector3 max, int bits) => new(
        (bits & 1) != 0 ? max.X : min.X,
        (bits & 2) != 0 ? max.Y : min.Y,
        (bits & 4) != 0 ? max.Z : min.Z);
}
#endif
