#if TOOLS
using System.Globalization;
using System.Linq;
using Godot;
using Godoodads.Common;
using Godoodads.Curves.Adapters;
using Godoodads.Curves.Core;

namespace Godoodads.Curves.Editor;

/// <summary>
/// godoocurves' editor entry (brief godoocurves §5): "Generate Curve (GDDS)" for a selected Path3D
/// opens a dialog on its <see cref="CurveSettings"/>. Generate replaces the path's curve or appends
/// the shape at its end, as one undo step, and a preview shows the shape while the dialog is open.
/// </summary>
[Tool]
public partial class GodoocurvesPlugin : EditorPlugin
{
    /// <summary>Menu item, dialog title and undo action name. GDDS = Godoodads, this suite.</summary>
    private const string ToolName = "Generate Curve (GDDS)";

    private const int PreviewSamplesPerSegment = 24;
    private static readonly Color PreviewColour = new(1f, 0.3f, 0.75f);
    private static readonly Color UpColour = new(1f, 0.85f, 0.15f);

    private SpatialMenuHook _menu;
    private DialogInspectorFilterPlugin _dialogFilter;
    private ToolDialog _dialog;
    /// <summary>The selected Path3D; follows the editor selection.</summary>
    private Path3D _selected;
    /// <summary>The Path3D the open dialog generates on, fixed when it opens. The dialog is not modal.</summary>
    private Path3D _target;
    private CurveSettings _settings = new();

    private MeshInstance3D _overlay;
    private string _overlayKey;
    /// <summary>The settings problem the preview showed last, so it is reported once, not every frame.</summary>
    private string _previewProblem;

    public override void _EnterTree()
    {
        _menu = new SpatialMenuHook(this, "Path3D", "Path3D");
        _menu.AddItem(ToolName, new Callable(this, MethodName.OpenDialog));
        if (OS.IsStdOutVerbose())
            GD.Print($"godoocurves: {(_menu.IsHooked ? "added to a built-in Path3D menu" : "using its own Path3D toolbar button")}.");

        _dialogFilter = new DialogInspectorFilterPlugin();
        AddInspectorPlugin(_dialogFilter);

        _dialog = new ToolDialog(ToolName, "Generate", "godoocurves_dialog_size") { Filter = _dialogFilter };
        _dialog.ActionPressed += OnGeneratePressed;
        EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);
    }

    public override void _ExitTree()
    {
        RemoveOverlay();
        _menu?.Remove();
        _menu = null;
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

    public override bool _Handles(GodotObject @object) => @object is Path3D;

    public override void _Edit(GodotObject @object) => _selected = @object as Path3D;

    public override void _MakeVisible(bool visible) => _menu?.SetFallbackVisible(visible);

    public override void _Process(double delta)
    {
        if (_dialog == null)
            return;
        if (_dialog.Visible && TargetIsAlive)
            UpdatePreview();
        else
            RemoveOverlay();
    }

    private bool TargetIsAlive => _target != null && IsInstanceValid(_target) && _target.IsInsideTree();

    private void OpenDialog()
    {
        if (_selected == null || !IsInstanceValid(_selected))
            return;
        RemoveOverlay();
        _target = _selected;
        if (CurveSettings.StoredOn(_target) is { } stored)
            _settings = (CurveSettings)stored.Duplicate();
        _previewProblem = null;
        _dialog.Edit(_settings);
        _dialog.SetStatus($"Target: {_target.Name}. The preview shows where Generate puts the shape.");
        _dialog.PopupForEditing();
    }

    /// <summary>
    /// The shape for the current settings, in the path's local space, placed by Start, Direction and
    /// Up (Replace) or at the end of the target's curve (Append). Null with a reason when it cannot be made.
    /// </summary>
    private ShapeCurve PlaceShape(out ShapeCurve local, out string error, out string note)
    {
        local = null;
        note = null;
        error = _settings.Check();
        if (error != null)
            return null;
        var result = Shapes.Build(_settings.Shape, _settings.ToParameters());
        if (!result.IsValid)
        {
            error = result.Error;
            return null;
        }
        local = result.Curve;
        if (_settings.Mode == CurveMode.Replace)
        {
            _settings.TryFrame(out var frame, out _);
            return local.ToPath(frame);
        }
        if (_target.Curve is { Closed: true })
        {
            error = "The path's curve is closed, so nothing can be appended to its end. Use Replace.";
            return null;
        }
        return local.ToPath(CurveWriter.AppendFrame(_target.Curve, out note));
    }

    private void OnGeneratePressed()
    {
        _dialog.ClearStatus();
        if (!TargetIsAlive)
        {
            Notify.Report(_dialog, "The target Path3D is no longer in the scene.", ToolDialog.Severity.Error);
            return;
        }
        var shape = PlaceShape(out var local, out string error, out _);
        if (shape == null)
        {
            Notify.Report(_dialog, error, ToolDialog.Severity.Error);
            return;
        }

        var previous = _target.Curve;
        Curve3D curve;
        string note = null;
        if (_settings.Mode == CurveMode.Replace)
            curve = CurveWriter.Create(shape);
        else
        {
            var written = CurveWriter.Append(previous, local);
            if (!written.IsValid)
            {
                Notify.Report(_dialog, written.Error, ToolDialog.Severity.Error);
                return;
            }
            curve = written.Curve;
            note = written.Note;
        }

        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction(ToolName, UndoRedo.MergeMode.Disable, _target);
        undoRedo.AddDoProperty(_target, Path3D.PropertyName.Curve, curve);
        undoRedo.AddUndoProperty(_target, Path3D.PropertyName.Curve, previous);
        undoRedo.AddDoMethod(this, MethodName.ApplySettings, _target, (CurveSettings)_settings.Duplicate());
        undoRedo.AddUndoMethod(this, MethodName.ApplySettings, _target, CurveSettings.StoredOn(_target));
        undoRedo.CommitAction();

        string label = CurveSettings.ShapeLabels[(int)_settings.Shape];
        string length = Metres(local.Length);
        _dialog.SetStatus(_settings.Mode == CurveMode.Replace
            ? $"Generated {label}: {local.Points.Count} points, {length}."
            : $"Appended {label}: {local.Points.Count - 1} new points, {length}. The curve is now {Metres(curve.GetBakedLength())}.",
            ToolDialog.Severity.Success);
        if (note != null)
            _dialog.AddStatus(note);
        if (previous != null && !string.IsNullOrEmpty(previous.ResourcePath))
            _dialog.AddStatus($"The path's curve was saved as {previous.ResourcePath}; the path now has its own copy, and that file is unchanged.");
    }

    private static string Metres(double length) => string.Format(CultureInfo.InvariantCulture, "{0:F1} m", length);

    /// <summary>Do and undo target for the settings stored on the Path3D. Null removes them.</summary>
    public void ApplySettings(Path3D path, CurveSettings settings)
    {
        if (IsInstanceValid(path))
            CurveSettings.StoreOn(path, settings);
    }

    // ---- Preview (brief §5) ------------------------------------------------------------------

    /// <summary>Redraws the preview when the settings or the target's curve changed.</summary>
    private void UpdatePreview()
    {
        string key = PreviewKey();
        if (key == _overlayKey && _overlay != null && IsInstanceValid(_overlay))
            return;
        _overlayKey = key;

        var shape = PlaceShape(out var local, out string error, out _);
        if (shape == null)
        {
            if (error != _previewProblem)
                _dialog.SetStatus(error, ToolDialog.Severity.Warning);
            _previewProblem = error;
            DrawPreview(null, 0);
            return;
        }
        if (_previewProblem != null)
            _dialog.SetStatus($"Target: {_target.Name}. The preview shows where Generate puts the shape.");
        _previewProblem = null;
        DrawPreview(shape, Shapes.Size(_settings.Shape, _settings.ToParameters()));
    }

    /// <summary>Everything the preview depends on: every stored setting, and the end of the target's curve.</summary>
    private string PreviewKey()
    {
        var values = _settings.GetPropertyList()
            .Where(p => ((PropertyUsageFlags)p["usage"].AsInt64()).HasFlag(PropertyUsageFlags.Storage))
            .Select(p => _settings.Get(p["name"].AsString()).ToString());
        var curve = _target.Curve;
        string end = curve == null || curve.PointCount == 0 ? "empty"
            : $"{curve.GetInstanceId()}|{curve.PointCount}|{curve.Closed}|{curve.GetPointPosition(curve.PointCount - 1)}|{curve.GetPointIn(curve.PointCount - 1)}|{curve.GetPointTilt(curve.PointCount - 1)}";
        return string.Join("|", values) + "|" + end;
    }

    /// <summary>Draws the shape's curve and a tick for the wanted up at each point; null clears it.</summary>
    private void DrawPreview(ShapeCurve shape, float size)
    {
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
            // No owner, so the preview is never saved with the scene.
            _target.AddChild(_overlay);
        }
        var mesh = (ImmediateMesh)_overlay.Mesh;
        mesh.ClearSurfaces();
        if (shape == null || shape.SegmentCount == 0)
            return;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        for (int s = 0; s < shape.SegmentCount; s++)
        {
            var previous = shape.Sample(s, 0);
            for (int i = 1; i <= PreviewSamplesPerSegment; i++)
            {
                var next = shape.Sample(s, i / (float)PreviewSamplesPerSegment);
                Line(mesh, previous, next, PreviewColour);
                previous = next;
            }
        }
        float tick = Mathf.Max(0.3f, size * 0.06f);
        foreach (var point in shape.Points)
            Line(mesh, point.Position, point.Position + point.Up * tick, UpColour);
        mesh.SurfaceEnd();
    }

    private static void Line(ImmediateMesh mesh, Vector3 from, Vector3 to, Color colour)
    {
        mesh.SurfaceSetColor(colour);
        mesh.SurfaceAddVertex(from);
        mesh.SurfaceSetColor(colour);
        mesh.SurfaceAddVertex(to);
    }

    private void RemoveOverlay()
    {
        if (_overlay != null && IsInstanceValid(_overlay))
            _overlay.QueueFree();
        _overlay = null;
        _overlayKey = null;
    }
}
#endif
