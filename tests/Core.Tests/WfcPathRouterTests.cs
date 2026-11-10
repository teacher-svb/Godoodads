using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godoodads.WfcGenerator.Core;
using Xunit;
using static Godoodads.Tests.WfcTestTiles;

namespace Godoodads.Tests;

// Brief §13.3: guided paths between placed path cells.
public class WfcPathRouterTests
{
    private const string P = "path";

    // A flat set: ground, and path pieces whose path faces face each other.
    private static readonly TileDefinition[] Flat =
    {
        Tile2D("g", "g", "g", "g"),
        Tile2D(P, P, "g", "g", TileRotation.FourWay),
        Tile2D(P, "g", P, "g", TileRotation.FourWay),
        Tile2D(P, P, P, "g", TileRotation.FourWay),
        Tile2D(P, P, P, P),
        Tile2D(P, "g", "g", "g", TileRotation.FourWay),
    };

    private static int Mask(params int[] directions) => directions.Aggregate(0, (mask, d) => mask | 1 << d);

    private static int VariantWithFaces(CompiledTileSet set, int mask)
    {
        var faces = PathRouter.PathFaces(set, P);
        return Array.FindIndex(faces, f => f == mask);
    }

    private static (GridTopology Grid, CompiledTileSet Set, int[] Initial) Setup(GridTopology grid, TileDefinition[] tiles,
        params (Vector3I Cell, int Mask)[] placed)
    {
        var set = CompiledTileSet.Compile(tiles, grid.ActiveDirections);
        Assert.True(set.IsValid);
        var initial = Enumerable.Repeat(WfcSolver.Free, grid.CellCount).ToArray();
        foreach (var (cell, mask) in placed)
        {
            int variant = VariantWithFaces(set, mask);
            Assert.True(variant >= 0, $"No variant with faces {mask}.");
            initial[grid.Index(cell)] = variant;
        }
        return (grid, set, initial);
    }

    private static int[] SolveWith(GridTopology grid, CompiledTileSet set, int[] initial, PathPlan plan, long seed = 1)
    {
        Assert.True(plan.IsValid, string.Join("; ", plan.Errors.Select(e => e.Message)));
        var result = new WfcSolver(grid, set, initial).Solve(new SolveOptions { Seed = seed, Restrictions = plan.Restrictions });
        Assert.Equal(SolveStatus.Solved, result.Status);
        AssertValid(grid, set, result.Cells);

        // Every free cell shows exactly the path faces its route asks for (none off the route).
        var faces = PathRouter.PathFaces(set, P);
        for (int cell = 0; cell < grid.CellCount; cell++)
            if (initial[cell] == WfcSolver.Free)
                Assert.Equal(plan.RouteFaces[cell], faces[result.Cells[cell]]);
        return result.Cells;
    }

    /// <summary>The cells reachable from <paramref name="start"/> through matching path faces.</summary>
    private static HashSet<int> Connected(GridTopology grid, CompiledTileSet set, int[] cells, int start)
    {
        var faces = PathRouter.PathFaces(set, P);
        var seen = new HashSet<int> { start };
        var stack = new Stack<int>(seen);
        while (stack.Count > 0)
        {
            int cell = stack.Pop();
            foreach (int direction in grid.ActiveDirections)
            {
                int neighbour = grid.Neighbour(cell, direction);
                if ((faces[cells[cell]] & 1 << direction) != 0 && neighbour >= 0 && seen.Add(neighbour))
                    stack.Push(neighbour);
            }
        }
        return seen;
    }

    private static int PathCells(CompiledTileSet set, int[] cells)
    {
        var faces = PathRouter.PathFaces(set, P);
        return cells.Count(v => faces[v] != 0);
    }

    [Fact]
    public void WithoutAPathSocketNothingChanges()
    {
        var (grid, set, initial) = Setup(GridTopology.Create2D(8, 8), Flat);
        var plan = PathRouter.Plan(grid, set, initial, "", 0.3, 1);
        Assert.True(plan.IsValid);
        Assert.Null(plan.Restrictions);
        var plain = new WfcSolver(grid, set, initial).Solve(new SolveOptions { Seed = 4 });
        var planned = new WfcSolver(grid, set, initial).Solve(new SolveOptions { Seed = 4, Restrictions = plan.Restrictions });
        Assert.Equal(plain.Cells, planned.Cells);
    }

    [Fact]
    public void TwoEndsGiveOneConnectedPathAndNoOtherPathTiles()
    {
        var a = new Vector3I(1, 0, 1);
        var b = new Vector3I(10, 0, 9);
        var (grid, set, initial) = Setup(GridTopology.Create2D(12, 12), Flat,
            (a, Mask(Directions.PosX)), (b, Mask(Directions.NegX)));
        var plan = PathRouter.Plan(grid, set, initial, P, 0.3, 7);
        Assert.Equal(1, plan.RouteCount);
        var cells = SolveWith(grid, set, initial, plan);

        var reached = Connected(grid, set, cells, grid.Index(a));
        Assert.Contains(grid.Index(b), reached);
        Assert.Equal(plan.RouteCellCount + 2, reached.Count);
        Assert.Equal(reached.Count, PathCells(set, cells));
    }

    [Fact]
    public void ThreeEndsBranchThroughAJunction()
    {
        var ends = new[] { new Vector3I(0, 0, 6), new Vector3I(12, 0, 6), new Vector3I(6, 0, 12) };
        var (grid, set, initial) = Setup(GridTopology.Create2D(13, 13), Flat,
            (ends[0], Mask(Directions.PosX)), (ends[1], Mask(Directions.NegX)), (ends[2], Mask(Directions.NegZ)));
        var plan = PathRouter.Plan(grid, set, initial, P, 0, 1);
        Assert.Equal(2, plan.RouteCount);
        var cells = SolveWith(grid, set, initial, plan);

        var reached = Connected(grid, set, cells, grid.Index(ends[0]));
        Assert.All(ends, e => Assert.Contains(grid.Index(e), reached));
        var faces = PathRouter.PathFaces(set, P);
        Assert.Contains(cells, v => BitCount(faces[v]) == 3);
        Assert.Equal(reached.Count, PathCells(set, cells));
    }

    private static int BitCount(int mask) => System.Numerics.BitOperations.PopCount((uint)mask);

    [Fact]
    public void WanderZeroIsAShortestRouteAndTheSeedRepeats()
    {
        var (grid, set, initial) = Setup(GridTopology.Create2D(10, 10), Flat,
            (new Vector3I(0, 0, 5), Mask(Directions.PosX)), (new Vector3I(9, 0, 5), Mask(Directions.NegX)));
        Assert.Equal(8, PathRouter.Plan(grid, set, initial, P, 0, 3).RouteCellCount);

        var wandering = Enumerable.Range(1, 6).Select(seed => PathRouter.Plan(grid, set, initial, P, 1, seed)).ToArray();
        Assert.All(wandering, p => Assert.True(p.RouteCellCount >= 8));
        Assert.True(wandering.Select(p => string.Join(",", p.RouteFaces)).Distinct().Count() > 1);
        Assert.Equal(wandering[0].RouteFaces, PathRouter.Plan(grid, set, initial, P, 1, 1).RouteFaces);
    }

    [Fact]
    public void AStraightPlacedAloneGetsARouteForBothFaces()
    {
        var (grid, set, initial) = Setup(GridTopology.Create2D(8, 8), Flat, (new Vector3I(4, 0, 4), Mask(Directions.PosX, Directions.NegX)));
        var plan = PathRouter.Plan(grid, set, initial, P, 0.3, 2);
        Assert.Equal(1, plan.RouteCount);
        var cells = SolveWith(grid, set, initial, plan);
        Assert.Equal(PathCells(set, cells), Connected(grid, set, cells, grid.Index(new Vector3I(4, 0, 4))).Count);
    }

    [Fact]
    public void AFaceOutOfTheBoundsNeedsNoRoute()
    {
        var (grid, set, initial) = Setup(GridTopology.Create2D(6, 6), Flat, (new Vector3I(0, 0, 2), Mask(Directions.NegX)));
        var plan = PathRouter.Plan(grid, set, initial, P, 0.3, 1);
        Assert.Equal(0, plan.RouteCount);
        var cells = SolveWith(grid, set, initial, plan);
        Assert.Equal(1, PathCells(set, cells));
    }

    // Climbing: a rise has path faces on its low side and top, the cell above it on its bottom and high side.
    private static readonly TileDefinition[] Climbing =
    {
        Tile3D("g", "g", "v", "v", "g", "g"),
        Tile3D(P, P, "v", "v", "g", "g", TileRotation.FourWay),
        Tile3D(P, "g", "v", "v", "g", "g", TileRotation.FourWay),
        Tile3D("g", P, P, "v", "g", "g", TileRotation.FourWay),
        Tile3D(P, "g", "v", P, "g", "g", TileRotation.FourWay),
    };

    [Fact]
    public void APathClimbsWhereTheTileSetOffersIt()
    {
        var low = new Vector3I(0, 0, 2);
        var high = new Vector3I(6, 1, 2);
        var (grid, set, initial) = Setup(GridTopology.Create3D(new Vector3I(7, 2, 5)), Climbing,
            (low, Mask(Directions.PosX)), (high, Mask(Directions.NegX)));
        var plan = PathRouter.Plan(grid, set, initial, P, 0.3, 5);
        var cells = SolveWith(grid, set, initial, plan);
        Assert.Contains(grid.Index(high), Connected(grid, set, cells, grid.Index(low)));
        Assert.Contains(plan.RouteFaces, f => (f & Mask(Directions.PosY)) != 0);
    }

    [Fact]
    public void WithoutClimbingTilesAPathCannotChangeLayer()
    {
        var (grid, set, initial) = Setup(GridTopology.Create3D(new Vector3I(7, 2, 5)), Climbing.Take(3).ToArray(),
            (new Vector3I(0, 0, 2), Mask(Directions.PosX)), (new Vector3I(6, 1, 2), Mask(Directions.NegX)));
        var plan = PathRouter.Plan(grid, set, initial, P, 0.3, 5);
        var error = Assert.Single(plan.Errors);
        Assert.Contains("No route joins", error.Message);
        Assert.Contains(new Vector3I(6, 1, 2), error.Cells);
    }

    [Fact]
    public void AnUnusedPathSocketIsAnError()
    {
        var (grid, set, initial) = Setup(GridTopology.Create2D(4, 4), Flat);
        var error = Assert.Single(PathRouter.Plan(grid, set, initial, "road", 0.3, 1).Errors);
        Assert.Contains("'road'", error.Message);
        Assert.False(PathRouter.Plan(grid, set, initial, "bad name", 0.3, 1).IsValid);
        Assert.True(PathRouter.Plan(grid, set, initial, " path+ ", 0.3, 1).IsValid);
    }

    [Fact]
    public void ALoneEndLeadsNowhere()
    {
        var (grid, set, initial) = Setup(GridTopology.Create2D(6, 6), Flat, (new Vector3I(2, 0, 2), Mask(Directions.PosX)));
        var error = Assert.Single(PathRouter.Plan(grid, set, initial, P, 0.3, 1).Errors);
        Assert.Contains("leads nowhere", error.Message);
        Assert.Equal(new[] { new Vector3I(2, 0, 2) }, error.Cells);
    }

    [Fact]
    public void AClosedPathCannotBeJoinedToOthers()
    {
        // Two ends facing each other are complete; a third, separate pair still needs them.
        var (grid, set, initial) = Setup(GridTopology.Create2D(8, 8), Flat,
            (new Vector3I(1, 0, 1), Mask(Directions.PosX)), (new Vector3I(2, 0, 1), Mask(Directions.NegX)),
            (new Vector3I(1, 0, 6), Mask(Directions.PosX)), (new Vector3I(6, 0, 6), Mask(Directions.NegX)));
        var error = Assert.Single(PathRouter.Plan(grid, set, initial, P, 0.3, 1).Errors);
        Assert.Contains("no open path face", error.Message);
    }

    [Fact]
    public void AnEnclosedEndCannotBeConnected()
    {
        // The end at (3, 3) faces +X into a free cell walled in by placed ground.
        var placed = new List<(Vector3I, int)> { (new Vector3I(3, 0, 3), Mask(Directions.PosX)), (new Vector3I(0, 0, 0), Mask(Directions.PosZ)) };
        foreach (var wall in new[] { new Vector3I(5, 0, 3), new Vector3I(4, 0, 2), new Vector3I(4, 0, 4) })
            placed.Add((wall, 0));
        var (grid, set, initial) = Setup(GridTopology.Create2D(8, 8), Flat, placed.ToArray());
        var error = Assert.Single(PathRouter.Plan(grid, set, initial, P, 0.3, 1).Errors);
        Assert.Contains("No route joins", error.Message);
    }
}
