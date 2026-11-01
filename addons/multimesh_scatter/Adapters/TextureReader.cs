using Godot;

namespace Godoodads.MultiMeshScatter.Adapters;

/// <summary>Reads textures as RGBA8 bytes, always on a copy so the user's resources stay untouched.</summary>
public static class TextureReader
{
    public readonly record struct Rgba8(int Width, int Height, byte[] Data, Image.Format SourceFormat);

    /// <summary>Returns null and sets <paramref name="error"/> if the texture cannot be read.</summary>
    public static Rgba8? Read(Texture2D texture, string role, out string error)
    {
        // GetImage returns a copy, so decompressing or converting never touches the user's texture.
        var image = texture?.GetImage();
        if (image == null || image.IsEmpty())
        {
            error = $"The {role} '{Describe(texture)}' has no readable image data.";
            return null;
        }
        return Read(image, role, out error, copy: false);
    }

    public static Rgba8? Read(Image image, string role, out string error, bool copy = true)
    {
        error = null;
        var sourceFormat = image.GetFormat();
        if (copy)
            image = (Image)image.Duplicate();
        if (image.IsCompressed() && image.Decompress() != Error.Ok)
        {
            error = $"The {role} uses a compressed format ({sourceFormat}) that cannot be decompressed. Import it as Lossless or Uncompressed.";
            return null;
        }
        if (image.GetFormat() != Image.Format.Rgba8)
            image.Convert(Image.Format.Rgba8);
        return new Rgba8(image.GetWidth(), image.GetHeight(), image.GetData(), sourceFormat);
    }

    /// <summary>True for formats that store only red and green, such as Godot's BC5 normal map import.</summary>
    public static bool IsTwoChannel(Image.Format format) => format is Image.Format.Rg8 or Image.Format.Rgh
        or Image.Format.Rgf or Image.Format.RgtcRg or Image.Format.Etc2Rg11 or Image.Format.Etc2Rg11S;

    private static string Describe(Resource resource) =>
        resource == null ? "(none)" : string.IsNullOrEmpty(resource.ResourcePath) ? resource.GetClass() : resource.ResourcePath;
}
