#if TOOLS
using Godot;
using Godoodads.Common.Wfc.Core;
using Godoodads.Common.Wfc.Editor;

namespace Godoodads.Common.Editor;

/// <summary>
/// Draws <see cref="GridMapBox"/> nodes and lets you drag their faces (WFC brief §16.3). Every box
/// the plugin handles shows its outline in its <see cref="GridMapBox.BoxColour"/>; the selected one
/// also shows six face handles, snapped to cells, and a drag is one undo step on the node. Handles
/// work through Godot's own handle API because the GridMap editor does not take input while a
/// child is selected. Subclass it per addon to choose the boxes and the gizmo menu name.
/// </summary>
[Tool]
public partial class GridMapBoxGizmoPlugin : EditorNode3DGizmoPlugin
{
    private const string HandleMaterial = "box_handles";

    // Line materials by colour. After an assembly reload this is empty again and the materials are
    // created anew under the same names, which replaces them.
    private readonly System.Collections.Generic.HashSet<string> _materials = new();

    public GridMapBoxGizmoPlugin() => CreateHandleMaterial(HandleMaterial);

    /// <summary>True for the boxes this plugin draws. Subclasses choose their box type.</summary>
    protected virtual bool Handles(GridMapBox box) => false;

    public override bool _HasGizmo(Node3D forNode3D) => forNode3D is GridMapBox box && Handles(box);

    public override void _Redraw(EditorNode3DGizmo gizmo)
    {
        gizmo.Clear();
        if (gizmo.GetNode3D() is not GridMapBox box || box.Map is not { } map)
            return;
        var colour = box.BoxColour;
        if (!box.Enabled)
            colour.A *= 0.35f;
        var (min, max) = Corners(map, box);
        gizmo.AddLines(Edges(min, max), LineMaterial(colour, gizmo));

        var handles = new Vector3[Directions.Count];
        var ids = new int[Directions.Count];
        for (int direction = 0; direction < Directions.Count; direction++)
        {
            handles[direction] = FaceCentre(min, max, direction);
            ids[direction] = direction;
        }
        gizmo.AddHandles(handles, GetMaterial(HandleMaterial, gizmo), ids);
    }

    public override string _GetHandleName(EditorNode3DGizmo gizmo, int handleId, bool secondary) =>
        handleId switch
        {
            Directions.PosX => "+X", Directions.NegX => "−X",
            Directions.PosY => "+Y", Directions.NegY => "−Y",
            Directions.PosZ => "+Z", _ => "−Z",
        };

    /// <summary>The box before the drag, as [Origin, Size], for the undo step.</summary>
    public override Variant _GetHandleValue(EditorNode3DGizmo gizmo, int handleId, bool secondary) =>
        gizmo.GetNode3D() is GridMapBox box ? new Godot.Collections.Array { box.Origin, box.Size } : default;

    public override void _SetHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Camera3D camera, Vector2 screenPos)
    {
        if (gizmo.GetNode3D() is not GridMapBox box || box.Map is not { } map)
            return;
        var (origin, size) = BoundsGizmoPlugin.DragFace(map, camera, screenPos, handleId, box.Origin, Positive(box.Size));
        box.Origin = origin;
        box.Size = size;
    }

    public override void _CommitHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Variant restore, bool cancel)
    {
        if (gizmo.GetNode3D() is not GridMapBox box)
            return;
        var before = restore.AsGodotArray();
        var oldOrigin = before[0].AsVector3I();
        var oldSize = before[1].AsVector3I();
        if (cancel)
        {
            box.Origin = oldOrigin;
            box.Size = oldSize;
            return;
        }
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        undoRedo.CreateAction("Resize Box", UndoRedo.MergeMode.Disable, box);
        undoRedo.AddDoProperty(box, GridMapBox.PropertyName.Origin, box.Origin);
        undoRedo.AddDoProperty(box, GridMapBox.PropertyName.Size, box.Size);
        undoRedo.AddUndoProperty(box, GridMapBox.PropertyName.Origin, oldOrigin);
        undoRedo.AddUndoProperty(box, GridMapBox.PropertyName.Size, oldSize);
        undoRedo.CommitAction(false);
    }

    /// <summary>The box's corners in the box node's own space (the node sits at the centre of the box's floor).</summary>
    public static (Vector3 Min, Vector3 Max) Corners(GridMap map, GridMapBox box)
    {
        var (min, max) = BoundsGizmoPlugin.Extents(map, box.Origin, Positive(box.Size));
        var pivot = GridMapBox.PivotOf(map, box.Origin, box.Size);
        return (min - pivot, max - pivot);
    }

    private static Vector3I Positive(Vector3I size) => new(Mathf.Max(size.X, 1), Mathf.Max(size.Y, 1), Mathf.Max(size.Z, 1));

    private StandardMaterial3D LineMaterial(Color colour, EditorNode3DGizmo gizmo)
    {
        string name = "box_" + colour.ToHtml();
        if (_materials.Add(name))
            CreateMaterial(name, colour, false, true);
        return GetMaterial(name, gizmo);
    }

    private static Vector3[] Edges(Vector3 a, Vector3 b)
    {
        Vector3 P(int x, int y, int z) => new(x == 0 ? a.X : b.X, y == 0 ? a.Y : b.Y, z == 0 ? a.Z : b.Z);
        var lines = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            {
                lines.Add(P(0, i, j)); lines.Add(P(1, i, j));
                lines.Add(P(i, 0, j)); lines.Add(P(i, 1, j));
                lines.Add(P(i, j, 0)); lines.Add(P(i, j, 1));
            }
        return lines.ToArray();
    }

    public static Vector3 FaceCentre(Vector3 min, Vector3 max, int direction)
    {
        var centre = (min + max) * 0.5f;
        int axis = direction / 2;
        centre[axis] = direction % 2 == 0 ? max[axis] : min[axis];
        return centre;
    }
}
#endif
