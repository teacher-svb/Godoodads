using Godot;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.MultiMeshScatter.Adapters;

/// <summary>Reads a normal map texture into a <see cref="NormalMap"/>, working on a copy.</summary>
public static class NormalMapSource
{
    private const string Role = "normal map";

    public static NormalMap FromTexture(Texture2D texture, NormalMapSpace space, bool flipGreen, out string error) =>
        Build(TextureReader.Read(texture, Role, out error), space, flipGreen, ref error);

    public static NormalMap FromImage(Image image, NormalMapSpace space, bool flipGreen, out string error) =>
        Build(TextureReader.Read(image, Role, out error), space, flipGreen, ref error);

    private static NormalMap Build(TextureReader.Rgba8? pixels, NormalMapSpace space, bool flipGreen, ref string error)
    {
        if (pixels is not { } p)
            return null;
        // Tangent space rebuilds z from x and y, like Godot; object space needs the stored z.
        if (space == NormalMapSpace.Object && TextureReader.IsTwoChannel(p.SourceFormat))
        {
            error = $"The normal map is stored with only red and green ({p.SourceFormat}), which object-space maps cannot use. "
                + "Re-import it without the Normal Map compression mode, or use a tangent-space map.";
            return null;
        }
        return NormalMap.FromRgba8(p.Width, p.Height, p.Data, flipGreen);
    }
}
