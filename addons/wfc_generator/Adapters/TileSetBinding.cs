using System.Collections.Generic;
using Godot;
using Godoodads.WfcGenerator.Core;

namespace Godoodads.WfcGenerator.Adapters;

/// <summary>A tile set resource compiled for the solver, with each tile's MeshLibrary item.</summary>
public sealed class TileSetBinding
{
    private readonly Dictionary<int, int> _tileForItem = new();

    public CompiledTileSet Compiled { get; private set; }
    public int[] Items { get; private set; }
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;

    /// <summary>The tile index using a MeshLibrary item, or -1.</summary>
    public int TileForItem(int item) => _tileForItem.TryGetValue(item, out int tile) ? tile : -1;

    public static TileSetBinding Create(WfcTileSet tileSet, int[] directions)
    {
        var binding = new TileSetBinding();
        if (tileSet.Library == null)
            binding.Errors.Add("The tile set has no MeshLibrary.");

        var definitions = new List<TileDefinition>();
        var items = new List<int>();
        for (int i = 0; i < tileSet.Tiles.Count; i++)
        {
            var tile = tileSet.Tiles[i];
            if (tile == null)
            {
                binding.Errors.Add($"Tile {i} is empty.");
                continue;
            }
            binding.CheckItem(tileSet.Library, tile.Item, items.Count, i);
            definitions.Add(tile.ToDefinition());
            items.Add(tile.Item);
        }

        binding.Items = items.ToArray();
        binding.Compiled = CompiledTileSet.Compile(definitions, directions);
        foreach (var issue in binding.Compiled.Errors)
            binding.Errors.Add(issue.TileIndex < 0
                ? issue.Message
                : $"Tile {issue.TileIndex} ({Directions.Name(issue.Direction)}): {issue.Message}");
        return binding;
    }

    private void CheckItem(MeshLibrary library, int item, int tileIndex, int listIndex)
    {
        if (item == WfcTile.EmptyItem)
            return;
        if (library != null && System.Array.IndexOf(library.GetItemList(), item) < 0)
            Errors.Add($"Tile {listIndex} uses item {item}, which is not in the MeshLibrary.");
        if (!_tileForItem.TryAdd(item, tileIndex))
            Errors.Add($"Item {item} is used by more than one tile.");
    }
}
