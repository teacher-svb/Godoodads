#if TOOLS
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Godoodads.Common;
using Godoodads.MultiMeshScatter.Adapters;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.MultiMeshScatter.Editor;

[Tool]
public partial class MultiMeshScatterPlugin : EditorPlugin
{
    private const string SettingsMeta = ScatterSettings.MetaKey;
    private const string PresetDirectory = "res://addons/multimesh_scatter/Presets";

    /// <summary>Menu item, dialog title and undo action name. GDDS = Godoodads, this suite.</summary>
    private const string ToolName = "Populate Surface (GDDS)";

    private SpatialMenuHook _menu;
    private ToolButtonRowInspectorPlugin _buttonRows;
    private DialogInspectorFilterPlugin _dialogFilter;
    private ToolDialog _dialog;
    private NodePickerRow _surfacePicker;
    private PresetRow _presets;
    private MultiMeshInstance3D _target;
    private ScatterSettings _settings = new();

    public override void _EnterTree()
    {
        _menu = new SpatialMenuHook(this, "MultiMeshInstance3D", "MultiMesh");
        _menu.AddItem(ToolName, new Callable(this, MethodName.OpenDialog));
        _menu.AppendToHostItems(" (Godot)"); // Godot's own "Populate Surface"
        if (!_menu.IsHooked)
            GD.PushWarning("MultiMesh Scatter: built-in MultiMesh menu not found, using a separate toolbar button.");

        _buttonRows = new ToolButtonRowInspectorPlugin(nameof(ScatterSettings));
        _buttonRows.AddRow(ScatterSettings.PropertyName.TargetUpButton, ScatterSettings.PropertyName.TargetDownButton,
            ScatterSettings.PropertyName.TargetRightButton, ScatterSettings.PropertyName.TargetBackButton);
        AddInspectorPlugin(_buttonRows);

        _dialogFilter = new DialogInspectorFilterPlugin();
        AddInspectorPlugin(_dialogFilter);

        _dialog = new ToolDialog(ToolName, "Populate", "multimesh_scatter_dialog_size") { Filter = _dialogFilter };
        _presets = new PresetRow(PresetDirectory);
        _presets.PresetLoaded += OnPresetLoaded;
        _presets.SaveRequested += OnPresetSaveRequested;
        _dialog.Header.AddChild(_presets);
        _surfacePicker = new NodePickerRow("Surface", "MeshInstance3D", "CSGShape3D", "GridMap");
        _surfacePicker.NodePicked += OnSurfacePicked;
        _dialog.Header.AddChild(_surfacePicker);
        _dialog.ActionPressed += OnPopulatePressed;
        EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);
    }

    public override void _ExitTree()
    {
        _menu?.Remove();
        _menu = null;
        if (_buttonRows != null)
        {
            RemoveInspectorPlugin(_buttonRows);
            _buttonRows = null;
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
        _target = null;
    }

    public override bool _Handles(GodotObject @object) => @object is MultiMeshInstance3D;

    public override void _Edit(GodotObject @object) => _target = @object as MultiMeshInstance3D;

    public override void _MakeVisible(bool visible) => _menu?.SetFallbackVisible(visible);

    private void OpenDialog()
    {
        if (_target == null)
            return;

        // Restore the settings of this node's last populate; otherwise keep the last-used ones.
        if (_target.HasMeta(SettingsMeta) && _target.GetMeta(SettingsMeta).As<ScatterSettings>() is { } stored)
            _settings = (ScatterSettings)stored.Duplicate();
        var surface = _settings.SurfacePath.IsEmpty ? null : _target.GetNodeOrNull(_settings.SurfacePath);
        if (surface == null && _surfacePicker.Value is { } previous && IsInstanceValid(previous) && previous.IsInsideTree())
            surface = previous;
        _surfacePicker.SetValue(surface);
        _settings.SurfaceIsGridMap = surface is GridMap;

        _dialog.Edit(_settings);
        _presets.Refresh();
        _dialog.SetActionEnabled(surface != null);
        _dialog.SetStatus($"Target: {_target.Name}");
        if (_target.Multimesh?.Mesh == null)
            _dialog.AddStatus("The MultiMesh has no mesh assigned; instances will be invisible until it has one.", ToolDialog.Severity.Warning);
        if (surface is Node3D surfaceNode && ReadSurface(surfaceNode, out _, out _, out _) is { } data)
        {
            BuildFilters(data, out var problem);
            if (problem == null)
                BuildNormalMap(data, out problem);
            if (problem != null)
                _dialog.AddStatus(problem, ToolDialog.Severity.Warning);
        }
        _dialog.PopupForEditing();
    }

    private void OnPopulatePressed()
    {
        _dialog.ClearStatus();
        if (_target == null || !IsInstanceValid(_target) || !_target.IsInsideTree())
        {
            Notify.Report(_dialog, "The target MultiMeshInstance3D is no longer in the scene.", ToolDialog.Severity.Error);
            return;
        }
        if (_surfacePicker.Value is not Node3D surfaceNode || !IsInstanceValid(surfaceNode))
        {
            Notify.Report(_dialog, "Pick a surface node first.", ToolDialog.Severity.Warning);
            return;
        }

        var surface = ReadSurface(surfaceNode, out var error, out bool generatedTangents, out var gridStats);
        if (surface == null)
        {
            Notify.Report(_dialog, error, ToolDialog.Severity.Error);
            ReportUnmatchedItems(gridStats);
            return;
        }
        var filters = BuildFilters(surface, out error);
        var normalMap = error == null ? BuildNormalMap(surface, out error) : null;
        if (error != null)
        {
            Notify.Report(_dialog, error, ToolDialog.Severity.Error);
            return;
        }

        if (_settings.SlopeMinDegrees > _settings.SlopeMaxDegrees)
        {
            Notify.Report(_dialog, "Slope Min Degrees is larger than Slope Max Degrees, so no surface qualifies.", ToolDialog.Severity.Error);
            return;
        }

        bool append = _settings.Mode == OutputMode.Append;
        var options = _settings.ToOptions();
        options.NormalMap = normalMap;
        int existing = append ? _target.Multimesh?.InstanceCount ?? 0 : 0;
        options.SeedSalt = (ulong)existing;
        var clock = Stopwatch.StartNew();
        var result = ScatterPipeline.Run(surface, options, filters, FixedPoints(surfaceNode, append, options.MinDistance > 0));

        var toLocal = _target.GlobalTransform.AffineInverse();
        var local = result.Transforms.Select(t => toLocal * t).ToArray();
        var multimesh = MultiMeshWriter.Build(_target.Multimesh ?? new MultiMesh(), local, append);
        clock.Stop();

        var stored = (ScatterSettings)_settings.Duplicate();
        stored.SurfacePath = _target.GetPathTo(surfaceNode);
        _settings.SurfacePath = stored.SurfacePath;
        UndoHelpers.SetProperties(GetUndoRedo(), ToolName, _target,
            (MultiMeshInstance3D.PropertyName.Multimesh, multimesh),
            ("metadata/" + SettingsMeta, stored));

        ReportResult(result, existing, clock.ElapsedMilliseconds);
        if (gridStats is { } cells)
            _dialog.AddStatus($"Read {cells.Cells:N0} GridMap cells; left out {cells.CoveredTriangles:N0} triangles covered by neighbouring cells.");
        ReportUnmatchedItems(gridStats);
        if (_settings.NormalSource != NormalSource.Face && !surface.HasNormals)
            _dialog.AddStatus("The surface has no vertex normals; face normals were used instead.", ToolDialog.Severity.Warning);
        if (generatedTangents)
            _dialog.AddStatus("The surface has no tangents, so they were generated on a temporary copy for the normal map. Your mesh is unchanged.", ToolDialog.Severity.Info);
    }

    private void OnSurfacePicked(Node node)
    {
        _settings.SurfaceIsGridMap = node is GridMap;
        _dialog.SetActionEnabled(node != null);
    }

    /// <summary>One warning per GridMap item filter entry that matches no MeshLibrary item, so typos show.</summary>
    private void ReportUnmatchedItems(GridMapSurfaceStats? gridStats)
    {
        if (gridStats?.UnmatchedItems is not { } unmatched)
            return;
        foreach (var entry in unmatched)
            _dialog.AddStatus($"GridMap item '{entry}' matches no item.", ToolDialog.Severity.Warning);
    }

    /// <summary>The surface's triangles; for a GridMap with the dialog's item filter and box, and its counts.</summary>
    private SurfaceData ReadSurface(Node3D node, out string error, out bool generatedTangents, out GridMapSurfaceStats? gridStats)
    {
        gridStats = null;
        if (node is not GridMap map)
            return SurfaceSource.FromNode(node, UsesTangentSpaceMap, out error, out generatedTangents);
        var data = SurfaceSource.FromGridMap(map, _settings.ToGridMapSelection(), UsesTangentSpaceMap, out error, out generatedTangents, out var stats);
        gridStats = stats;
        return data;
    }

    /// <summary>Copies a preset's values into the dialog, keeping the chosen surface.</summary>
    private void OnPresetLoaded(Resource preset)
    {
        if (preset is not ScatterSettings settings)
        {
            _dialog.SetStatus($"'{preset.ResourcePath}' is not a ScatterSettings preset.", ToolDialog.Severity.Error);
            return;
        }
        var surfacePath = _settings.SurfacePath;
        _settings = (ScatterSettings)settings.Duplicate();
        _settings.SurfacePath = surfacePath;
        _settings.SurfaceIsGridMap = _surfacePicker.Value is GridMap;
        _dialog.Edit(_settings);
        string name = string.IsNullOrEmpty(settings.ResourceName) ? settings.ResourcePath.GetFile() : settings.ResourceName;
        _dialog.SetStatus($"Loaded preset '{name}'.", ToolDialog.Severity.Info);
    }

    private void OnPresetSaveRequested(string path)
    {
        var preset = (ScatterSettings)_settings.Duplicate();
        preset.SurfacePath = new NodePath();
        if (string.IsNullOrEmpty(preset.ResourceName))
            preset.ResourceName = path.GetFile().GetBaseName().Capitalize();
        var error = ResourceSaver.Save(preset, path);
        if (error != Error.Ok)
        {
            Notify.Report(_dialog, $"Could not save the preset to {path}: {error}.", ToolDialog.Severity.Error);
            return;
        }
        EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(path);
        _presets.Refresh();
        _dialog.SetStatus($"Saved preset to {path}.", ToolDialog.Severity.Success);
    }

    private bool UsesTangentSpaceMap =>
        _settings.NormalSource == NormalSource.NormalMap && _settings.NormalMapSpace == NormalMapSpace.Tangent;

    /// <summary>Normal map stage for the current settings; null when the normal source is not Normal Map.</summary>
    private NormalMapOptions BuildNormalMap(SurfaceData surface, out string error)
    {
        error = null;
        if (_settings.NormalSource != NormalSource.NormalMap)
            return null;
        if (_settings.NormalMapTexture == null)
        {
            error = "Normal Source is Normal Map, but no normal map texture is set.";
            return null;
        }
        bool secondSet = _settings.NormalMapUvSet == UvSet.Uv2;
        if (secondSet && _settings.NormalMapSpace == NormalMapSpace.Tangent)
        {
            error = "Tangent-space normal maps follow the mesh tangents, which are built from UV1. Use UV1, or an object-space map.";
            return null;
        }
        if (secondSet ? !surface.HasUv2 : !surface.HasUv)
        {
            error = $"A normal map is set, but the surface has no {(secondSet ? "UV2" : "UV1")} coordinates.";
            return null;
        }
        var map = NormalMapSource.FromTexture(_settings.NormalMapTexture, _settings.NormalMapSpace, _settings.NormalMapFlipGreen, out error);
        return map == null ? null : new NormalMapOptions
        {
            Map = map,
            Space = _settings.NormalMapSpace,
            Strength = _settings.NormalMapStrength,
            SecondUvSet = secondSet,
        };
    }

    /// <summary>Candidate filters for the current settings, in pipeline order (brief §4.6).</summary>
    private List<ICandidateFilter> BuildFilters(SurfaceData surface, out string error)
    {
        error = null;
        var filters = new List<ICandidateFilter>();
        if (_settings.MaskTexture == null)
            return filters;

        bool secondSet = _settings.MaskUvSet == UvSet.Uv2;
        bool topDown = _settings.MaskProjection == MaskProjection.TopDown;
        if (!topDown && (secondSet ? !surface.HasUv2 : !surface.HasUv))
        {
            error = $"A mask texture is set, but the surface has no {(secondSet ? "UV2" : "UV1")} coordinates.";
            return filters;
        }
        var mask = MaskSource.FromTexture(_settings.MaskTexture, _settings.MaskChannel, out error);
        if (mask == null)
            return filters;
        filters.Add(new MaskFilter(mask, _settings.MaskMode, _settings.MaskThreshold, _settings.MaskInvert, secondSet,
            topDown ? TopDownArea(surface) : null));
        return filters;
    }

    /// <summary>
    /// The area a top-down mask covers: the GridMap's cell box when one is set, so the mask lines
    /// up with the box, otherwise the surface seen from above.
    /// </summary>
    private Rect2 TopDownArea(SurfaceData surface)
    {
        if (_surfacePicker.Value is GridMap map && _settings.ToGridMapSelection() is { HasBox: true } box)
        {
            var size = map.CellSize;
            var low = new Vector3(box.BoxOrigin.X * size.X, box.BoxOrigin.Y * size.Y, box.BoxOrigin.Z * size.Z);
            var high = low + new Vector3(box.BoxSize.X * size.X, box.BoxSize.Y * size.Y, box.BoxSize.Z * size.Z);
            var corners = new List<Vector3>();
            for (int i = 0; i < 8; i++)
                corners.Add(map.GlobalTransform * new Vector3((i & 1) != 0 ? high.X : low.X, (i & 2) != 0 ? high.Y : low.Y, (i & 4) != 0 ? high.Z : low.Z));
            return MaskFilter.TopDownArea(corners);
        }
        return MaskFilter.TopDownArea(surface.Positions);
    }

    /// <summary>Positions that new instances keep their distance from.</summary>
    private List<Vector3> FixedPoints(Node surfaceNode, bool append, bool spacing)
    {
        var points = new List<Vector3>();
        if (!spacing)
            return points;
        if (append)
            points.AddRange(MultiMeshWriter.WorldPositions(_target));
        if (_settings.AvoidSiblings && _target.GetParent() is { } parent)
        {
            foreach (var child in parent.GetChildren())
            {
                if (child == _target || child == surfaceNode)
                    continue;
                if (child is MultiMeshInstance3D other)
                    points.AddRange(MultiMeshWriter.WorldPositions(other));
                else if (child is MeshInstance3D mesh)
                    points.Add(mesh.GlobalPosition);
            }
        }
        return points;
    }

    private void ReportResult(ScatterResult result, int appendedTo, long milliseconds)
    {
        _dialog.ClearStatus();
        foreach (var line in ScatterReport.Describe(result, _settings.Seed, appendedTo, milliseconds))
        {
            _dialog.AddStatus(line.Text, line.Kind switch
            {
                ScatterReport.Kind.Success => ToolDialog.Severity.Success,
                ScatterReport.Kind.Warning => ToolDialog.Severity.Warning,
                _ => ToolDialog.Severity.Info,
            });
        }
    }
}
#endif
