using Jellyfin.Plugin.PlutoSetup.Api;
using Jellyfin.Plugin.PlutoSetup.Configuration;
using Jellyfin.Plugin.PlutoSetup.Services;
using Xunit;

namespace Jellyfin.Plugin.PlutoSetup.Tests;

public sealed class DockerCommandServiceTests
{
    [Fact]
    public void MaskedPreviewDoesNotIncludeSubmittedPassword()
    {
        var service = new DockerCommandService();
        var response = service.BuildCommand(BuildConfig(), new DockerCommandRequest
        {
            UserName = "user@example.com",
            Password = "secret",
            RevealPassword = false
        });

        Assert.False(response.PasswordIncluded);
        Assert.DoesNotContain("secret", response.Bash, StringComparison.Ordinal);
        Assert.Contains("********", response.Bash, StringComparison.Ordinal);
    }

    [Fact]
    public void RevealRequiresCurrentSessionPassword()
    {
        var service = new DockerCommandService();
        var response = service.BuildCommand(BuildConfig(), new DockerCommandRequest
        {
            UserName = "user@example.com",
            RevealPassword = true
        });

        Assert.True(response.RequiresCurrentPassword);
        Assert.Empty(response.Bash);
    }

    [Fact]
    public void DockerCommandEscapesShellSpecificCredentials()
    {
        var service = new DockerCommandService();
        var response = service.BuildCommand(BuildConfig(), new DockerCommandRequest
        {
            UserName = "user'o@example.com",
            Password = "p$\"%word",
            RevealPassword = true
        });

        Assert.Contains("user'\"'\"'o@example.com", response.Bash, StringComparison.Ordinal);
        Assert.Contains("p$\"%word", response.Bash, StringComparison.Ordinal);
        Assert.Contains("p`$`\"%word", response.PowerShell, StringComparison.Ordinal);
        Assert.Contains("p$\"\"%%word", response.Cmd, StringComparison.Ordinal);
    }

    private static PluginConfiguration BuildConfig()
    {
        return new PluginConfiguration
        {
            Mode = PlutoSetupMode.DockerHelper,
            DockerPort = 8090,
            StartChannelNumber = 10000
        };
    }
}
