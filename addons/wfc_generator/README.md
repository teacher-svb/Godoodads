# WFC Generator

Fill a `GridMap` with modules using wave function collapse, straight from the Godot editor. You describe which modules may sit next to each other by labelling their faces with sockets, choose a box of cells, and press **Generate**. Generation works in true 3D, so modules stack and connect vertically as well as horizontally.

![A stacked hillside with ruins, generated from the demo tile set](docs/hillside.png)

**Requirements:** Godot 4.7 or later, .NET build.

## Install

1. Copy `addons/wfc_generator/` and `addons/editor_tools_common/` into your project's `addons/` folder.
2. If your project has no C# solution yet, create one with *Project → Tools → C# → Create C# solution*.
3. Build the C# project (the hammer icon, or `dotnet build`).
4. Enable **WFC Generator** in *Project → Project Settings → Plugins*.

## Use

1. Create a **tile set**: a `WfcTileSet` resource that points at your `MeshLibrary` and lists the sockets of each module (see below).
2. Select a `GridMap` that uses the same `MeshLibrary`.
3. In the 3D viewport toolbar, open **GridMap → Generate WFC (GDDS)**. GDDS is short for Godoodads, the suite this addon belongs to.
4. Choose the **Tile Set** and the **Bounds**, which are drawn in orange in the viewport, then press **Generate**.

The solve runs in the background, and you can watch the cells fill in. Press **Stop** to cancel; the GridMap is then left exactly as it was. Each generation is one undo step (Ctrl+Z). The settings are saved on the GridMap, so reopening the dialog restores them and the scene file can reproduce the result. The status area shows the result:

```
Solved 160,000 cells in 3.9 s (0 backtracks, 0 restarts). Seed 7.
Kept 10,000 existing cells.
```

### Cells you place yourself

Cells already in the bounds stay where they are, and the solve fills in around them. Use this to paint a ground layer, a road or a landmark first. Turn on **Clear Bounds First** to replace everything instead.

A cell whose item has no tile, or whose rotation is not one the tile allows, is left alone and does not constrain its neighbours. The status area reports how many there were.

Generated cells also count as placed cells the next time. To try another seed, undo first or turn on Clear Bounds First.

## Options

| Option | What it does |
|---|---|
| Tile Set | The `WfcTileSet` to generate with. Its `MeshLibrary` must be the GridMap's. |
| Seed, Randomise Seed | The same seed, settings and placed cells give the same result. |
| Clear Bounds First | Removes everything in the bounds before solving. Otherwise placed cells are kept. |
| Bounds → Origin, Size | The box of cells to fill, in GridMap cell coordinates. |
| Animation → Cells Per Frame | How fast the solve is shown. 0 shows only the final result. The result never depends on this. About 1,000 keeps the editor responsive. |
| Solver Limits → Max Backtracks | Backtracks allowed per attempt before starting over with a new seed derived from yours. |
| Solver Limits → Max Restarts | Attempts after the first one before giving up. |

## Tile sets

A `WfcTileSet` holds a `MeshLibrary` and a list of `WfcTile` entries. The rules live in the tile set; your `MeshLibrary` is never modified.

| Tile field | What it does |
|---|---|
| Item | The MeshLibrary item this tile places, or -1 for an empty tile (air). |
| Rotation | **None**, **Half Turn** (0° and 180°) or **Quarter Turns** (all four). Rotations whose sockets are identical are merged. Straight pieces such as walls need Quarter Turns to run in both directions. |
| Sockets | One label per face: +X, -X, +Y, -Y, +Z, -Z, as the module is modelled. |

The tile set's inspector shows a preview: the selected tile in 3D with its socket labels (drag to orbit), the tiles allowed next to each face, and a check for problems.

<img src="docs/preview.png" alt="The tile set preview: a stair module with its socket labels and valid neighbours" width="560">

### Sockets

Two facing sockets connect when their labels match:

| Face | Label | Connects to |
|---|---|---|
| Side, symmetric | `path` | `path` |
| Side, one-sided | `path+` | `path-`, and the other way round |
| Top or bottom | `stair` | `stair`, in any rotation |
| Top or bottom, rotation-specific | `stair@0` to `stair@3` | the same label with the same number, after the tile's rotation is applied |

Labels use letters, digits and underscores. Every face needs a label.

### Designing a tile set

- **Give empty space real tiles.** Air is a tile with Item -1. A useful pattern: one air tile that rests on `ground` and one that rests on `air`, so modules can only stand on surfaces you mark as `ground`, and nothing floats.
- **Control density with copies and variants.** v1 has no tile weights: every remaining option is equally likely, and each rotation counts as an option. To make something more common, add copies of it (empty tiles can repeat freely) or extra variants with their own items, such as several grass tiles. The demo uses 40 copies of each air tile and 16 grass variants.
- **Keep requirements local.** Rules only see direct neighbours. A bridge that may run for any length but must end on a pillar often fails at large sizes. Give it a fixed length instead (head, deck, deck, head), using one-sided sockets so a deck can only meet a head at its outer end.
- **Use one-sided sockets for direction.** `span+` against `span-` stops a module from connecting to a rotated copy of itself.
- **Anchor the ground.** Edges are unconstrained, so empty columns can reach the bottom. Paint a ground layer, or design sockets that keep air above ground.

### The tile set check

| Message | Meaning |
|---|---|
| Socket has no partner | No tile in any rotation can face this socket. The tile can only appear where that face is at the edge of the bounds. |
| Only fits at the edges of the grid | The tile depends on tiles that cannot be placed themselves. |

## When a solve fails

- **No solution exists with the cells already in the bounds.** Two placed cells do not fit together, or nothing fits between them. The cells are named in the status area and marked red in the viewport.
- **This tile set has no solution for these bounds.** The search proved it, without placed cells being involved. Check the tile set warnings.
- **Ran out of attempts.** A solution may still exist. Try another seed, or raise the limits.

In every case the GridMap is left unchanged.

## How it works

1. Each tile becomes one variant per allowed rotation, and an adjacency table records which variants may face each other in each direction.
2. Every cell starts with all variants as options. Placed cells start with one.
3. The cell with the fewest options (ties broken by the seed) is set to one of its options, chosen uniformly at random, and the options of its neighbours are narrowed, repeatedly.
4. When a cell runs out of options, the solver backtracks: it undoes its last choice and rules that option out. After Max Backtracks, it starts over with a seed derived from yours.
5. The result is written to the GridMap as one undoable action.

The solver runs on a background thread and touches no engine objects, so the editor stays responsive. A rebuild of the C# project cancels a running solve first.

**Performance** (100 × 16 × 100 cells, demo tile set with 144 variants): about 3.9 s with a placed ground layer and 5.2 s without, with every one of 20 seeds solved first time.

## Demo

`demo/wfc_generator/hillside_ground.tscn` has a placed rock layer, and `hillside_free.tscn` starts empty. Both use the stacked hillside ruin tile set in `demo/wfc_generator/assets/`, which `demo/wfc_generator/tools/build_demo.tscn` rebuilds.

## Limitations

- Editor-only. There is no runtime generation yet.
- No tile weights. Use copies and variants instead (see above).
- Rotation is around Y only, and one item fills one cell. Mirrored modules need their own items.
- Rules are local. Connected paths and reachability are not guaranteed.
- The edges of the bounds are unconstrained.
- While a solve is being shown, the GridMap holds a preview. Saving the scene at that moment saves the preview.
- 3D `GridMap` only. The solver also supports 2D, but there is no `TileMapLayer` support yet.
