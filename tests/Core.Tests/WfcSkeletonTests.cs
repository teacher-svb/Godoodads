using Godoodads.WfcGenerator.Core;
using Xunit;

namespace Godoodads.Tests;

public class WfcSkeletonTests
{
    [Fact]
    public void CoreFolderIsCompiledIntoTests() => Assert.Equal("WFC Generator", WfcInfo.AddonName);
}
