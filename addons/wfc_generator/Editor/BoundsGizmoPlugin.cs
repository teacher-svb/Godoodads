#if TOOLS
using System.Collections.Generic;
using Godot;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Editor;

/// <summary>
/// Draws face handles for the boxes of the WFC dialog (bounds, then the pin box) on the GridMap
/// the dialog edits. Handle id = box × 6 + direction (see <see cref="Directions"/>). Dragging is
/// done by the plugin in _Forward3DGuiInput, because the GridMap editor takes mouse input before
/// gizmo handles see it. The outlines are drawn by the plugin's overlay.
/// </summary>
[Tool]
public partial class BoundsGizmoPlugin : EditorNode3DGizmoPlugin
{
    private const string HandleMaterial = "wfc_handles";

    public BoundsGizmoPlugin() => CreateHandleMaterial(HandleMaterial);

    /// <summary>The plugin that owns the boxes. Cleared when the plugin is removed.</summary>
    public WfcGeneratorPlugin Owner { get; set; }

    public override string _GetGizmoName() => "WFC Generator bounds";

    public override bool _HasGizmo(Node3D forNode3D) => forNode3D is GridMap;

    private GridMap ActiveMap(EditorNode3DGizmo gizmo) =>
        Owner != null && IsInstanceValid(Owner) && gizmo.GetNode3D() is GridMap map && Owner.HandleTarget == map ? map : null;

    public override void _Redraw(EditorNode3DGizmo gizmo)
    {
        gizmo.Clear();
        var map = ActiveMap(gizmo);
        if (map == null)
            return;

        var points = new List<Vector3>();
        var ids = new List<int>();
        for (int box = 0; box < Owner.BoxCount; box++)
        {
            if (!Owner.BoxVisible(box))
                continue;
            var positions = HandlePositions(map, Owner.BoxOrigin(box), Owner.BoxSize(box));
            for (int direction = 0; direction < Directions.Count; direction++)
            {
                points.Add(positions[direction]);
                ids.Add(box * Directions.Count + direction);
            }
        }
        if (points.Count > 0)
            gizmo.AddHandles(points.ToArray(), GetMaterial(HandleMaterial, gizmo), ids.ToArray());
    }

    /// <summary>The handle positions of a box in GridMap space, indexed by direction.</summary>
    public static Vector3[] HandlePositions(GridMap map, Vector3I origin, Vector3I size)
    {
        var (min, max) = Extents(map, origin, size);
        var points = new Vector3[Directions.Count];
        for (int direction = 0; direction < Directions.Count; direction++)
            points[direction] = FaceCentre(min, max, direction);
        return points;
    }

    /// <summary>
    /// The box after dragging the face in <paramref name="direction"/> to the mouse: the point on
    /// the face's axis closest to the mouse ray, snapped to the nearest boundary between cells. The
    /// opposite face stays where it is, and the box keeps at least one cell.
    /// </summary>
    public static (Vector3I Origin, Vector3I Size) DragFace(GridMap map, Camera3D camera, Vector2 screenPos,
        int direction, Vector3I origin, Vector3I size)
    {
        int axis = direction / 2;
        var (min, max) = Extents(map, origin, size);
        var toLocal = map.GlobalTransform.AffineInverse();
        var rayFrom = toLocal * camera.ProjectRayOrigin(screenPos);
        var rayDir = (toLocal.Basis * camera.ProjectRayNormal(screenPos)).Normalized();
        var axisFrom = FaceCentre(min, max, direction);
        var axisDir = Vector3.Zero;
        axisDir[axis] = 1;
        float b = axisDir.Dot(rayDir);
        float denominator = 1 - b * b;
        if (Mathf.Abs(denominator) < 1e-5f)
            return (origin, size);   // looking straight along the axis
        var w = axisFrom - rayFrom;
        float t = (b * rayDir.Dot(w) - axisDir.Dot(w)) / denominator;
        float value = axisFrom[axis] + t;

        float firstEdge = map.MapToLocal(Vector3I.Zero)[axis] - map.CellSize[axis] * 0.5f;
        int boundary = Mathf.RoundToInt((value - firstEdge) / map.CellSize[axis]);
        if (direction % 2 == 0)
        {
            size[axis] = Mathf.Max(1, boundary - origin[axis]);
        }
        else
        {
            int far = origin[axis] + size[axis];
            origin[axis] = Mathf.Min(boundary, far - 1);
            size[axis] = far - origin[axis];
        }
        return (origin, size);
    }

    /// <summary>The box's corners in GridMap space: the outer faces of its first and last cells.</summary>
    public static (Vector3 Min, Vector3 Max) Extents(GridMap map, Vector3I origin, Vector3I size)
    {
        var half = map.CellSize * 0.5f;
        return (map.MapToLocal(origin) - half, map.MapToLocal(origin + size - Vector3I.One) + half);
    }

    private static Vector3 FaceCentre(Vector3 min, Vector3 max, int direction)
    {
        var centre = (min + max) * 0.5f;
        int axis = direction / 2;
        centre[axis] = direction % 2 == 0 ? max[axis] : min[axis];
        return centre;
    }
}
#endif
