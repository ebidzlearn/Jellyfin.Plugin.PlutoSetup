using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Jellyfin.Plugin.PlutoSetup.Api;
using Jellyfin.Plugin.PlutoSetup.Configuration;
using MediaBrowser.Common;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public sealed class SystemInfoService
{
    private readonly IApplicationHost _applicationHost;
    private readonly ModeSelectionService _modeSelectionService;
    private readonly NativeProviderAvailabilityService _nativeProviderAvailabilityService;
    private readonly LiveTvAutoSetupService _liveTvAutoSetupService;
    private readonly PortScannerService _portScannerService;

    public SystemInfoService(
        IApplicationHost applicationHost,
        ModeSelectionService modeSelectionService,
        NativeProviderAvailabilityService nativeProviderAvailabilityService,
        LiveTvAutoSetupService liveTvAutoSetupService,
        PortScannerService portScannerService)
    {
        _applicationHost = applicationHost;
        _modeSelectionService = modeSelectionService;
        _nativeProviderAvailabilityService = nativeProviderAvailabilityService;
        _liveTvAutoSetupService = liveTvAutoSetupService;
        _portScannerService = portScannerService;
    }

    public async Task<SystemInfoResponse> GetSystemInfoAsync(HttpRequest request, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        var localAddresses = GetLocalIpv4Addresses();
        var nativeAvailability = _nativeProviderAvailabilityService.GetAvailability();
        var generatedUrls = _modeSelectionService.GetGeneratedUrls(configuration, request, localAddresses);
        var liveTvSnapshot = _liveTvAutoSetupService.GetSnapshot();
        var availablePorts = configuration.Mode == PlutoSetupMode.DockerHelper
            ? await _portScannerService.FindAvailablePortsAsync(cancellationToken).ConfigureAwait(false)
            : [];

        return new SystemInfoResponse
        {
            JellyfinVersion = GetJellyfinVersion(),
            BaseUrl = ModeSelectionService.GetRequestBaseUrl(request),
            JellyfinPort = request.Host.Port,
            LocalIpv4Addresses = localAddresses,
            Config = ToDto(configuration),
            GeneratedUrls = generatedUrls,
            NativeAvailability = nativeAvailability,
            AutoAddAvailability = liveTvSnapshot.AutoAddAvailability,
            ExistingTuners = liveTvSnapshot.ExistingTuners,
            ExistingGuideProviders = liveTvSnapshot.ExistingGuideProviders,
            AvailableDockerPorts = availablePorts
        };
    }

    public static PlutoSetupConfigDto ToDto(PluginConfiguration configuration)
    {
        return new PlutoSetupConfigDto
        {
            Mode = configuration.Mode.ToString(),
            HostedM3uUrl = string.IsNullOrWhiteSpace(configuration.HostedM3uUrl) ? PlutoSetupDefaults.HostedM3uUrl : configuration.HostedM3uUrl,
            HostedXmltvUrl = string.IsNullOrWhiteSpace(configuration.HostedXmltvUrl) ? PlutoSetupDefaults.HostedXmltvUrl : configuration.HostedXmltvUrl,
            SelectedIpAddress = configuration.SelectedIpAddress ?? string.Empty,
            ManualHostOverride = configuration.ManualHostOverride ?? string.Empty,
            DockerPort = configuration.DockerPort,
            StartChannelNumber = configuration.StartChannelNumber,
            LastSuccessfulValidationUtc = configuration.LastSuccessfulValidationUtc
        };
    }

    public static IReadOnlyList<string> GetLocalIpv4Addresses()
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(addr => addr.Address)
            .Where(address => !IPAddress.IsLoopback(address))
            .Select(address => address.ToString())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(address => address, StringComparer.Ordinal)
            .ToArray();

        if (addresses.Length > 0)
        {
            return addresses;
        }

        return [IPAddress.Loopback.ToString()];
    }

    private string GetJellyfinVersion()
    {
        if (!string.IsNullOrWhiteSpace(_applicationHost.ApplicationVersionString))
        {
            return _applicationHost.ApplicationVersionString;
        }

        return _applicationHost.ApplicationVersion.ToString();
    }
}
