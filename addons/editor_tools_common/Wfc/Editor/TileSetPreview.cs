#if TOOLS
using System;
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
public partial class TileSetPreview : VBoxContainer, ISerializationListener
{
    private const double RefreshInterval = 0.5;
    private const float LabelDistance = 1.75f;
    private const float OrbitSpeed = 0.01f;
    private const float ClickSlop = 4f;
    private const float LabelPickRadius = 48f;
    private static readonly Color UnmatchedColour = new(1f, 0.4f, 0.35f);

    private WfcTileSet _tileSet;
    private OptionButton _tilePicker;
    private OptionButton _rotationPicker;
    private SubViewportContainer _viewportContainer;
    private Node3D _pivot;
    private Camera3D _camera;
    private Vector2 _pressPosition;
    private bool _dragged;
    private PopupPanel _editPopup;
    private Label _editTitle;
    private LineEdit _editLine;
    private ItemList _suggestions;
    private WfcTile _editTile;
    private int _editDirection;
    private int _editTurns;
    private MeshInstance3D _mesh;
    private Godot.Collections.Array<Label3D> _labels = new();
    private Label _weight;
    private RichTextLabel _neighbours;
    private RichTextLabel _problems;
    private Godot.Collections.Array<int> _tileForOption = new();
    private string _signature = "";
    private double _sinceRefresh;

    private TileSetBinding _binding;

    // Measured shares (brief §13.2).
    private SpinBox _sampleX;
    private SpinBox _sampleY;
    private SpinBox _sampleZ;
    private SpinBox _sampleSeed;
    private Button _measureButton;
    private RichTextLabel _sharesText;
    private SolveJob _measureJob;
    private GridTopology _measureGrid;
    private TileSetBinding _measureBinding;
    private int[] _measureGroups;
    private List<int> _measureGroupTiles;
    private ulong _measureStarted;

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

        _weight = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        AddChild(_weight);

        BuildViewport();
        AddChild(new Label
        {
            Text = "Drag to orbit. Click a face label to edit its socket. Red: no tile can face it.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(1, 1, 1, 0.7f),
        });
        BuildEditPopup();

        AddChild(new Label { Text = "Valid neighbours" });
        _neighbours = NewText();
        AddChild(_neighbours);
        AddChild(new Label { Text = "Tile set check" });
        _problems = NewText();
        AddChild(_problems);
        BuildMeasureRow();

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
        _camera = new Camera3D { Position = new Vector3(0, 0, 7), Fov = 45, Current = true };
        _pivot.AddChild(_camera);

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

    private void BuildMeasureRow()
    {
        AddChild(new Label { Text = "Tile shares" });
        var row = new HFlowContainer();
        AddChild(row);
        row.AddChild(new Label { Text = "Sample" });
        _sampleX = SampleField(16, 128, "Sample width (X)");
        _sampleY = SampleField(8, 64, "Sample height (Y)");
        _sampleZ = SampleField(16, 128, "Sample depth (Z)");
        row.AddChild(_sampleX);
        row.AddChild(_sampleY);
        row.AddChild(_sampleZ);
        row.AddChild(new Label { Text = "Seed" });
        _sampleSeed = new SpinBox { MinValue = 0, MaxValue = int.MaxValue, Value = 1, Rounded = true, TooltipText = "Seed of the sample solve" };
        row.AddChild(_sampleSeed);
        _measureButton = new Button { Text = "Measure", TooltipText = "Solve a sample in the background and compare each tile's real share with its weight share." };
        _measureButton.Connect(BaseButton.SignalName.Pressed, new Callable(this, MethodName.OnMeasurePressed));
        row.AddChild(_measureButton);
        _sharesText = NewText();
        _sharesText.BbcodeEnabled = true;
        _sharesText.Text = "Measure a sample solve to see how much of it each tile really covers.";
        AddChild(_sharesText);
    }

    private static SpinBox SampleField(int value, int max, string tooltip) =>
        new() { MinValue = 1, MaxValue = max, Value = value, Rounded = true, TooltipText = tooltip };

    private void OnMeasurePressed()
    {
        if (_measureJob != null)
        {
            StopMeasure("Measuring was stopped.");
            return;
        }
        if (_binding == null || !_binding.IsValid)
        {
            _sharesText.Text = "Fix the tile set errors before measuring.";
            return;
        }

        _measureBinding = _binding;
        _measureGrid = GridTopology.Create3D(new Vector3I((int)_sampleX.Value, (int)_sampleY.Value, (int)_sampleZ.Value));
        var keys = new List<string>();
        _measureGroupTiles = new List<int>();
        foreach (var (tile, index) in _tileSet.Tiles.Select((tile, index) => (tile, index)).Where(p => p.tile != null))
        {
            string key = TileKey(tile);
            if (!keys.Contains(key))
                _measureGroupTiles.Add(index);
            keys.Add(key);
        }
        _measureGroups = TileShares.GroupByKey(keys);
        _measureJob = new SolveJob(_measureGrid, _measureBinding.Compiled, null, new SolveOptions { Seed = (long)_sampleSeed.Value });
        _measureStarted = Time.GetTicksMsec();
        _measureJob.Start();
        _measureButton.Text = "Stop";
        _sharesText.Text = "Measuring...";
    }

    /// <summary>Cancels a running sample solve and waits briefly for its thread.</summary>
    private void StopMeasure(string message)
    {
        if (_measureJob != null)
        {
            _measureJob.Cancel();
            _measureJob.Wait(2000);
            _measureJob = null;
        }
        if (_measureButton != null && IsInstanceValid(_measureButton))
            _measureButton.Text = "Measure";
        if (message != null && _sharesText != null && IsInstanceValid(_sharesText))
            _sharesText.Text = message;
    }

    private void PollMeasure()
    {
        if (_measureJob == null)
            return;
        if (!_measureJob.IsFinished)
        {
            _sharesText.Text = $"Measuring... {_measureJob.ResolvedCells} of {_measureGrid.CellCount} cells.";
            return;
        }
        var job = _measureJob;
        _measureJob = null;
        _measureButton.Text = "Measure";
        double seconds = (Time.GetTicksMsec() - _measureStarted) / 1000.0;
        if (job.Error != null)
            _sharesText.Text = $"The sample solve failed: {job.Error.Message}";
        else if (job.Result.Status != SolveStatus.Solved)
            _sharesText.Text = SampleFailure(job.Result);
        else
            _sharesText.Text = DescribeShares(TileShares.Measure(_measureGrid, _measureBinding.Compiled, job.Result.Cells, _measureGroups), seconds);
    }

    private string SampleFailure(SolveResult result)
    {
        string text = result.Status == SolveStatus.Impossible
            ? "This tile set has no solution for the sample."
            : "The sample could not be solved within the attempt limits. A solution may still exist; try another seed or size.";
        if (result.Failure != null)
        {
            text += $" No tile fits at cell {result.Failure.Cell}.";
            foreach (var neighbour in result.Failure.Neighbours)
                text += $"\n{Directions.Name(neighbour.Direction)} neighbour {neighbour.Cell} offers: {string.Join(", ", neighbour.Sockets)}";
        }
        return text;
    }

    private string DescribeShares(TileShares shares, double seconds)
    {
        var size = _measureGrid.Size;
        var lines = new List<string>
        {
            $"Sample {size.X}×{size.Y}×{size.Z}, seed {(long)_sampleSeed.Value}, solved in {seconds:0.0} s. "
                + "Weight share / measured share. Weights also decide which cells are filled first, so the two can differ.",
        };
        var order = Enumerable.Range(0, _measureGroupTiles.Count).OrderByDescending(g => shares.MeasuredShare[g]);
        foreach (int group in order)
        {
            var tile = _tileSet.Tiles[_measureGroupTiles[group]];
            string line = $"{DisplayName(_measureGroupTiles[group], tile)}: {shares.WeightShare[group]:P1} / {shares.MeasuredShare[group]:P1}";
            if (shares.IsFlagged(group))
                line = $"[color=#ff8a75]{line} (more than {TileShares.FlagFactor:0}× off)[/color]";
            if (tile.HeightCurve != null && shares.ThirdShare != null)
            {
                var thirds = shares.ThirdShare[group];
                line += $"\n    bottom {thirds[0]:P0}, middle {thirds[1]:P0}, top {thirds[2]:P0}";
            }
            lines.Add(line);
        }
        return string.Join("\n", lines);
    }

    public void OnBeforeSerialize() => StopMeasure("Measuring was stopped because the project was rebuilt.");

    public void OnAfterDeserialize() { }

    public override void _ExitTree() => StopMeasure(null);

    private void BuildEditPopup()
    {
        _editPopup = new PopupPanel();
        AddChild(_editPopup);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(220, 0) };
        _editPopup.AddChild(box);
        _editTitle = new Label();
        box.AddChild(_editTitle);
        _editLine = new LineEdit { PlaceholderText = "socket label" };
        _editLine.Connect(LineEdit.SignalName.TextChanged, new Callable(this, MethodName.OnEditTextChanged));
        _editLine.Connect(LineEdit.SignalName.TextSubmitted, new Callable(this, MethodName.OnEditSubmitted));
        box.AddChild(_editLine);
        _suggestions = new ItemList { CustomMinimumSize = new Vector2(0, 140), AutoHeight = false };
        _suggestions.Connect(ItemList.SignalName.ItemClicked, new Callable(this, MethodName.OnSuggestionClicked));
        box.AddChild(_suggestions);
    }

    public override void _Process(double delta)
    {
        if (_tileSet == null)
            return;
        PollMeasure();
        if (!IsVisibleInTree())
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
                text.Append('|').Append(TileKey(tile));
        return text.ToString();
    }

    private void Rebuild()
    {
        if (_measureJob != null || (_signature != "" && _signature != Signature()))
            StopMeasure("The tile set changed. Measure again.");
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
                string key = TileKey(tile);
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

    private static string TileKey(WfcTile tile) =>
        $"{tile.ResourceName}|{tile.Item}|{tile.Rotation}|{tile.Weight}|{string.Join(",", tile.ToDefinition().Sockets)}"
        + (tile.HeightCurve == null ? "" : "|" + string.Join(",", tile.SampleHeightCurve()));

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
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed)
            {
                _pressPosition = button.Position;
                _dragged = false;
            }
            else if (!_dragged)
            {
                TryEditLabelAt(button.Position);
            }
        }
        else if (@event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) != 0)
        {
            if (motion.Position.DistanceTo(_pressPosition) > ClickSlop)
                _dragged = true;
            var rotation = _pivot.Rotation;
            rotation.Y -= motion.Relative.X * OrbitSpeed;
            rotation.X = Mathf.Clamp(rotation.X - motion.Relative.Y * OrbitSpeed, -1.5f, 1.5f);
            _pivot.Rotation = rotation;
        }
    }

    /// <summary>Opens the socket editor for the face label nearest to a click, if one is close enough.</summary>
    private void TryEditLabelAt(Vector2 position)
    {
        var tile = SelectedTile;
        if (tile == null || _camera == null)
            return;
        int best = -1;
        float bestDistance = LabelPickRadius;
        foreach (int direction in Directions.All3D)
        {
            var point = _labels[direction].GlobalPosition;
            if (_camera.IsPositionBehind(point))
                continue;
            float distance = _camera.UnprojectPosition(point).DistanceTo(position);
            if (distance < bestDistance)
            {
                best = direction;
                bestDistance = distance;
            }
        }
        if (best < 0)
            return;

        _editTile = tile;
        _editDirection = best;
        _editTurns = SelectedTurns;
        int local = SocketEditing.LocalFace(best, _editTurns);
        _editTitle.Text = _editTurns == 0
            ? $"{DisplayName(SelectedTileIndex, tile)}, face {Directions.Name(best)}"
            : $"{DisplayName(SelectedTileIndex, tile)}, face {Directions.Name(best)} (the tile's {Directions.Name(local)})";
        _editLine.Text = ShownSocket(tile, _editTurns, best);
        FillSuggestions();
        _editPopup.Popup(new Rect2I((Vector2I)(_viewportContainer.GetScreenPosition() + position), Vector2I.Zero));
        _editLine.GrabFocus();
        _editLine.SelectAll();
    }

    private void OnEditTextChanged(string text) => FillSuggestions();

    private void FillSuggestions()
    {
        _suggestions.Clear();
        var tiles = _tileSet.Tiles.Where(t => t != null).Select(t => t.ToDefinition());
        foreach (string label in SocketEditing.Suggestions(tiles, Directions.IsVertical(_editDirection), _editLine.Text))
            _suggestions.AddItem(label);
    }

    private void OnSuggestionClicked(long index, Vector2 atPosition, long mouseButtonIndex) =>
        OnEditSubmitted(_suggestions.GetItemText((int)index));

    /// <summary>Stores the label on the tile's own face, as one undo action.</summary>
    private void OnEditSubmitted(string text)
    {
        _editPopup.Hide();
        var tile = _editTile;
        if (tile == null || !IsInstanceValid(tile))
            return;
        int local = SocketEditing.LocalFace(_editDirection, _editTurns);
        string label = SocketEditing.ToLocalLabel(text, _editDirection, _editTurns);
        var property = WfcTile.SocketProperty(local);
        string old = tile.GetSocket(local);
        if (label == old)
            return;

        var undo = EditorInterface.Singleton.GetEditorUndoRedo();
        undo.CreateAction($"Set socket {Directions.Name(local)} of {DisplayName(SelectedTileIndex, tile)}", UndoRedo.MergeMode.Disable, _tileSet);
        undo.AddDoProperty(tile, property, label);
        undo.AddUndoProperty(tile, property, old);
        undo.AddDoMethod(tile, Resource.MethodName.EmitChanged);
        undo.AddUndoMethod(tile, Resource.MethodName.EmitChanged);
        undo.AddDoMethod(tile, GodotObject.MethodName.NotifyPropertyListChanged);
        undo.AddUndoMethod(tile, GodotObject.MethodName.NotifyPropertyListChanged);
        undo.CommitAction();
        Rebuild();
    }

    /// <summary>The label shown on a face of the variant turned <paramref name="turns"/>.</summary>
    private string ShownSocket(WfcTile tile, int turns, int direction)
    {
        int variant = _binding.IsValid ? _binding.Compiled.FindVariant(SelectedCompiledTile, turns) : -1;
        return variant >= 0
            ? _binding.Compiled.Variants[variant].Sockets[direction].ToString()
            : tile.ToDefinition().Sockets[SocketEditing.LocalFace(direction, turns)];
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
            _weight.Text = "";
            return;
        }

        ShowWeight(tile);

        int turns = SelectedTurns;
        var hasMesh = tile.Item != WfcTile.EmptyItem && _tileSet.Library != null && _tileSet.Library.GetItemList().Contains(tile.Item);
        _mesh.Mesh = hasMesh ? _tileSet.Library.GetItemMesh(tile.Item) : null;
        _mesh.Transform = new Transform3D(new Basis(Vector3.Up, turns * Mathf.Pi / 2f), Vector3.Zero)
            * (hasMesh ? _tileSet.Library.GetItemMeshTransform(tile.Item) : Transform3D.Identity);

        ShowFaceLabels(tile, turns);
        ShowNeighbours(turns);
    }

    /// <summary>The tile's weight and its share of the total, counting identical copies together.</summary>
    private void ShowWeight(WfcTile tile)
    {
        var tiles = _tileSet.Tiles.Where(t => t != null).ToArray();
        string key = TileKey(tile);
        int copies = tiles.Count(t => TileKey(t) == key);
        double own = Math.Max(0, tile.Weight) * copies;
        double total = tiles.Sum(t => Math.Max(0, (double)t.Weight));
        string copiesText = copies > 1 ? $" ({copies} copies)" : "";

        if (tile.Weight < 0)
            _weight.Text = $"Weight {tile.Weight:0.###} is not allowed.";
        else if (tile.Weight == 0)
            _weight.Text = $"Weight 0{copiesText}: never generated, but it can be placed by hand.";
        else
            _weight.Text = $"Weight {tile.Weight:0.###}{copiesText}: {own / total:P1} of the total weight, shared by its rotations.";
        if (tile.Weight > 0 && tile.SampleHeightCurve() is { } curve)
            _weight.Text += $"\nHeight curve: ×{curve[0]:0.##} at the bottom, ×{curve[curve.Length / 2]:0.##} in the middle, ×{curve[^1]:0.##} at the top.";
    }

    private void ShowFaceLabels(WfcTile tile, int turns)
    {
        int variant = _binding.IsValid ? _binding.Compiled.FindVariant(SelectedCompiledTile, turns) : -1;
        var unmatched = variant >= 0 ? SocketEditing.UnmatchedFaces(_binding.Compiled, variant) : new bool[Directions.Count];
        foreach (int direction in Directions.All3D)
        {
            string socket = ShownSocket(tile, turns, direction);
            _labels[direction].Text = $"{Directions.Name(direction)}\n{(string.IsNullOrEmpty(socket) ? "(empty)" : socket)}";
            _labels[direction].Modulate = unmatched[direction] ? UnmatchedColour : Colors.White;
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
