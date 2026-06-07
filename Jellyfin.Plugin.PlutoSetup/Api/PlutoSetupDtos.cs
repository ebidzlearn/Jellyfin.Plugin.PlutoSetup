using Jellyfin.Plugin.PlutoSetup.Configuration;

namespace Jellyfin.Plugin.PlutoSetup.Api;

public class SaveConfigRequest
{
    public string? Mode { get; set; }

    public string? HostedM3uUrl { get; set; }

    public string? HostedXmltvUrl { get; set; }

    public string? SelectedIpAddress { get; set; }

    public string? ManualHostOverride { get; set; }

    public int? DockerPort { get; set; }

    public int? StartChannelNumber { get; set; }
}

public sealed class ValidateUrlsRequest : SaveConfigRequest
{
    public bool AllowPrivateNetwork { get; set; }
}

public sealed class DockerCommandRequest : SaveConfigRequest
{
    public string? UserName { get; set; }

    public string? Password { get; set; }

    public bool RevealPassword { get; set; }

    public string? Shell { get; set; }
}

public sealed class AutoAddRequest : SaveConfigRequest
{
    public bool ConfirmExistingGuideProviderConflict { get; set; }
}

public sealed class TestDockerUrlRequest : SaveConfigRequest
{
    public bool ConfirmContainerIsRunning { get; set; }

    public bool AllowPrivateNetwork { get; set; } = true;
}

public sealed class SystemInfoResponse
{
    public string JellyfinVersion { get; set; } = "Unknown";

    public string BaseUrl { get; set; } = string.Empty;

    public int? JellyfinPort { get; set; }

    public IReadOnlyList<string> LocalIpv4Addresses { get; set; } = [];

    public PlutoSetupConfigDto Config { get; set; } = new();

    public GeneratedUrlSet GeneratedUrls { get; set; } = new();

    public NativeAvailabilityDto NativeAvailability { get; set; } = new();

    public AutoAddAvailabilityDto AutoAddAvailability { get; set; } = new();

    public IReadOnlyList<LiveTvItemDto> ExistingTuners { get; set; } = [];

    public IReadOnlyList<LiveTvItemDto> ExistingGuideProviders { get; set; } = [];

    public IReadOnlyList<int> AvailableDockerPorts { get; set; } = [];
}

public sealed class PlutoSetupConfigDto
{
    public string Mode { get; set; } = PlutoSetupMode.HostedNoDocker.ToString();

    public string HostedM3uUrl { get; set; } = PlutoSetupDefaults.HostedM3uUrl;

    public string HostedXmltvUrl { get; set; } = PlutoSetupDefaults.HostedXmltvUrl;

    public string SelectedIpAddress { get; set; } = string.Empty;

    public string ManualHostOverride { get; set; } = string.Empty;

    public int DockerPort { get; set; } = PlutoSetupDefaults.DefaultDockerPort;

    public int StartChannelNumber { get; set; } = PlutoSetupDefaults.DefaultStartChannelNumber;

    public DateTimeOffset? LastSuccessfulValidationUtc { get; set; }
}

public sealed class GeneratedUrlSet
{
    public string M3uUrl { get; set; } = string.Empty;

    public string XmltvUrl { get; set; } = string.Empty;

    public IReadOnlyList<string> DockerTunerUrls { get; set; } = [];

    public IReadOnlyList<string> NativeTunerUrls { get; set; } = [];

    public string ModeNote { get; set; } = string.Empty;
}

public sealed class NativeAvailabilityDto
{
    public bool IsAvailable { get; set; }

    public string Status { get; set; } = "Unavailable";

    public string Message { get; set; } = "Native no-Docker mode is disabled because real Pluto authentication, playlist, and XMLTV generation have not been implemented and tested.";
}

public sealed class AutoAddAvailabilityDto
{
    public bool IsAvailable { get; set; }

    public string Status { get; set; } = "Unavailable";

    public string Message { get; set; } = "Safe Jellyfin Live TV auto-add support is not enabled by this MVP. Use the manual setup steps.";

    public bool ExistingGuideProviderWarning { get; set; }
}

public sealed class LiveTvItemDto
{
    public string Name { get; set; } = "Unknown";

    public string Type { get; set; } = "Unknown";

    public string Url { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;
}

public sealed class ValidateUrlsResponse
{
    public ValidationResultDto M3u { get; set; } = new();

    public ValidationResultDto Xmltv { get; set; } = new();

    public bool AllReachable { get; set; }

    public DateTimeOffset? LastSuccessfulValidationUtc { get; set; }
}

public sealed class ValidationResultDto
{
    public string Url { get; set; } = string.Empty;

    public string Status { get; set; } = "Unknown";

    public string Message { get; set; } = string.Empty;

    public bool IsPrivateOrLocalAddress { get; set; }
}

public sealed class DockerCommandResponse
{
    public string Bash { get; set; } = string.Empty;

    public string PowerShell { get; set; } = string.Empty;

    public string Cmd { get; set; } = string.Empty;

    public bool PasswordIncluded { get; set; }

    public bool RequiresCurrentPassword { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class ActionResultDto
{
    public bool Success { get; set; }

    public string Status { get; set; } = "Unavailable";

    public string Message { get; set; } = string.Empty;

    public IReadOnlyList<string> ManualSetupSteps { get; set; } =
    [
        "Go to Dashboard > Live TV > Tuner Devices > Add Tuner Device. Set Tuner Type to M3U Tuner. Paste the generated M3U URL into File or URL.",
        "Go to Dashboard > Live TV > TV Guide Data Providers > Add Provider. Select XMLTV. Paste the generated XMLTV URL into File or URL.",
        "Save and refresh guide data.",
        "Map channels if Jellyfin requires guide/channel mapping."
    ];
}
