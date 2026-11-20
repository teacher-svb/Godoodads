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
public partial class GodoodungeonPlugin : EditorPlugin, ISerializationListener, IBoundsBoxes
{
    /// <summary>Menu item, dialog title and undo action name. GDDS = Godoodads, this suite.</summary>
    private const string ToolName = "Generate Dungeon (GDDS)";
    private const string ActionText = "Generate";
    private const string StopText = "Stop";
    private const double ProgressInterval = 0.1;
    private const int MaxReportedErrors = 12;
    private const string WfcGeneratorConfig = "res://addons/wfc_generator/plugin.cfg";
    // Vertex colours are marked sRGB on the overlay material, so these show as written (the board saw
    // the first violet as yellow: linear colours are brightened, and the demo scene tonemaps).
    private static readonly Color BoundsColour = new(0.55f, 0.2f, 0.95f);
    private static readonly Color RoomColour = new(0.3f, 0.8f, 1f);
    private static readonly Color GeneratedBoxColour = new(0.55f, 0.95f, 1f);
    private static readonly Color BuiltBoxColour = new(1f, 0.6f, 0.25f);
    private static readonly Color DoorColour = new(1f, 0.85f, 0.2f);
    private static readonly Color CorridorColour = new(0.45f, 1f, 0.45f);
    private static readonly Color FailureColour = new(1f, 0.15f, 0.15f);

    /// <summary>
    /// One running generation. Never survives an assembly reload, which cancels it first.
    /// PreviousSeed is the seed before Randomise Seed picked this run's, or null when it was not on.
    /// Settings is a copy taken at Generate: the dialog stays editable while the generation runs,
    /// and the GridMap stores the settings that made the result.
    /// </summary>
    private sealed record ActiveRun(DungeonJob Job, DungeonTileSets TileSets, long Seed, long? PreviousSeed, Stopwatch Clock, List<string> Warnings,
        DungeonSettings Settings);

    private SpatialMenuHook _menu;
    private TileSetInspectorPlugin _tileSetInspector;
    private DialogInspectorFilterPlugin _dialogFilter;
    private ToolDialog _dialog;
    private GridMap _selected;
    private GridMap _target;
    private DungeonSettings _settings = new();
    private ActiveRun _run;
    private double _sinceProgress;
    private BoundsGizmoPlugin _gizmo;
    private RoomBoxGizmoPlugin _roomBoxGizmo;
    private MeshInstance3D _overlay;
    private string _overlayState;
    private string _previewKey;
    private PreviewResult _preview;

    /// <summary>How often Preview Layout checks the GridMap for painted, erased or edited cells.</summary>
    private const ulong CellCheckMsec = 250;
    private long _cellFingerprint;
    private ulong _cellsCheckedAt;

    /// <summary>Cells where the last Generate failed, drawn in red until the next Generate or dialog open.</summary>
    private Godot.Collections.Array<Vector3I> _failedCells = new();

    /// <summary>Names of room boxes the last errors were about, drawn in red.</summary>
    private Godot.Collections.Array<string> _failedBoxes = new();

    /// <summary>True while the status shows a result the preview should add to rather than replace.</summary>
    private bool _keepStatus;

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
        _gizmo = new BoundsGizmoPlugin { Owner = this, GizmoName = "godoodungeon bounds" };
        AddNode3DGizmoPlugin(_gizmo);
        _roomBoxGizmo = new RoomBoxGizmoPlugin();
        AddNode3DGizmoPlugin(_roomBoxGizmo);
        // Forwarded input reaches this plugin before the GridMap editor, which would take handle drags.
        SetInputEventForwardingAlwaysEnabled();

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
        RemoveOverlay();
        DisconnectSettings();
        if (_gizmo != null)
        {
            _gizmo.Owner = null;
            RemoveNode3DGizmoPlugin(_gizmo);
            _gizmo = null;
        }
        if (_roomBoxGizmo != null)
        {
            RemoveNode3DGizmoPlugin(_roomBoxGizmo);
            _roomBoxGizmo = null;
        }
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

        // A drag whose mouse release was lost (released over the dialog) ends on the settings it started on.
        if (_dragHandle >= 0)
            CommitBox(_dragHandle / Directions.Count, _dragOrigin, _dragSize, true);
        _dragHandle = -1;
        _failedSeeds.Clear();
        _failedCells.Clear();
        _failedBoxes.Clear();
        _keepStatus = false;
        _previewKey = null;
        DisconnectSettings();
        _target = _selected;
        _settings = DungeonSettings.StoredOn(_target) is { } stored ? (DungeonSettings)stored.Duplicate() : new DungeonSettings();
        _settings.Connect(DungeonSettings.SignalName.ClearRequested, new Callable(this, MethodName.OnClearRequested));
        _settings.Connect(DungeonSettings.SignalName.AddRoomBoxRequested, new Callable(this, MethodName.OnAddRoomBoxRequested));
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
        var onAdd = new Callable(this, MethodName.OnAddRoomBoxRequested);
        if (_settings != null && _settings.IsConnected(DungeonSettings.SignalName.AddRoomBoxRequested, onAdd))
            _settings.Disconnect(DungeonSettings.SignalName.AddRoomBoxRequested, onAdd);
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
        _failedCells.Clear();
        _failedBoxes.Clear();
        _keepStatus = true;
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
        var job = new DungeonJob(DungeonGridMap.Request(_settings, tileSets, handPlaced,
            DungeonGridMap.ReadRoomBoxes(_target, _settings.BoundsOrigin, _settings.Door)));
        _run = new ActiveRun(job, tileSets, _settings.Seed, previousSeed, Stopwatch.StartNew(), tileSets.Warnings, (DungeonSettings)_settings.Duplicate());
        _sinceProgress = ProgressInterval;
        SetRunning(true);
        _dialog.SetStatus("Generating…");
        job.Start();
    }

    private void Finish(ActiveRun run)
    {
        _keepStatus = true;
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
                MarkFailedBoxes(result.Errors);
                RestoreSeed(run);
                return;
            case DungeonStatus.InternalError:
                ReportErrors("The dungeon was generated, but failed its connectivity check.", result.Errors);
                foreach (int room in result.UnreachableRooms)
                    _failedCells.Add(result.Layout.GridCell(result.Layout.Rooms[room].Position));
                if (_failedCells.Count > 0)
                    _dialog.AddStatus("Red boxes in the viewport mark the unreachable rooms.");
                RestoreSeed(run);
                return;
            default:
                Notify.Report(_dialog, $"No layout could be generated in {result.Attempts.Count} attempt(s). The GridMap was not changed.",
                    ToolDialog.Severity.Error);
                MarkFailedBoxes(result.Attempts.Select(a => a.Message));
                foreach (var failure in result.Attempts.Take(MaxReportedErrors))
                {
                    _dialog.AddStatus($"Layout {failure.Attempt + 1}: {failure.Message}", ToolDialog.Severity.Error);
                    if (failure.Cell is { } cell)
                        _failedCells.Add(cell);
                }
                if (_failedCells.Count > 0)
                    _dialog.AddStatus("Red boxes in the viewport mark where each layout failed.");
                ReportFailedSeed(run);
                return;
        }
    }

    private void Commit(ActiveRun run, DungeonResult result)
    {
        double generated = run.Clock.Elapsed.TotalSeconds;
        var writing = Stopwatch.StartNew();
        var previous = DungeonGridMap.ReadRecord(_target);
        var cells = DungeonGridMap.Cells(result, run.TileSets.Room, run.TileSets.Corridor);
        var (changes, record) = DungeonGridMap.Write(_target, previous, cells);
        var stored = run.Settings;
        var graph = DungeonGridMap.Graph(result, _target);
        var previousGraph = DungeonGraph.StoredOn(_target);
        int replacedData = DungeonGridMap.EntriesWithData(previousGraph, _target);

        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction(ToolName, UndoRedo.MergeMode.Disable, _target);
        undoRedo.AddDoMethod(this, MethodName.ApplyCells, _target, changes.Do);
        undoRedo.AddUndoMethod(this, MethodName.ApplyCells, _target, changes.Undo);
        var previousSettings = DungeonSettings.StoredOn(_target);
        undoRedo.AddDoMethod(this, MethodName.ApplySettings, _target, stored);
        undoRedo.AddUndoMethod(this, MethodName.ApplySettings, _target, previousSettings);
        // The open dialog's Seed follows the dungeon on the map, so Preview Layout and its room
        // numbers show that dungeon after undo and redo (decided at the 1.1 release review).
        undoRedo.AddDoMethod(this, MethodName.ApplySeed, _target, run.Seed);
        undoRedo.AddUndoMethod(this, MethodName.ApplySeed, _target, previousSettings?.Seed ?? run.PreviousSeed ?? run.Seed);
        undoRedo.AddDoMethod(this, MethodName.ApplyRecord, _target, record.Encode());
        undoRedo.AddUndoMethod(this, MethodName.ApplyRecord, _target, DungeonGridMap.StoredRecord(_target));
        undoRedo.AddDoMethod(this, MethodName.ApplyGraph, _target, graph);
        undoRedo.AddUndoMethod(this, MethodName.ApplyGraph, _target, previousGraph);
        var markers = AddMarkerChanges(undoRedo, result, run.TileSets, previous, cells.Keys.ToHashSet());
        long? cellsBefore = CellsBeforeWrite();
        undoRedo.CommitAction();
        KeepPreviewAfterWrite(cellsBefore);

        var layout = result.Layout;
        _dialog.SetStatus($"Generated {layout.Rooms.Count} rooms{BoxCounts(layout)}, {layout.Doors.Count} doors and {result.RouteCount} corridors "
            + $"in {generated:F1} s, written in {writing.Elapsed.TotalSeconds:F1} s. Seed {run.Seed}.", ToolDialog.Severity.Success);
        if (result.LayoutAttempt > 0)
            _dialog.AddStatus($"Used layout {result.LayoutAttempt + 1}; the earlier one(s) could not be finished: "
                + string.Join(" ", result.Attempts.Select(a => a.Message)));
        if (graph != null)
            _dialog.AddStatus($"Stored the dungeon graph in the GridMap's metadata ({DungeonGraph.MetaKey}): {graph.Rooms.Count} rooms, "
                + $"{graph.Corridors.Count} corridors ({graph.Corridors.Count(c => !c.IsTreeEdge)} loops).");
        if (replacedData > 0)
            _dialog.AddStatus($"The previous dungeon graph had Data on {replacedData} room(s) and corridor(s); the new graph starts without it. "
                + "Ctrl+Z brings the previous dungeon and its graph back.", ToolDialog.Severity.Warning);
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

    /// <summary>
    /// Adds a Generated room box to the target GridMap, owned by the edited scene, centred in the
    /// bounds at Max Room Size and Room Height (brief §13.1), as one undo step, and selects it.
    /// </summary>
    public void OnAddRoomBoxRequested()
    {
        if (!TargetIsAlive || _run != null)
            return;
        var (origin, size) = DungeonRoomBox.CentredIn(_settings.BoundsOrigin, _settings.BoundsSize, _settings.MaxRoomSize, _settings.RoomHeight);
        var box = new DungeonRoomBox { Name = "RoomBox", Origin = origin, Size = size };
        var root = EditorInterface.Singleton.GetEditedSceneRoot() ?? _target;
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Add Room Box", UndoRedo.MergeMode.Disable, _target);
        undoRedo.AddDoMethod(_target, Node.MethodName.AddChild, box, true);
        undoRedo.AddDoMethod(box, Node.MethodName.SetOwner, root);
        undoRedo.AddDoReference(box);
        undoRedo.AddUndoMethod(_target, Node.MethodName.RemoveChild, box);
        undoRedo.CommitAction();

        var selection = EditorInterface.Singleton.GetSelection();
        selection.Clear();
        selection.AddNode(box);
        _keepStatus = true;
        _dialog.SetStatus($"Added {box.Name} (room {HandleBoxes().IndexOf(box) + 1}): cells {origin} to {origin + size - Vector3I.One}. "
            + "Set its Mode to Built to keep cells you build in it, drag its handles to move its walls.", ToolDialog.Severity.Success);
    }

    /// <summary>" (2 room boxes, 1 built)", or empty without boxes.</summary>
    private static string BoxCounts(DungeonLayout layout)
    {
        int boxes = layout.Boxes.Count;
        if (boxes == 0)
            return "";
        int built = layout.Boxes.Count(b => b.IsBuilt);
        return $" ({boxes} room box{(boxes == 1 ? "" : "es")}" + (built > 0 ? $", {built} built)" : ")");
    }

    /// <summary>Marks the room boxes the messages name, so the overlay draws them in red.</summary>
    private void MarkFailedBoxes(IEnumerable<string> messages)
    {
        var text = string.Join("\n", messages);
        foreach (var box in HandleBoxes())
            if (text.Contains($"`{box.Name}`") && !_failedBoxes.Contains(box.Name))
                _failedBoxes.Add(box.Name);
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
        var graph = DungeonGraph.StoredOn(_target);
        if (cleared == 0 && markers.Count == 0 && record.Count == 0 && graph == null)
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
        undoRedo.AddDoMethod(this, MethodName.ApplyGraph, _target, (DungeonGraph)null);
        undoRedo.AddUndoMethod(this, MethodName.ApplyGraph, _target, graph);
        if (markers.Count > 0)
            AddMarkerRemovals(undoRedo, DungeonGridMap.MarkerContainer(_target), EditorInterface.Singleton.GetEditedSceneRoot() ?? _target, markers);
        long? cellsBefore = CellsBeforeWrite();
        undoRedo.CommitAction();
        KeepPreviewAfterWrite(cellsBefore);

        int kept = record.Count - cleared;
        _keepStatus = true;
        _failedCells.Clear();
        _failedBoxes.Clear();
        _dialog.SetStatus($"Cleared {cleared:N0} cells of the dungeon.", ToolDialog.Severity.Success);
        if (kept > 0)
            _dialog.AddStatus($"Kept {kept:N0} dungeon cells you edited since.");
        if (markers.Count > 0)
            _dialog.AddStatus($"Removed {markers.Count:N0} markers from {DungeonGridMap.MarkerContainerName}.");
        if (graph != null)
            _dialog.AddStatus($"Removed the dungeon graph from the GridMap's metadata"
                + (DungeonGridMap.EntriesWithData(graph, _target) > 0 ? ", with its Data. Ctrl+Z brings it back." : "."));
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

    /// <summary>Do and undo target for the dungeon graph stored on the GridMap (brief §12.2). Null removes it.</summary>
    public void ApplyGraph(GridMap map, DungeonGraph graph)
    {
        if (IsInstanceValid(map))
            DungeonGraph.StoreOn(map, graph);
    }

    /// <summary>
    /// Do and undo target for the open dialog's Seed: when the dialog edits this GridMap, its Seed
    /// becomes the seed of the dungeon now on the map. Other settings stay as the user left them.
    /// </summary>
    public void ApplySeed(GridMap map, long seed)
    {
        if (_settings == null || !TargetIsAlive || _target != map || _settings.Seed == seed)
            return;
        _settings.Seed = seed;
        _settings.NotifyPropertyListChanged();
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

    // ---- Viewport overlay and bounds handles -----------------------------------------------

    /// <summary>Draws the bounds while the dialog is open. The overlay has no owner, so it is never saved.</summary>
    private void UpdateOverlay()
    {
        if (!_dialog.Visible || !TargetIsAlive)
        {
            RemoveOverlay();
            return;
        }

        var (min, max) = BoundsGizmoPlugin.Extents(_target, _settings.BoundsOrigin, _settings.BoundsSize);
        UpdatePreview();
        // The run flag is part of the state, so the handles hide when a generation starts and come back after.
        string boxes = string.Join(";", HandleBoxes().Select(b => $"{b.Origin}{b.Size}{b.Mode}"));
        string state = $"{min}|{max}|{boxes}|{(_settings.PreviewLayout ? _previewKey : "")}|{_run != null}|{string.Join(";", _failedCells)}|{string.Join(";", _failedBoxes)}";
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
                    VertexColorIsSrgb = true,
                    DisableFog = true,
                },
            };
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
        if (_settings.PreviewLayout && _preview?.Layout is { } layout)
            DrawPreview(mesh, layout, _preview.Corridors);
        var half = _target.CellSize * 0.5f;
        foreach (var cell in _failedCells)
        {
            // Slightly larger than the cell, so the box stays visible around a placed module.
            var centre = _target.MapToLocal(cell);
            AddBox(mesh, centre - half * 1.08f, centre + half * 1.08f, FailureColour);
        }
        foreach (var box in HandleBoxes())
            if (_failedBoxes.Contains(box.Name))
            {
                var (boxMin, boxMax) = BoundsGizmoPlugin.Extents(_target, box.Origin, box.Size);
                AddBox(mesh, boxMin - half * 0.1f, boxMax + half * 0.1f, FailureColour);
            }
        mesh.SurfaceEnd();
        UpdateRoomLabels(_settings.PreviewLayout ? _preview?.Layout : null);
    }

    /// <summary>
    /// Room numbers above the preview's rooms (brief §12.4): the number status messages use, which
    /// is <c>DungeonGraph.Rooms[number - 1]</c> after Generate. Children of the overlay, so they have
    /// no owner either and go with it.
    /// </summary>
    private void UpdateRoomLabels(DungeonLayout layout)
    {
        foreach (var child in _overlay.GetChildren())
            if (child is Label3D old)
            {
                _overlay.RemoveChild(old);
                old.QueueFree();
            }
        if (layout == null)
            return;
        var cell = _target.CellSize;
        for (int r = 0; r < layout.Rooms.Count; r++)
        {
            var room = layout.Rooms[r];
            var (min, max) = BoundsGizmoPlugin.Extents(_target, layout.GridCell(room.Position),
                new Vector3I(room.Size.X, _settings.RoomHeight, room.Size.Y));
            _overlay.AddChild(new Label3D
            {
                Text = (r + 1).ToString(),
                Position = new Vector3((min.X + max.X) / 2, max.Y, (min.Z + max.Z) / 2),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true,
                FontSize = 64,
                OutlineSize = 16,
                PixelSize = Mathf.Min(cell.X, cell.Z) * 0.025f,
                Modulate = RoomColour,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }
    }

    /// <summary>The settings Preview Layout depends on. The GridMap's cells are in the key too, see <see cref="UpdatePreview"/>.</summary>
    private string PreviewKey() => string.Join("|", string.Join(";", HandleBoxes().Select(b => $"{b.Name}{b.Origin}{b.Size}{b.Mode}")),
        _settings.Door, _settings.BoundsOrigin, _settings.BoundsSize, _settings.RoomCount, _settings.MinRoomSize,
        _settings.MaxRoomSize, _settings.RoomHeight, _settings.Gap, _settings.Margin, _settings.WallClearance, _settings.ExtraConnections,
        _settings.MaxDoorsPerRoom, _settings.PathWander, _settings.Seed, _settings.LayoutAttempts, _settings.AttemptsPerRoom);

    /// <summary>
    /// After Generate or Clear Dungeon wrote the GridMap: the cells changed, but not which of them
    /// were placed by hand, so the preview still holds. Takes the new fingerprint without
    /// recomputing, so the preview's line and layout warnings are not added below the result again.
    /// Does nothing when a setting changed too, or the preview was not up to date: <paramref name="before"/>
    /// is the fingerprint just before the write, so a cell painted since the last check still counts.
    /// </summary>
    /// <summary>The fingerprint before Generate or Clear Dungeon writes, or null when Preview Layout has no preview.</summary>
    private long? CellsBeforeWrite() => _previewKey == null ? null : DungeonGridMap.Fingerprint(_target);

    private void KeepPreviewAfterWrite(long? before)
    {
        if (before == null || _previewKey != PreviewKey() + "|" + before)
            return;
        _cellFingerprint = DungeonGridMap.Fingerprint(_target);
        _cellsCheckedAt = Time.GetTicksMsec();
        _previewKey = PreviewKey() + "|" + _cellFingerprint;
    }

    /// <summary>
    /// Recomputes the preview when it is on and a setting it depends on, or the GridMap's cells,
    /// changed. The cells are fingerprinted a few times per second, not every frame.
    /// </summary>
    private void UpdatePreview()
    {
        if (!_settings.PreviewLayout)
        {
            _previewKey = null;
            _preview = null;
            return;
        }
        ulong now = Time.GetTicksMsec();
        if (_previewKey == null || now - _cellsCheckedAt >= CellCheckMsec)
        {
            _cellsCheckedAt = now;
            _cellFingerprint = DungeonGridMap.Fingerprint(_target);
        }
        string key = PreviewKey() + "|" + _cellFingerprint;
        if (key == _previewKey)
            return;
        _previewKey = key;

        var record = DungeonGridMap.ReadRecord(_target);
        var request = new DungeonRequest
        {
            Layout = _settings.ToLayoutSettings(),
            Origin = _settings.BoundsOrigin,
            Seed = _settings.Seed,
            LayoutAttempts = _settings.LayoutAttempts,
            PathWander = _settings.PathWander,
            HandPlaced = DungeonGridMap.ReadHandPlaced(_target, _settings.BoundsOrigin, _settings.BoundsSize, record),
            RoomBoxes = DungeonGridMap.ReadRoomBoxes(_target, _settings.BoundsOrigin, _settings.Door),
        };
        _preview = DungeonPreview.Plan(request);
        _failedBoxes.Clear();
        MarkFailedBoxes(_preview.Errors.Concat(_preview.Failures.Select(f => f.Message)));
        if (_run != null)
            return;
        // Right after a result (or a seed put back after a failure), add the preview below it.
        bool add = _keepStatus;
        _keepStatus = false;
        void Line(string text, ToolDialog.Severity severity = ToolDialog.Severity.Info)
        {
            if (add)
                _dialog.AddStatus(text, severity);
            else
                _dialog.SetStatus(text, severity);
        }
        if (_preview.IsValid)
            Line($"Preview: {_preview.Layout.Rooms.Count} rooms{BoxCounts(_preview.Layout)}, {_preview.Layout.Doors.Count} doors, {_preview.Corridors.RouteCount} corridors"
                + (_preview.LayoutAttempt > 0 ? $" (layout {_preview.LayoutAttempt + 1})" : "") + $". Seed {_settings.Seed}.");
        else
            Line("Preview: " + string.Join(" ", _preview.Errors.Concat(_preview.Failures.Select(f => $"Layout {f.Attempt + 1}: {f.Message}"))),
                ToolDialog.Severity.Warning);
        foreach (string warning in _preview.Layout?.Warnings ?? new List<string>())
            _dialog.AddStatus(warning, ToolDialog.Severity.Warning);
    }

    /// <summary>Rooms as boxes, doors as small boxes and corridors as lines along their cells, on the floor of the bounds.</summary>
    private void DrawPreview(ImmediateMesh mesh, DungeonLayout layout, CorridorPlan corridors)
    {
        var origin = _settings.BoundsOrigin;
        var cell = _target.CellSize;
        for (int r = 0; r < layout.Rooms.Count; r++)
        {
            var room = layout.Rooms[r];
            var (min, max) = BoundsGizmoPlugin.Extents(_target, layout.GridCell(room.Position),
                new Vector3I(room.Size.X, layout.Height(r), room.Size.Y));
            var colour = layout.BoxOf(r) is { } box ? (box.IsBuilt ? BuiltBoxColour : GeneratedBoxColour) : RoomColour;
            AddBox(mesh, min + cell * 0.04f, max - cell * 0.04f, colour);
        }
        foreach (var door in layout.Doors)
        {
            var centre = _target.MapToLocal(layout.GridCell(door.Cell));
            AddBox(mesh, centre - cell * 0.3f, centre + cell * 0.3f, DoorColour);
        }
        if (corridors == null)
            return;
        float floor = -cell.Y * 0.45f;
        for (int i = 0; i < corridors.Faces.Length; i++)
        {
            int faces = corridors.Faces[i];
            if (faces == 0)
                continue;
            var centre = _target.MapToLocal(origin + new Vector3I(i % corridors.Size.X, 0, i / corridors.Size.X)) + new Vector3(0, floor, 0);
            foreach (int direction in DungeonTileRules.HorizontalDirections)
            {
                if ((faces & (1 << direction)) == 0)
                    continue;
                var offset = direction switch
                {
                    Directions.PosX => new Vector3(cell.X * 0.5f, 0, 0),
                    Directions.NegX => new Vector3(-cell.X * 0.5f, 0, 0),
                    Directions.PosZ => new Vector3(0, 0, cell.Z * 0.5f),
                    _ => new Vector3(0, 0, -cell.Z * 0.5f),
                };
                mesh.SurfaceSetColor(CorridorColour);
                mesh.SurfaceAddVertex(centre);
                mesh.SurfaceSetColor(CorridorColour);
                mesh.SurfaceAddVertex(centre + offset);
            }
        }
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
            // A drag that loses its target (deselected, dialog closed, generation started) is cancelled.
            if (_dragHandle >= 0)
                CommitBox(_dragHandle / Directions.Count, _dragOrigin, _dragSize, true);
            _dragHandle = -1;
            return pass;
        }

        if (_dragHandle >= 0)
        {
            int box = _dragHandle / Directions.Count;
            switch (@event)
            {
                case InputEventMouseMotion motion:
                    bool room = box > 0 && _dragRoomBox != null && IsInstanceValid(_dragRoomBox);
                    var (origin, size) = BoundsGizmoPlugin.DragFace(map, viewportCamera, motion.Position, _dragHandle % Directions.Count,
                        room ? _dragRoomBox.Origin : BoxOrigin(box), room ? _dragRoomBox.Size : BoxSize(box));
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
            _dragRoomBox = RoomBoxAt(handle / Directions.Count);
            _dragOrigin = BoxOrigin(handle / Directions.Count);
            _dragSize = BoxSize(handle / Directions.Count);
            return stop;
        }
        return pass;
    }

    /// <summary>The handle id (box × 6 + face) under the mouse, or -1. The nearest one wins when handles overlap.</summary>
    private int HandleAt(GridMap map, Camera3D camera, Vector2 mouse)
    {
        int best = -1;
        float bestDistance = HandleRadius;
        for (int box = 0; box < BoxCount; box++)
        {
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

    /// <summary>
    /// The GridMap whose bounds have handles: the dialog's target while the dialog is open and no
    /// generation runs.
    /// </summary>
    public GridMap HandleTarget => _dialog != null && _dialog.Visible && _run == null && TargetIsAlive ? _target : null;

    // Box 0 is the bounds; boxes 1 to n are the enabled room box children in child order (§13.5).
    public int BoxCount => 1 + HandleBoxes().Count;

    public bool BoxVisible(int box) => box < BoxCount;

    public Vector3I BoxOrigin(int box) => box == 0 ? _settings.BoundsOrigin : RoomBoxAt(box)?.Origin ?? Vector3I.Zero;

    public Vector3I BoxSize(int box) => box == 0 ? _settings.BoundsSize : RoomBoxAt(box)?.Size ?? Vector3I.Zero;

    private DungeonRoomBox _dragRoomBox;

    private List<DungeonRoomBox> HandleBoxes() => TargetIsAlive ? GridMapBox.ActiveChildren<DungeonRoomBox>(_target) : new List<DungeonRoomBox>();

    /// <summary>The room box with handle box number <paramref name="box"/> (1 for the first), or null.</summary>
    private DungeonRoomBox RoomBoxAt(int box)
    {
        var boxes = HandleBoxes();
        return box >= 1 && box <= boxes.Count ? boxes[box - 1] : null;
    }

    /// <summary>Changes a box while its handle is dragged. Undo is added in <see cref="CommitBox"/>.</summary>
    private void SetBox(int box, Vector3I origin, Vector3I size)
    {
        if (box == 0)
            _settings.SetBounds(origin, size);
        else if (_dragRoomBox != null && IsInstanceValid(_dragRoomBox))
        {
            _dragRoomBox.Origin = origin;
            _dragRoomBox.Size = size;
        }
        if (TargetIsAlive)
            _target.UpdateGizmos();
    }

    /// <summary>
    /// Ends a handle drag: puts the bounds back when cancelled, otherwise adds one undo step. The step
    /// targets the settings resource that was dragged, so undoing it after the dialog was opened for
    /// another GridMap leaves that GridMap's settings alone.
    /// </summary>
    private void CommitBox(int box, Vector3I origin, Vector3I size, bool cancel)
    {
        if (cancel)
        {
            SetBox(box, origin, size);
            _dragRoomBox = null;
            return;
        }
        var undoRedo = GetUndoRedo();
        if (box > 0)
        {
            // A room box drag is one undo step on the node, not on the settings (§13.5).
            var node = _dragRoomBox;
            _dragRoomBox = null;
            if (node == null || !IsInstanceValid(node))
                return;
            undoRedo.CreateAction("Resize Room Box", UndoRedo.MergeMode.Disable, node);
            undoRedo.AddDoProperty(node, GridMapBox.PropertyName.Origin, node.Origin);
            undoRedo.AddDoProperty(node, GridMapBox.PropertyName.Size, node.Size);
            undoRedo.AddUndoProperty(node, GridMapBox.PropertyName.Origin, origin);
            undoRedo.AddUndoProperty(node, GridMapBox.PropertyName.Size, size);
            undoRedo.CommitAction(false);
            return;
        }
        undoRedo.CreateAction("Resize Dungeon Bounds");
        undoRedo.AddDoMethod(_settings, DungeonSettings.MethodName.SetBounds, _settings.BoundsOrigin, _settings.BoundsSize);
        undoRedo.AddUndoMethod(_settings, DungeonSettings.MethodName.SetBounds, origin, size);
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

    /// <summary>
    /// A warning when WFC Generator older than 1.3.3 is installed (brief godoodungeon §4.1), else empty.
    /// Versions up to 1.3.4 live in addons/wfc_generator; from 1.4.0 on it is godoowave, which is never too old.
    /// </summary>
    private static string WfcGeneratorWarning()
    {
        if (!FileAccess.FileExists(WfcGeneratorConfig))
            return "";
        var config = new ConfigFile();
        if (config.Load(WfcGeneratorConfig) != Error.Ok)
            return "";
        string version = config.GetValue("plugin", "version", "").AsString();
        return WfcGeneratorVersion.NeedsUpdate(version)
            ? $"godoodungeon: WFC Generator {version} is installed. Replace it with godoowave, its new name from 1.4.0 on. "
              + "Older versions keep their own copy of the tile set classes, so tile sets load as types godoodungeon cannot use."
            : "";
    }
}
#endif
