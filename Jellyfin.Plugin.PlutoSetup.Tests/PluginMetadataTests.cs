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

    [Fact]
    public void UninstallCleanupTargetsOnlySiblingPluginVersionFolders()
    {
        var root = Directory.CreateTempSubdirectory("pluto-plugin-test-");
        try
        {
            var oldVersion = Directory.CreateDirectory(Path.Combine(root.FullName, "Pluto TV Auto Tuner_0.1.0.1"));
            var currentVersion = Directory.CreateDirectory(Path.Combine(root.FullName, "Pluto TV Auto Tuner_0.1.0.4"));
            _ = Directory.CreateDirectory(Path.Combine(root.FullName, "Other Plugin_1.0.0.0"));
            File.WriteAllText(Path.Combine(currentVersion.FullName, "Jellyfin.Plugin.PlutoSetup.dll"), string.Empty);

            var targets = Plugin.GetSiblingVersionDirectoriesForRemoval(
                Path.Combine(currentVersion.FullName, "Jellyfin.Plugin.PlutoSetup.dll"));

            var target = Assert.Single(targets);
            Assert.Equal(oldVersion.FullName, target);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
