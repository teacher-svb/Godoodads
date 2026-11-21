using Godot;

namespace Godoodads.Common;

/// <summary>
/// A box of GridMap cells kept as a child node of the GridMap (godoowave pin boxes, godoodungeon
/// room boxes; WFC brief §16.1). <see cref="Origin"/> and <see cref="Size"/> are in cells, like a
/// tool's bounds. The node sits at the centre of the box's floor (centred on X and Z, on the bottom
/// face on Y): setting Origin or Size moves it, and moving it (move tool, undo of a move) snaps
/// Origin to the nearest cell and keeps Size. Rotation and scale are reset.
/// Not a global class, so it is not offered in Add Child Node; subclasses are.
/// </summary>
[Tool]
public partial class GridMapBox : Node3D
{
    private Vector3I _origin;
    private Vector3I _size = new(4, 1, 4);
    private bool _enabled = true;
    private bool _syncing;

    /// <summary>The box's first cell, in the parent GridMap's cells.</summary>
    [Export]
    public Vector3I Origin
    {
        get => _origin;
        set
        {
            _origin = value;
            PlaceAtOrigin();
            UpdateGizmos();
        }
    }

    /// <summary>The box's extent in cells. A box with a zero or negative side does nothing.</summary>
    [Export]
    public Vector3I Size
    {
        get => _size;
        set
        {
            _size = value;
            PlaceAtOrigin();
            UpdateGizmos();
        }
    }

    /// <summary>A disabled box does nothing and is drawn faded.</summary>
    [Export]
    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            UpdateGizmos();
        }
    }

    /// <summary>The colour the shared gizmo draws this box in. Subclasses choose their own.</summary>
    public virtual Color BoxColour => new(1, 1, 1);

    /// <summary>The parent GridMap, or null when the box is not a direct child of one.</summary>
    public GridMap Map => GetParent() as GridMap;

    /// <summary>True when the box is enabled, has cells and sits directly under a GridMap.</summary>
    public bool IsActive => Enabled && Size.X > 0 && Size.Y > 0 && Size.Z > 0 && Map != null;

    /// <summary>
    /// The active boxes of type <typeparamref name="T"/> under a GridMap: direct children only,
    /// enabled and with cells, in child order. Nested boxes and disabled ones do nothing.
    /// </summary>
    public static System.Collections.Generic.List<T> ActiveChildren<T>(GridMap map) where T : GridMapBox
    {
        var boxes = new System.Collections.Generic.List<T>();
        if (map == null)
            return boxes;
        foreach (var child in map.GetChildren())
            if (child is T box && box.IsActive)
                boxes.Add(box);
        return boxes;
    }

    /// <summary>True when the cell lies inside the box.</summary>
    public bool HasCell(Vector3I cell) =>
        cell.X >= Origin.X && cell.Y >= Origin.Y && cell.Z >= Origin.Z
        && cell.X < Origin.X + Size.X && cell.Y < Origin.Y + Size.Y && cell.Z < Origin.Z + Size.Z;

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationParented:
                PlaceAtOrigin();
                UpdateConfigurationWarnings();
                break;
            case NotificationUnparented:
                UpdateConfigurationWarnings();
                break;
            case NotificationEnterTree:
                SetNotifyLocalTransform(true);
                PlaceAtOrigin();
                break;
            case NotificationLocalTransformChanged:
                FollowTransform();
                break;
        }
    }

    public override string[] _GetConfigurationWarnings() =>
        Map == null ? new[] { "This box only works as a direct child of a GridMap." } : System.Array.Empty<string>();

    /// <summary>The local position of a cell's minimum corner in <paramref name="map"/>.</summary>
    public static Vector3 CornerOf(GridMap map, Vector3I cell) => map.MapToLocal(cell) - map.CellSize * 0.5f;

    /// <summary>The cell whose minimum corner is nearest to a local position in <paramref name="map"/>.</summary>
    public static Vector3I NearestCell(GridMap map, Vector3 position)
    {
        var first = CornerOf(map, Vector3I.Zero);
        var cell = map.CellSize;
        return new Vector3I(
            Mathf.RoundToInt((position.X - first.X) / cell.X),
            Mathf.RoundToInt((position.Y - first.Y) / cell.Y),
            Mathf.RoundToInt((position.Z - first.Z) / cell.Z));
    }

    /// <summary>
    /// The local position in <paramref name="map"/> where a box's node sits: the centre of its floor,
    /// half its extent from Origin's corner on X and Z. Sides below 1 count as 1, as in the gizmo.
    /// </summary>
    public static Vector3 PivotOf(GridMap map, Vector3I origin, Vector3I size) => CornerOf(map, origin) + PivotOffset(map, size);

    /// <summary>From a box's minimum corner to its pivot: half the box's X and Z extent.</summary>
    private static Vector3 PivotOffset(GridMap map, Vector3I size) =>
        new(Mathf.Max(size.X, 1) * map.CellSize.X * 0.5f, 0, Mathf.Max(size.Z, 1) * map.CellSize.Z * 0.5f);

    /// <summary>Moves the node to the box's pivot, with no rotation or scale.</summary>
    private void PlaceAtOrigin()
    {
        if (Map is not { } map)
            return;
        var target = new Transform3D(Basis.Identity, PivotOf(map, _origin, _size));
        if (Transform.IsEqualApprox(target))
            return;
        _syncing = true;
        Transform = target;
        _syncing = false;
    }

    /// <summary>After the node was moved: snaps Origin to the nearest cell and puts the node there.</summary>
    private void FollowTransform()
    {
        if (_syncing || Map is not { } map)
            return;
        var cell = NearestCell(map, Position - PivotOffset(map, _size));
        if (cell != _origin)
        {
            _origin = cell;
            UpdateGizmos();
            NotifyPropertyListChanged();
        }
        PlaceAtOrigin();
    }
}
