using System.Collections.Generic;
using Godot;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Adapters;

/// <summary>A marker scene to place at a cell, with its transform in GridMap space.</summary>
public sealed record MarkerPlacement(Vector3I Cell, PackedScene Scene, Transform3D Transform);

/// <summary>
/// Marker tiles (brief §12.4): scenes placed at generated cells, in a container child of the
/// GridMap. Placed markers carry metadata, so the next generation can find and replace them.
/// </summary>
public static class GridMapMarkers
{
    public const string ContainerName = "WfcMarkers";
    public const string TagKey = "godoodads_wfc_marker";

    /// <summary>The markers for the cells this generation writes (free cells only).</summary>
    public static List<MarkerPlacement> Plan(GridMap map, Vector3I origin, GridTopology grid, TileSetBinding binding, int[] initial, int[] solved)
    {
        var placements = new List<MarkerPlacement>();
        for (int cell = 0; cell < grid.CellCount; cell++)
        {
            if (initial[cell] != WfcSolver.Free)
                continue;
            var variant = binding.Compiled.Variants[solved[cell]];
            var scene = binding.Markers[variant.TileIndex];
            if (scene == null)
                continue;
            var position = origin + grid.Coordinates(cell);
            var basis = map.GetBasisWithOrthogonalIndex(GridMapOrientation.FromQuarterTurns(variant.QuarterTurns));
            placements.Add(new MarkerPlacement(position, scene, new Transform3D(basis, map.MapToLocal(position))));
        }
        return placements;
    }

    /// <summary>The container of a GridMap's markers, or null.</summary>
    public static Node3D Container(GridMap map) => map.GetNodeOrNull<Node3D>(ContainerName);

    /// <summary>
    /// The placed markers that this generation replaces: tagged markers whose cell, wherever the
    /// marker is now, is a free cell in the bounds. Markers on cells that stay (placed, edited or
    /// pinned) are kept with them.
    /// </summary>
    public static List<Node3D> Replaced(GridMap map, Vector3I origin, GridTopology grid, int[] initial)
    {
        var replaced = new List<Node3D>();
        var container = Container(map);
        if (container == null)
            return replaced;
        foreach (var child in container.GetChildren())
        {
            if (child is not Node3D marker || !marker.HasMeta(TagKey))
                continue;
            var cell = map.LocalToMap(container.Transform * marker.Position) - origin;
            if (grid.Contains(cell) && initial[grid.Index(cell)] == WfcSolver.Free)
                replaced.Add(marker);
        }
        return replaced;
    }

    /// <summary>
    /// Instances a placement as a tagged marker, not yet in the tree. The container sits at the
    /// GridMap's origin, so GridMap space is container space. Null when the scene's root is not a Node3D.
    /// </summary>
    public static Node3D Instantiate(MarkerPlacement placement)
    {
        var node = placement.Scene.Instantiate();
        if (node is not Node3D marker)
        {
            node.Free();
            return null;
        }
        marker.Transform = placement.Transform;
        marker.SetMeta(TagKey, true);
        return marker;
    }
}
