using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godoodads.WfcGenerator;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Demo;

/// <summary>
/// Test bench for the solver until the dialog (M2) and the demo tileset (M3) exist. Builds a small
/// hill-and-ramp tile set in code and writes the solve into the child GridMap.
/// </summary>
[Tool]
public partial class WfcSandbox : Node3D
{
    private const int Air = 5;

    // GridMap orientation index for 0, 90, 180 and 270 degrees around +Y.
    private static readonly int[] Orientations = { 0, 16, 10, 22 };

    private static readonly TileDefinition[] Tiles =
    {
        //   +X       -X       +Y      -Y      +Z       -Z
        new(new[] { "solid", "solid", "rock", "rock", "solid", "solid" }),                                  // Rock
        new(new[] { "solid", "solid", "air", "rock", "solid", "solid" }),                                   // Grass
        new(new[] { "solid", "open", "air", "rock", "side+", "side-" }, TileRotation.FourWay),              // Ramp
        new(new[] { "side-", "open", "air", "rock", "side+", "open" }, TileRotation.FourWay),               // Outer corner
        new(new[] { "solid", "side+", "air", "rock", "solid", "side-" }, TileRotation.FourWay),             // Inner corner
        new(new[] { "open", "open", "air", "air", "open", "open" }),                                        // Air
    };

    [Export] public Vector3I GridSize { get; set; } = new(24, 8, 24);
    [Export] public long Seed { get; set; } = 1;

    /// <summary>Fixes the bottom layer to rock. Without it, empty columns can reach the bottom.</summary>
    [Export] public bool GroundLayer { get; set; } = true;

    /// <summary>Keeps cells already in the GridMap and solves around them. Paint some first.</summary>
    [Export] public bool KeepExistingCells { get; set; }

    [ExportToolButton("Generate")] public Callable GenerateButton => new(this, MethodName.Generate);
    [ExportToolButton("New Seed + Generate")] public Callable NewSeedButton => new(this, MethodName.GenerateWithNewSeed);
    [ExportToolButton("Clear")] public Callable ClearButton => new(this, MethodName.Clear);
    [ExportToolButton("Save Test Assets")] public Callable SaveAssetsButton => new(this, MethodName.SaveTestAssets);

    [Export(PropertyHint.MultilineText)] public string Status { get; set; } = "";

    public override void _Ready()
    {
        Map.MeshLibrary ??= BuildLibrary();
        if (!Engine.IsEditorHint())
        {
            Generate();
            PointCamera();
        }
    }

    private GridMap Map => GetNode<GridMap>("GridMap");

    public void GenerateWithNewSeed()
    {
        Seed = (long)GD.Randi();
        Generate();
    }

    public void Clear()
    {
        Map.Clear();
        Report("Cleared.");
    }

    /// <summary>Writes the MeshLibrary and tile set that generate_test.tscn uses.</summary>
    public void SaveTestAssets()
    {
        const string libraryPath = "res://demo/wfc_generator/hills_library.tres";
        const string tileSetPath = "res://demo/wfc_generator/hills_tileset.tres";

        var library = BuildLibrary();
        library.TakeOverPath(libraryPath);
        var tileSet = new WfcTileSet { Library = library };
        for (int i = 0; i < Tiles.Length; i++)
        {
            var s = Tiles[i].Sockets;
            tileSet.Tiles.Add(new WfcTile
            {
                Item = i == Air ? WfcTile.EmptyItem : i,
                Rotation = Tiles[i].Rotation,
                SocketPosX = s[Directions.PosX], SocketNegX = s[Directions.NegX],
                SocketPosY = s[Directions.PosY], SocketNegY = s[Directions.NegY],
                SocketPosZ = s[Directions.PosZ], SocketNegZ = s[Directions.NegZ],
            });
        }
        var first = ResourceSaver.Save(library, libraryPath);
        var second = ResourceSaver.Save(tileSet, tileSetPath);
        Report($"Saved test assets: library {first}, tile set {second}.");
    }

    public void Generate()
    {
        var map = Map;
        map.MeshLibrary ??= BuildLibrary();

        GridTopology grid;
        try
        {
            grid = GridTopology.Create3D(GridSize);
        }
        catch (ArgumentException e)
        {
            Report(e.Message);
            return;
        }

        var set = CompiledTileSet.Compile(Tiles, grid.ActiveDirections);
        if (!KeepExistingCells)
            map.Clear();
        var initial = BuildInitialCells(map, grid, set, out int kept, out int excluded);

        var clock = Stopwatch.StartNew();
        var result = new WfcSolver(grid, set, initial).Solve(new SolveOptions { Seed = Seed });
        double seconds = clock.Elapsed.TotalSeconds;

        if (result.Status != SolveStatus.Solved)
        {
            Report(Describe(result, seconds, Seed));
            return;
        }

        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            if (initial[cell] != WfcSolver.Free && !(GroundLayer && initial[cell] >= 0 && grid.Coordinates(cell).Y == 0))
                continue;
            var variant = set.Variants[result.Cells[cell]];
            if (variant.TileIndex != Air)
                map.SetCellItem(grid.Coordinates(cell), variant.TileIndex, Orientations[variant.QuarterTurns]);
        }

        Report($"Solved {grid.CellCount:N0} cells in {seconds:F2} s "
            + $"({result.Backtracks} backtracks, {result.Restarts} restarts, {kept} kept, {excluded} ignored). Seed {Seed}.");
    }

    private int[] BuildInitialCells(GridMap map, GridTopology grid, CompiledTileSet set, out int kept, out int excluded)
    {
        kept = excluded = 0;
        var initial = new int[grid.CellCount];
        Array.Fill(initial, WfcSolver.Free);

        foreach (Vector3I position in map.GetUsedCells())
        {
            if (!grid.Contains(position))
                continue;
            int turns = Array.IndexOf(Orientations, map.GetCellItemOrientation(position));
            int variant = turns < 0 ? -1 : set.FindVariant(map.GetCellItem(position), turns);
            initial[grid.Index(position)] = variant >= 0 ? variant : WfcSolver.Excluded;
            if (variant >= 0)
                kept++;
            else
                excluded++;
        }

        if (GroundLayer)
        {
            int rock = set.FindVariant(0, 0);
            for (int z = 0; z < grid.Size.Z; z++)
                for (int x = 0; x < grid.Size.X; x++)
                {
                    int cell = grid.Index(new Vector3I(x, 0, z));
                    if (initial[cell] == WfcSolver.Free)
                        initial[cell] = rock;
                }
        }
        return initial;
    }

    private static string Describe(SolveResult result, double seconds, long seed)
    {
        var lines = new List<string> { $"{result.Status} after {seconds:F2} s ({result.Backtracks} backtracks, {result.Restarts} restarts). Seed {seed}." };
        foreach (var conflict in result.FixedConflicts)
            lines.Add($"Fixed cells {conflict.CellA} and {conflict.CellB} conflict: {conflict.SocketA} against {conflict.SocketB}.");
        if (result.Failure != null)
        {
            lines.Add($"Failing cell {result.Failure.Cell}:");
            foreach (var neighbour in result.Failure.Neighbours)
                lines.Add($"  {Directions.Name(neighbour.Direction)} {neighbour.Cell}: {string.Join(", ", neighbour.Sockets)}");
        }
        return string.Join("\n", lines);
    }

    private void Report(string message)
    {
        Status = message;
        NotifyPropertyListChanged();
        GD.Print($"WFC sandbox: {message}");
    }

    private void PointCamera()
    {
        var camera = GetNode<Camera3D>("Camera3D");
        var size = (Vector3)GridSize * Map.CellSize;
        var centre = size * new Vector3(0.5f, 0.25f, 0.5f);
        float distance = Mathf.Max(size.X, size.Z);
        camera.LookAtFromPosition(centre + new Vector3(0.7f, 0.8f, 0.7f) * distance, centre);
    }

    private static MeshLibrary BuildLibrary()
    {
        // Corner heights of the top surface, ordered (+X+Z, +X-Z, -X+Z, -X-Z).
        var shapes = new (string Name, Color Colour, float[] Heights)[]
        {
            ("Rock", new Color(0.45f, 0.43f, 0.42f), new[] { 1f, 1f, 1f, 1f }),
            ("Grass", new Color(0.36f, 0.62f, 0.27f), new[] { 1f, 1f, 1f, 1f }),
            ("Ramp", new Color(0.72f, 0.62f, 0.34f), new[] { 1f, 1f, -1f, -1f }),
            ("Outer corner", new Color(0.82f, 0.48f, 0.26f), new[] { 1f, -1f, -1f, -1f }),
            ("Inner corner", new Color(0.33f, 0.56f, 0.72f), new[] { 1f, 1f, 1f, -1f }),
        };

        var library = new MeshLibrary();
        for (int item = 0; item < shapes.Length; item++)
        {
            library.CreateItem(item);
            library.SetItemName(item, shapes[item].Name);
            library.SetItemMesh(item, BuildBlock(shapes[item].Heights, shapes[item].Colour));
        }
        return library;
    }

    /// <summary>A two-unit block whose four top corners have the given heights.</summary>
    private static ArrayMesh BuildBlock(float[] h, Color colour)
    {
        Vector3 top0 = new(1, h[0], 1), top1 = new(1, h[1], -1), top2 = new(-1, h[2], 1), top3 = new(-1, h[3], -1);
        Vector3 low0 = new(1, -1, 1), low1 = new(1, -1, -1), low2 = new(-1, -1, 1), low3 = new(-1, -1, -1);

        var tool = new SurfaceTool();
        tool.Begin(Mesh.PrimitiveType.Triangles);

        Quad(tool, top0, top1, top3, top2);
        Quad(tool, low0, low2, low3, low1);
        Quad(tool, top0, low0, low1, top1);
        Quad(tool, top2, top3, low3, low2);
        Quad(tool, top0, top2, low2, low0);
        Quad(tool, top1, low1, low3, top3);

        var mesh = tool.Commit();
        mesh.SurfaceSetMaterial(0, new StandardMaterial3D { AlbedoColor = colour, CullMode = BaseMaterial3D.CullModeEnum.Disabled });
        return mesh;
    }

    private static void Quad(SurfaceTool tool, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Triangle(tool, a, b, c);
        Triangle(tool, a, c, d);
    }

    private static void Triangle(SurfaceTool tool, Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = (c - a).Cross(b - a);
        if (normal.LengthSquared() < 1e-6f)
            return; // A side that collapsed to a line at a low corner.

        // Blocks are convex around the origin, so winding can be fixed from the outward direction.
        if (normal.Dot(a + b + c) < 0f)
        {
            (b, c) = (c, b);
            normal = -normal;
        }
        normal = normal.Normalized();
        foreach (var point in new[] { a, b, c })
        {
            tool.SetNormal(normal);
            tool.AddVertex(point);
        }
    }
}
