namespace Godoodads.Tests.MultiMeshScatter;

using Godot;
using GdUnit4;
using Godoodads.MultiMeshScatter.Adapters;
using static GdUnit4.Assertions;

[TestSuite]
public class MaskSourceTests
{
    private static Image Pixel(Color color)
    {
        var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        image.Fill(color);
        return image;
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Channels_are_extracted()
    {
        var image = Pixel(new Color(1f, 0.5f, 0f, 0.25f));
        AssertThat(MaskSource.FromImage(image, MaskChannel.Red, out _)[0, 0]).IsEqualApprox(1f, 0.01f);
        AssertThat(MaskSource.FromImage(image, MaskChannel.Green, out _)[0, 0]).IsEqualApprox(0.5f, 0.01f);
        AssertThat(MaskSource.FromImage(image, MaskChannel.Blue, out _)[0, 0]).IsEqualApprox(0f, 0.01f);
        AssertThat(MaskSource.FromImage(image, MaskChannel.Alpha, out _)[0, 0]).IsEqualApprox(0.25f, 0.01f);
        AssertThat(MaskSource.FromImage(Pixel(Colors.White), MaskChannel.Luminance, out _)[0, 0]).IsEqualApprox(1f, 0.01f);
        AssertThat(MaskSource.FromImage(Pixel(new Color(0, 1, 0)), MaskChannel.Luminance, out _)[0, 0]).IsEqualApprox(0.7152f, 0.01f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Other_formats_and_rows_map_correctly()
    {
        var image = Image.CreateEmpty(2, 2, false, Image.Format.L8);
        image.SetPixel(1, 0, Colors.White); // top right
        var mask = MaskSource.FromImage(image, MaskChannel.Red, out var error);
        AssertThat(error == null).IsTrue();
        AssertThat(mask[1, 0]).IsEqualApprox(1f, 0.01f);
        AssertThat(mask[0, 1]).IsEqualApprox(0f, 0.01f);
        AssertThat(image.GetFormat()).IsEqual(Image.Format.L8); // the source image is not converted
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Imported_demo_texture_is_readable()
    {
        var texture = GD.Load<Texture2D>("res://demo/multimesh_scatter/assets/mask_checker.png");
        var mask = MaskSource.FromTexture(texture, MaskChannel.Luminance, out var error);
        AssertThat(error == null).OverrideFailureMessage(error ?? "no error").IsTrue();
        AssertThat(mask.Width).IsEqual(256);
        AssertThat(mask[0, 0]).IsEqualApprox(1f, 0.05f);   // top-left cell is white
        AssertThat(mask[40, 0]).IsEqualApprox(0f, 0.05f);  // next cell is black
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compressed_images_are_decompressed_on_a_copy()
    {
        var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        image.Fill(new Color(0.5f, 0.5f, 0.5f));
        var compressed = (Image)image.Duplicate();
        AssertThat(compressed.Compress(Image.CompressMode.S3Tc)).IsEqual(Error.Ok);
        AssertThat(compressed.IsCompressed()).IsTrue();

        var mask = MaskSource.FromImage(compressed, MaskChannel.Luminance, out var error);
        AssertThat(error == null).IsTrue();
        AssertThat(mask[8, 8]).IsEqualApprox(0.5f, 0.03f);
        AssertThat(compressed.IsCompressed()).IsTrue();
    }
}
