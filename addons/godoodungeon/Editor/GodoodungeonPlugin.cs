#if TOOLS
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godoodads.Common;
using Godoodads.Common.Wfc.Adapters;
using Godoodads.Common.Wfc.Core;
using Godoodads.Common.Wfc.Editor;
using Godoodads.Dungeon.Adapters;
using Godoodads.Dungeon.Core;

namespace Godoodads.Dungeon.Editor;

/// <summary>
/// "Generate Dungeon (GDDS)" in the GridMap menu of the 3D viewport toolbar, shared with WFC
/// Generator (brief godoodungeon §5). The dialog edits a copy of the GridMap's stored
/// <see cref="DungeonSettings"/>. Generate runs a <see cref="DungeonJob"/> on a background thread and
/// writes its result as one undo step: cells, settings, the generated record and markers. Stop, an
/// assembly reload, or the GridMap leaving the scene cancels the run, and nothing is written.
/// </summary>
[Tool]
public partial class GodoodungeonPlugin : EditorPlugin, ISerializationListener
{
    /// <summary>Menu item, dialog title and undo action name. GDDS = Godoodads, this suite.</summary>
    private const string ToolName = "Generate Dungeon (GDDS)";
    private const string ActionText = "Generate";
    private const string StopText = "Stop";
    private const double ProgressInterval = 0.1;
    private const int MaxReportedErrors = 12;
    private const string WfcGeneratorConfig = "res://addons/wfc_generator/plugin.cfg";

    /// <summary>
    /// One running generation. Never survives an assembly reload, which cancels it first.
    /// PreviousSeed is the seed before Randomise Seed picked this run's, or null when it was not on.
    /// </summary>
    private sealed record ActiveRun(DungeonJob Job, DungeonTileSets TileSets, long Seed, long? PreviousSeed, Stopwatch Clock, List<string> Warnings);

    private SpatialMenuHook _menu;
    private TileSetInspectorPlugin _tileSetInspector;
    private DialogInspectorFilterPlugin _dialogFilter;
    private ToolDialog _dialog;
    private GridMap _selected;
    private GridMap _target;
    private DungeonSettings _settings = new();
    private ActiveRun _run;
    private double _sinceProgress;

    /// <summary>Randomised seeds that failed since the dialog was opened. Randomise Seed does not pick them again.</summary>
    private readonly HashSet<long> _failedSeeds = new();

    /// <summary>Set when an older WFC Generator is installed next to this addon, else empty.</summary>
    private string _wfcWarning = "";

    public override void _EnterTree()
    {
        _menu = new SpatialMenuHook(this, "GridMap", "GridMap");
        _menu.AddItem(ToolName, new Callable(this, MethodName.OpenDialog));
        _tileSetInspector = TileSetInspectorPlugin.Register(this);
        _dialogFilter = new DialogInspectorFilterPlugin();
        AddInspectorPlugin(_dialogFilter);

        _dialog = new ToolDialog(ToolName, ActionText, "godoodungeon_dialog_size") { Filter = _dialogFilter };
        _dialog.ActionPressed += OnActionPressed;
        _dialog.Canceled += OnDialogClosed;
        EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);

        _wfcWarning = WfcGeneratorWarning();
        if (_wfcWarning.Length > 0)
            Notify.Report(null, _wfcWarning, ToolDialog.Severity.Warning);
    }

    public override void _ExitTree()
    {
        StopRun();
        DisconnectSettings();
        _menu?.Remove();
        _menu = null;
        if (_tileSetInspector != null)
        {
            TileSetInspectorPlugin.Unregister(this, _tileSetInspector);
            _tileSetInspector = null;
        }
        if (_dialogFilter != null)
        {
            _dialogFilter.Target = null;
            RemoveInspectorPlugin(_dialogFilter);
            _dialogFilter = null;
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
        if (_dialog == null || _run == null)
            return;

        if (!TargetIsAlive)
        {
            StopRun();
            SetRunning(false);
            Notify.Report(_dialog, "Generation was cancelled because the GridMap is no longer in the open scene.", ToolDialog.Severity.Warning);
            return;
        }

        if (_run.Job.IsFinished)
        {
            var finished = _run;
            _run = null;
            SetRunning(false);
            Finish(finished);
            return;
        }

        _sinceProgress += delta;
        if (_sinceProgress < ProgressInterval)
            return;
        _sinceProgress = 0;
        _dialog.SetStatus($"{_run.Job.Stage} ({_run.Clock.Elapsed.TotalSeconds:F1} s)");
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

        _failedSeeds.Clear();
        DisconnectSettings();
        _target = _selected;
        _settings = DungeonSettings.StoredOn(_target) is { } stored ? (DungeonSettings)stored.Duplicate() : new DungeonSettings();
        _settings.Connect(DungeonSettings.SignalName.ClearRequested, new Callable(this, MethodName.OnClearRequested));
        _dialog.Edit(_settings);
        _dialog.SetStatus($"Target: {_target.Name}");
        if (_target.MeshLibrary == null)
            _dialog.AddStatus("The GridMap has no MeshLibrary. Assign the one your tile sets use.", ToolDialog.Severity.Warning);
        if (_wfcWarning.Length > 0)
            _dialog.AddStatus(_wfcWarning, ToolDialog.Severity.Warning);
        // The dialog node outlives assembly reloads; the M0 skeleton left Generate disabled on it.
        _dialog.SetActionEnabled(true);
        SetRunning(false);
        _dialog.PopupForEditing();
    }

    private void DisconnectSettings()
    {
        var onClear = new Callable(this, MethodName.OnClearRequested);
        if (_settings != null && _settings.IsConnected(DungeonSettings.SignalName.ClearRequested, onClear))
            _settings.Disconnect(DungeonSettings.SignalName.ClearRequested, onClear);
    }

    private void OnDialogClosed()
    {
        if (StopRun())
            SetRunning(false);
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
        if (!TargetIsAlive)
        {
            Notify.Report(_dialog, "The target GridMap is no longer in the scene.", ToolDialog.Severity.Error);
            return;
        }

        var tileSets = DungeonTileSets.Check(_settings, _target);
        if (!tileSets.IsValid)
        {
            ReportErrors("The dungeon cannot be generated with these settings.", tileSets.Errors);
            return;
        }

        long? previousSeed = null;
        if (_settings.RandomiseSeed)
        {
            previousSeed = _settings.Seed;
            _settings.PickNewSeed(_failedSeeds);
        }

        var record = DungeonGridMap.ReadRecord(_target);
        var handPlaced = DungeonGridMap.ReadHandPlaced(_target, _settings.BoundsOrigin, _settings.BoundsSize, record);
        var job = new DungeonJob(DungeonGridMap.Request(_settings, tileSets, handPlaced));
        _run = new ActiveRun(job, tileSets, _settings.Seed, previousSeed, Stopwatch.StartNew(), tileSets.Warnings);
        _sinceProgress = ProgressInterval;
        SetRunning(true);
        _dialog.SetStatus("Generating…");
        job.Start();
    }

    private void Finish(ActiveRun run)
    {
        var job = run.Job;
        if (job.Error != null)
        {
            GD.PushError($"godoodungeon: {job.Error}");
            Notify.Report(_dialog, $"The generator failed: {job.Error.Message}", ToolDialog.Severity.Error);
            RestoreSeed(run);
            return;
        }

        var result = job.Result;
        switch (result.Status)
        {
            case DungeonStatus.Solved:
                Commit(run, result);
                return;
            case DungeonStatus.Cancelled:
                RestoreSeed(run);
                _dialog.SetStatus("Generation stopped. The GridMap was not changed.", ToolDialog.Severity.Warning);
                return;
            case DungeonStatus.Invalid:
                ReportErrors("The dungeon cannot be generated with these settings.", result.Errors);
                RestoreSeed(run);
                return;
            case DungeonStatus.InternalError:
                ReportErrors("The dungeon was generated, but failed its connectivity check.", result.Errors);
                RestoreSeed(run);
                return;
            default:
                Notify.Report(_dialog, $"No layout could be generated in {result.Attempts.Count} attempt(s). The GridMap was not changed.",
                    ToolDialog.Severity.Error);
                foreach (var failure in result.Attempts.Take(MaxReportedErrors))
                    _dialog.AddStatus($"Layout {failure.Attempt + 1}: {failure.Message}", ToolDialog.Severity.Error);
                ReportFailedSeed(run);
                return;
        }
    }

    private void Commit(ActiveRun run, DungeonResult result)
    {
        var previous = DungeonGridMap.ReadRecord(_target);
        var cells = DungeonGridMap.Cells(result, run.TileSets.Room, run.TileSets.Corridor);
        var (changes, record) = DungeonGridMap.Write(_target, previous, cells);
        var stored = (DungeonSettings)_settings.Duplicate();

        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction(ToolName, UndoRedo.MergeMode.Disable, _target);
        undoRedo.AddDoMethod(this, MethodName.ApplyCells, _target, changes.Do);
        undoRedo.AddUndoMethod(this, MethodName.ApplyCells, _target, changes.Undo);
        undoRedo.AddDoMethod(this, MethodName.ApplySettings, _target, stored);
        undoRedo.AddUndoMethod(this, MethodName.ApplySettings, _target, DungeonSettings.StoredOn(_target));
        undoRedo.AddDoMethod(this, MethodName.ApplyRecord, _target, record.Encode());
        undoRedo.AddUndoMethod(this, MethodName.ApplyRecord, _target, DungeonGridMap.StoredRecord(_target));
        var markers = AddMarkerChanges(undoRedo, result, run.TileSets, previous, cells.Keys.ToHashSet());
        undoRedo.CommitAction();

        var layout = result.Layout;
        _dialog.SetStatus($"Generated {layout.Rooms.Count} rooms, {layout.Doors.Count} doors and {result.RouteCount} corridor routes "
            + $"in {run.Clock.Elapsed.TotalSeconds:F1} s. Seed {run.Seed}.", ToolDialog.Severity.Success);
        if (result.LayoutAttempt > 0)
            _dialog.AddStatus($"Used layout {result.LayoutAttempt + 1}; the earlier one(s) could not be finished: "
                + string.Join(" ", result.Attempts.Select(a => a.Message)));
        foreach (string warning in layout.Warnings.Concat(run.Warnings))
            _dialog.AddStatus(warning, ToolDialog.Severity.Warning);
        if (markers.Placed > 0 || markers.Removed > 0)
            _dialog.AddStatus($"Placed {markers.Placed:N0} markers in {DungeonGridMap.MarkerContainerName}"
                + (markers.Removed > 0 ? $", replacing {markers.Removed:N0} from the previous dungeon." : "."));
        if (markers.Skipped > 0)
            _dialog.AddStatus($"Skipped {markers.Skipped:N0} markers whose scene root is not a Node3D.", ToolDialog.Severity.Warning);
    }

    /// <summary>
    /// Adds the marker changes to the open undo action: the previous dungeon's markers on cells it
    /// still holds or the new one writes go, new ones are instanced in the DungeonMarkers container.
    /// </summary>
    private (int Placed, int Removed, int Skipped) AddMarkerChanges(EditorUndoRedoManager undoRedo, DungeonResult result, DungeonTileSets tileSets,
        GeneratedRecord previous, HashSet<Vector3I> written)
    {
        var placements = DungeonGridMap.PlanMarkers(_target, result, tileSets.Room, tileSets.Corridor);
        var replaced = DungeonGridMap.ReplacedMarkers(_target, previous, written);
        if (placements.Count == 0 && replaced.Count == 0)
            return (0, 0, 0);

        var root = EditorInterface.Singleton.GetEditedSceneRoot() ?? _target;
        var container = DungeonGridMap.MarkerContainer(_target);
        if (container == null)
        {
            container = new Node3D { Name = DungeonGridMap.MarkerContainerName };
            undoRedo.AddDoMethod(_target, Node.MethodName.AddChild, container, true);
            undoRedo.AddDoMethod(container, Node.MethodName.SetOwner, root);
            undoRedo.AddDoReference(container);
            undoRedo.AddUndoMethod(_target, Node.MethodName.RemoveChild, container);
        }
        AddMarkerRemovals(undoRedo, container, root, replaced);
        int placed = 0, skipped = 0;
        foreach (var placement in placements)
        {
            var marker = DungeonGridMap.InstantiateMarker(placement);
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

    private static void AddMarkerRemovals(EditorUndoRedoManager undoRedo, Node3D container, Node root, List<Node3D> markers)
    {
        foreach (var old in markers)
        {
            undoRedo.AddDoMethod(container, Node.MethodName.RemoveChild, old);
            undoRedo.AddUndoMethod(container, Node.MethodName.AddChild, old, true);
            undoRedo.AddUndoMethod(old, Node.MethodName.SetOwner, root);
            undoRedo.AddUndoReference(old);
        }
    }

    /// <summary>Removes the cells the last dungeon wrote, as one undo step. Cells placed or edited by hand stay.</summary>
    public void OnClearRequested()
    {
        if (!TargetIsAlive)
            return;
        if (_run != null)
        {
            _dialog.AddStatus("Stop the running generation before clearing.", ToolDialog.Severity.Warning);
            return;
        }
        _dialog.ClearStatus();
        var record = DungeonGridMap.ReadRecord(_target);
        var clear = DungeonGridMap.Clear(_target, record);
        var markers = DungeonGridMap.ReplacedMarkers(_target, record, new HashSet<Vector3I>());
        int cleared = clear.Do.Length / CellChanges.Stride;
        if (cleared == 0 && markers.Count == 0 && record.Count == 0)
        {
            _dialog.SetStatus("Nothing to clear: this GridMap has no generated dungeon.");
            return;
        }

        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Clear Dungeon", UndoRedo.MergeMode.Disable, _target);
        undoRedo.AddDoMethod(this, MethodName.ApplyCells, _target, clear.Do);
        undoRedo.AddUndoMethod(this, MethodName.ApplyCells, _target, clear.Undo);
        undoRedo.AddDoMethod(this, MethodName.ApplyRecord, _target, System.Array.Empty<byte>());
        undoRedo.AddUndoMethod(this, MethodName.ApplyRecord, _target, DungeonGridMap.StoredRecord(_target));
        if (markers.Count > 0)
            AddMarkerRemovals(undoRedo, DungeonGridMap.MarkerContainer(_target), EditorInterface.Singleton.GetEditedSceneRoot() ?? _target, markers);
        undoRedo.CommitAction();

        int kept = record.Count - cleared;
        _dialog.SetStatus($"Cleared {cleared:N0} cells of the dungeon.", ToolDialog.Severity.Success);
        if (kept > 0)
            _dialog.AddStatus($"Kept {kept:N0} dungeon cells you edited since.");
        if (markers.Count > 0)
            _dialog.AddStatus($"Removed {markers.Count:N0} markers from {DungeonGridMap.MarkerContainerName}.");
    }

    /// <summary>Do and undo target for GridMap cells.</summary>
    public void ApplyCells(GridMap map, int[] changes)
    {
        if (IsInstanceValid(map))
            GridMapCells.Apply(map, changes);
    }

    /// <summary>Do and undo target for the settings stored on the GridMap. Null removes them.</summary>
    public void ApplySettings(GridMap map, DungeonSettings settings)
    {
        if (IsInstanceValid(map))
            DungeonSettings.StoreOn(map, settings);
    }

    /// <summary>Do and undo target for the record of what the dungeon wrote.</summary>
    public void ApplyRecord(GridMap map, byte[] record)
    {
        if (IsInstanceValid(map))
            DungeonGridMap.WriteRecord(map, record);
    }

    /// <summary>Cancels a running generation and waits for its thread. Returns true if one was running.</summary>
    private bool StopRun()
    {
        if (_run == null)
            return false;
        _run.Job.Cancel();
        if (!_run.Job.Wait(5000))
            GD.PushError("godoodungeon: the generator thread did not stop in time.");
        RestoreSeed(_run);
        _run = null;
        return true;
    }

    /// <summary>Puts Seed back to the seed before a randomised Generate that did not change the GridMap. Returns that seed, or null.</summary>
    private long? RestoreSeed(ActiveRun run)
    {
        if (run.PreviousSeed is not long previous || _settings.Seed != run.Seed)
            return null;
        _settings.Seed = previous;
        _settings.NotifyPropertyListChanged();
        return previous;
    }

    /// <summary>After a failed run: restores the seed and keeps the failed one from being picked again.</summary>
    private void ReportFailedSeed(ActiveRun run)
    {
        if (RestoreSeed(run) is not long previous)
            return;
        _failedSeeds.Add(run.Seed);
        _dialog.AddStatus($"Seed {run.Seed} failed, so Seed is back at {previous}, the seed before this Generate. The next Generate tries a new seed.");
    }

    private void ReportErrors(string headline, IReadOnlyList<string> errors)
    {
        Notify.Report(_dialog, headline, ToolDialog.Severity.Error);
        foreach (string error in errors.Take(MaxReportedErrors))
            _dialog.AddStatus(error, ToolDialog.Severity.Error);
        if (errors.Count > MaxReportedErrors)
            _dialog.AddStatus($"… and {errors.Count - MaxReportedErrors} more.", ToolDialog.Severity.Error);
    }

    private void SetRunning(bool running)
    {
        if (_dialog != null)
            _dialog.OkButtonText = running ? StopText : ActionText;
    }

    /// <summary>A warning when WFC Generator older than 1.3.3 is installed (brief godoodungeon §4.1), else empty.</summary>
    private static string WfcGeneratorWarning()
    {
        if (!FileAccess.FileExists(WfcGeneratorConfig))
            return "";
        var config = new ConfigFile();
        if (config.Load(WfcGeneratorConfig) != Error.Ok)
            return "";
        string version = config.GetValue("plugin", "version", "").AsString();
        return WfcGeneratorVersion.NeedsUpdate(version)
            ? $"godoodungeon: WFC Generator {version} is installed. Update it to {WfcGeneratorVersion.Minimum} or later. "
              + "Older versions keep their own copy of the tile set classes, so tile sets load as types godoodungeon cannot use."
            : "";
    }
}
#endif
