using Godot;
using Godoodads.MultiMeshScatter.Core;

namespace Godoodads.MultiMeshScatter.Adapters;

public enum MaskChannel { Luminance, Red, Green, Blue, Alpha }

/// <summary>Reads one channel of a texture into a <see cref="MaskImage"/>, working on a copy.</summary>
public static class MaskSource
{
    private const string Role = "mask texture";

    /// <summary>Returns null and sets <paramref name="error"/> if the texture cannot be read.</summary>
    public static MaskImage FromTexture(Texture2D texture, MaskChannel channel, out string error) =>
        Build(TextureReader.Read(texture, Role, out error), channel);

    public static MaskImage FromImage(Image image, MaskChannel channel, out string error) =>
        Build(TextureReader.Read(image, Role, out error), channel);

    private static MaskImage Build(TextureReader.Rgba8? pixels, MaskChannel channel)
    {
        if (pixels is not { } p)
            return null;
        var values = new float[p.Width * p.Height];
        for (int i = 0; i < values.Length; i++)
        {
            int o = i * 4;
            values[i] = channel switch
            {
                MaskChannel.Red => p.Data[o] / 255f,
                MaskChannel.Green => p.Data[o + 1] / 255f,
                MaskChannel.Blue => p.Data[o + 2] / 255f,
                MaskChannel.Alpha => p.Data[o + 3] / 255f,
                // Rec. 709 weights on the stored values: masks are data, not colours.
                _ => (0.2126f * p.Data[o] + 0.7152f * p.Data[o + 1] + 0.0722f * p.Data[o + 2]) / 255f,
            };
        }
        return new MaskImage(p.Width, p.Height, values);
    }
}
