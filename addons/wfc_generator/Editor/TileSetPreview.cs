#if TOOLS
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godoodads.WfcGenerator.Adapters;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Editor;

/// <summary>
/// Shows one tile in 3D with its socket labels, the tiles allowed next to each face, and the tile
/// set check. Compiled data is rebuilt whenever it is missing, which also covers assembly reloads.
/// </summary>
[Tool]
public partial class TileSetPreview : VBoxContainer
{
    private const double RefreshInterval = 0.5;
    private const float LabelDistance = 1.75f;
    private const float OrbitSpeed = 0.01f;

    private WfcTileSet _tileSet;
    private OptionButton _tilePicker;
    private OptionButton _rotationPicker;
    private SubViewportContainer _viewportContainer;
    private Node3D _pivot;
    private MeshInstance3D _mesh;
    private Godot.Collections.Array<Label3D> _labels = new();
    private RichTextLabel _neighbours;
    private RichTextLabel _problems;
    private Godot.Collections.Array<int> _tileForOption = new();
    private string _signature = "";
    private double _sinceRefresh;

    private TileSetBinding _binding;

    public TileSetPreview() { }

    public TileSetPreview(WfcTileSet tileSet)
    {
        _tileSet = tileSet;
        CustomMinimumSize = new Vector2(0, 460);
        AddThemeConstantOverride("separation", 4);

        var pickers = new HBoxContainer();
        AddChild(pickers);
        _tilePicker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false, ClipText = true };
        _tilePicker.Connect(OptionButton.SignalName.ItemSelected, new Callable(this, MethodName.OnTileSelected));
        pickers.AddChild(_tilePicker);
        _rotationPicker = new OptionButton();
        _rotationPicker.Connect(OptionButton.SignalName.ItemSelected, new Callable(this, MethodName.OnRotationSelected));
        pickers.AddChild(_rotationPicker);

        BuildViewport();

        AddChild(new Label { Text = "Valid neighbours" });
        _neighbours = NewText();
        AddChild(_neighbours);
        AddChild(new Label { Text = "Tile set check" });
        _problems = NewText();
        AddChild(_problems);

        Rebuild();
    }

    private static RichTextLabel NewText() => new()
    {
        FitContent = true,
        ScrollActive = false,
        SelectionEnabled = true,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
    };

    private void BuildViewport()
    {
        _viewportContainer = new SubViewportContainer { Stretch = true, CustomMinimumSize = new Vector2(0, 260), MouseFilter = MouseFilterEnum.Stop };
        _viewportContainer.Connect(Control.SignalName.GuiInput, new Callable(this, MethodName.OnViewportInput));
        AddChild(_viewportContainer);

        var viewport = new SubViewport { OwnWorld3D = true, RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible };
        _viewportContainer.AddChild(viewport);

        viewport.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.16f, 0.17f, 0.2f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = Colors.White,
                AmbientLightEnergy = 0.55f,
            },
        });
        viewport.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50, -30, 0) });

        _mesh = new MeshInstance3D();
        viewport.AddChild(_mesh);

        _pivot = new Node3D { RotationDegrees = new Vector3(-28, 35, 0) };
        viewport.AddChild(_pivot);
        _pivot.AddChild(new Camera3D { Position = new Vector3(0, 0, 7), Fov = 45, Current = true });

        foreach (int direction in Directions.All3D)
        {
            var label = new Label3D
            {
                Position = (Vector3)Directions.Offset(direction) * LabelDistance,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                NoDepthTest = true,
                FontSize = 30,
                OutlineSize = 8,
                PixelSize = 0.01f,
            };
            viewport.AddChild(label);
            _labels.Add(label);
        }
    }

    public override void _Process(double delta)
    {
        if (_tileSet == null || !IsVisibleInTree())
            return;
        _sinceRefresh += delta;
        if (_sinceRefresh < RefreshInterval)
            return;
        _sinceRefresh = 0;
        if (_binding == null || Signature() != _signature)
            Rebuild();
    }

    private string Signature()
    {
        var text = new StringBuilder();
        text.Append(_tileSet.Library?.GetInstanceId() ?? 0);
        foreach (var tile in _tileSet.Tiles)
            if (tile != null)
                text.Append('|').Append(tile.Item).Append(tile.Rotation).Append(string.Join(",", tile.ToDefinition().Sockets)).Append(tile.ResourceName);
        return text.ToString();
    }

    private void Rebuild()
    {
        _signature = Signature();
        _binding = TileSetBinding.Create(_tileSet, Directions.All3D);
        FillTilePicker();
        ShowProblems();
        ShowSelected();
    }

    /// <summary>Repeated tiles (such as copies of an empty tile) appear once.</summary>
    private void FillTilePicker()
    {
        int previous = _tilePicker.Selected >= 0 && _tilePicker.Selected < _tileForOption.Count ? _tileForOption[_tilePicker.Selected] : 0;
        _tilePicker.Clear();
        _tileForOption.Clear();

        var seen = new Dictionary<string, int>();
        int index = 0;
        foreach (var tile in _tileSet.Tiles)
        {
            if (tile != null)
            {
                string key = $"{tile.ResourceName}|{tile.Item}|{tile.Rotation}|{string.Join(",", tile.ToDefinition().Sockets)}";
                if (seen.TryGetValue(key, out int option))
                {
                    if (index == previous)
                        previous = _tileForOption[option];
                }
                else
                {
                    seen[key] = _tilePicker.ItemCount;
                    _tilePicker.AddItem(DisplayName(index, tile));
                    _tileForOption.Add(index);
                }
            }
            index++;
        }

        int selected = _tileForOption.IndexOf(previous);
        if (_tilePicker.ItemCount > 0)
            _tilePicker.Select(selected >= 0 ? selected : 0);
        FillRotationPicker();
    }

    private string DisplayName(int index, WfcTile tile)
    {
        if (!string.IsNullOrEmpty(tile.ResourceName))
            return tile.ResourceName;
        if (tile.Item == WfcTile.EmptyItem)
            return $"{index}: Empty";
        return _tileSet.Library != null && _tileSet.Library.GetItemList().Contains(tile.Item)
            ? $"{index}: {_tileSet.Library.GetItemName(tile.Item)}"
            : $"{index}: Item {tile.Item}";
    }

    private WfcTile SelectedTile => SelectedTileIndex >= 0 ? _tileSet.Tiles[SelectedTileIndex] : null;

    private int SelectedTileIndex => _tilePicker.Selected >= 0 && _tilePicker.Selected < _tileForOption.Count ? _tileForOption[_tilePicker.Selected] : -1;

    private int SelectedTurns => _rotationPicker.Selected >= 0 ? _rotationPicker.GetItemId(_rotationPicker.Selected) : 0;

    private void FillRotationPicker()
    {
        int previous = SelectedTurns;
        _rotationPicker.Clear();
        int step = SelectedTile?.Rotation switch { TileRotation.FourWay => 1, TileRotation.TwoWay => 2, _ => 4 };
        for (int turns = 0; turns < 4; turns += step)
            _rotationPicker.AddItem($"{turns * 90}°", turns);
        int restored = _rotationPicker.GetItemIndex(previous);
        _rotationPicker.Select(restored >= 0 ? restored : 0);
    }

    private void OnTileSelected(long index)
    {
        FillRotationPicker();
        ShowSelected();
    }

    private void OnRotationSelected(long index) => ShowSelected();

    private void OnViewportInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) != 0)
        {
            var rotation = _pivot.Rotation;
            rotation.Y -= motion.Relative.X * OrbitSpeed;
            rotation.X = Mathf.Clamp(rotation.X - motion.Relative.Y * OrbitSpeed, -1.5f, 1.5f);
            _pivot.Rotation = rotation;
        }
    }

    private void ShowSelected()
    {
        var tile = SelectedTile;
        if (tile == null)
        {
            _mesh.Mesh = null;
            foreach (var label in _labels)
                label.Text = "";
            _neighbours.Text = "Add a tile to preview it.";
            return;
        }

        int turns = SelectedTurns;
        var hasMesh = tile.Item != WfcTile.EmptyItem && _tileSet.Library != null && _tileSet.Library.GetItemList().Contains(tile.Item);
        _mesh.Mesh = hasMesh ? _tileSet.Library.GetItemMesh(tile.Item) : null;
        _mesh.Transform = new Transform3D(new Basis(Vector3.Up, turns * Mathf.Pi / 2f), Vector3.Zero)
            * (hasMesh ? _tileSet.Library.GetItemMeshTransform(tile.Item) : Transform3D.Identity);

        ShowFaceLabels(tile, turns);
        ShowNeighbours(turns);
    }

    private void ShowFaceLabels(WfcTile tile, int turns)
    {
        var local = tile.ToDefinition().Sockets;
        int variant = _binding.IsValid ? _binding.Compiled.FindVariant(SelectedCompiledTile, turns) : -1;
        foreach (int direction in Directions.All3D)
        {
            string socket = variant >= 0
                ? _binding.Compiled.Variants[variant].Sockets[direction].ToString()
                : local[Directions.RotateY(direction, -turns)];
            _labels[direction].Text = $"{Directions.Name(direction)}\n{(string.IsNullOrEmpty(socket) ? "(empty)" : socket)}";
        }
    }

    // Null entries are left out of the compiled set, so list positions and tile indices can differ.
    private int SelectedCompiledTile => _tileSet.Tiles.Take(SelectedTileIndex).Count(t => t != null);

    private void ShowNeighbours(int turns)
    {
        if (!_binding.IsValid)
        {
            _neighbours.Text = "Fix the errors below to see valid neighbours.";
            return;
        }

        var compiled = _binding.Compiled;
        int variant = compiled.FindVariant(SelectedCompiledTile, turns);
        var lines = new List<string>();
        foreach (int direction in Directions.All3D)
        {
            var names = Enumerable.Range(0, compiled.Variants.Count)
                .Where(other => compiled.Allows(variant, direction, other))
                .Select(other => _binding.TileNames[compiled.Variants[other].TileIndex])
                .Distinct()
                .ToArray();
            string socket = compiled.Variants[variant].Sockets[direction].ToString();
            lines.Add($"{Directions.Name(direction)} ({socket}): {(names.Length > 0 ? string.Join(", ", names) : "nothing")}");
        }
        _neighbours.Text = string.Join("\n", lines);
    }

    private void ShowProblems()
    {
        var problems = _binding.Errors.Concat(_binding.Warnings()).ToArray();
        _problems.Text = problems.Length > 0 ? string.Join("\n", problems) : "No problems found.";
    }
}
#endif
