using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PlutoSetup.Configuration;

public enum PlutoSetupMode
{
    HostedNoDocker,
    DockerHelper,
    NativeNoDocker
}

public sealed class PluginConfiguration : BasePluginConfiguration
{
    public PlutoSetupMode Mode { get; set; } = PlutoSetupMode.HostedNoDocker;

    public string HostedM3uUrl { get; set; } = PlutoSetupDefaults.HostedM3uUrl;

    public string HostedXmltvUrl { get; set; } = PlutoSetupDefaults.HostedXmltvUrl;

    public string SelectedIpAddress { get; set; } = string.Empty;

    public string ManualHostOverride { get; set; } = string.Empty;

    public int DockerPort { get; set; } = PlutoSetupDefaults.DefaultDockerPort;

    public int StartChannelNumber { get; set; } = PlutoSetupDefaults.DefaultStartChannelNumber;

    public DateTimeOffset? LastSuccessfulValidationUtc { get; set; }
}
