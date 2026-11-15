using Godoodads.Dungeon.Core;
using Xunit;

namespace Godoodads.Tests;

public class DungeonWfcVersionTests
{
    [Theory]
    [InlineData("1.3.2", true)]
    [InlineData("1.0.0", true)]
    [InlineData("1.3.3", false)]
    [InlineData("1.4.0", false)]
    [InlineData("2.0", false)]
    [InlineData(" 1.3.1 ", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("1.3.3-beta", false)]
    public void Older_wfc_generator_versions_need_an_update(string installed, bool expected) =>
        Assert.Equal(expected, WfcGeneratorVersion.NeedsUpdate(installed));
}
