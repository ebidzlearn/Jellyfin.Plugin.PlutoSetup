using System.Runtime.CompilerServices;
using Xunit;

namespace Jellyfin.Plugin.PlutoSetup.Tests;

public sealed class PluginMetadataTests
{
    [Fact]
    public void PluginMetadataMatchesReleaseManifestIdentity()
    {
        var plugin = (Plugin)RuntimeHelpers.GetUninitializedObject(typeof(Plugin));

        Assert.Equal("Pluto TV Auto Tuner", plugin.Name);
        Assert.Equal(Guid.Parse("0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b"), plugin.Id);
    }
}
