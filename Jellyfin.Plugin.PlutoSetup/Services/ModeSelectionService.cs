using Jellyfin.Plugin.PlutoSetup.Api;
using Jellyfin.Plugin.PlutoSetup.Configuration;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public sealed class ModeSelectionService
{
    private readonly HostedUrlService _hostedUrlService;

    public ModeSelectionService(HostedUrlService hostedUrlService)
    {
        _hostedUrlService = hostedUrlService;
    }

    public GeneratedUrlSet GetGeneratedUrls(PluginConfiguration configuration, HttpRequest request, IReadOnlyList<string>? localIpv4Addresses = null)
    {
        return configuration.Mode switch
        {
            PlutoSetupMode.DockerHelper => GetDockerUrls(configuration, localIpv4Addresses ?? SystemInfoService.GetLocalIpv4Addresses()),
            PlutoSetupMode.NativeNoDocker => GetNativeUrls(request),
            _ => GetHostedUrls(configuration)
        };
    }

    public static string GetRequestBaseUrl(HttpRequest request)
    {
        var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;
        return $"{request.Scheme}://{request.Host}{pathBase}";
    }

    public PluginConfiguration Merge(PluginConfiguration current, SaveConfigRequest request)
    {
        var configuration = new PluginConfiguration
        {
            Mode = ParseMode(request.Mode, current.Mode),
            HostedM3uUrl = string.IsNullOrWhiteSpace(request.HostedM3uUrl) ? current.HostedM3uUrl : request.HostedM3uUrl.Trim(),
            HostedXmltvUrl = string.IsNullOrWhiteSpace(request.HostedXmltvUrl) ? current.HostedXmltvUrl : request.HostedXmltvUrl.Trim(),
            SelectedIpAddress = request.SelectedIpAddress?.Trim() ?? current.SelectedIpAddress,
            ManualHostOverride = request.ManualHostOverride?.Trim() ?? current.ManualHostOverride,
            DockerPort = SanitizePort(request.DockerPort ?? current.DockerPort),
            StartChannelNumber = SanitizeStart(request.StartChannelNumber ?? current.StartChannelNumber),
            LastSuccessfulValidationUtc = current.LastSuccessfulValidationUtc
        };

        configuration.HostedM3uUrl = _hostedUrlService.GetM3uUrl(configuration.HostedM3uUrl);
        configuration.HostedXmltvUrl = _hostedUrlService.GetXmltvUrl(configuration.HostedXmltvUrl);

        return configuration;
    }

    public static PlutoSetupMode ParseMode(string? value, PlutoSetupMode fallback)
    {
        return Enum.TryParse<PlutoSetupMode>(value, ignoreCase: true, out var mode) ? mode : fallback;
    }

    public static int SanitizePort(int port)
    {
        return port is >= 1 and <= 65535 ? port : PlutoSetupDefaults.DefaultDockerPort;
    }

    public static int SanitizeStart(int start)
    {
        return start is >= 1 and <= 999999 ? start : PlutoSetupDefaults.DefaultStartChannelNumber;
    }

    private GeneratedUrlSet GetHostedUrls(PluginConfiguration configuration)
    {
        return new GeneratedUrlSet
        {
            M3uUrl = _hostedUrlService.GetM3uUrl(configuration.HostedM3uUrl),
            XmltvUrl = _hostedUrlService.GetXmltvUrl(configuration.HostedXmltvUrl),
            ModeNote = "Hosted no-Docker mode uses third-party public URLs directly. Pluto credentials and START are not used."
        };
    }

    private static GeneratedUrlSet GetDockerUrls(PluginConfiguration configuration, IReadOnlyList<string> localIpv4Addresses)
    {
        var host = GetExternalHost(configuration, localIpv4Addresses);
        var baseUrl = $"http://{host}:{configuration.DockerPort}";
        var tunerUrls = Enumerable.Range(1, PlutoSetupDefaults.DockerTunerCount)
            .Select(index => $"{baseUrl}/tuner-{index}-playlist.m3u")
            .ToArray();

        return new GeneratedUrlSet
        {
            M3uUrl = tunerUrls[0],
            XmltvUrl = $"{baseUrl}/epg.xml",
            DockerTunerUrls = tunerUrls,
            ModeNote = "Optional Docker helper mode only generates commands and URLs. The plugin does not run Docker."
        };
    }

    private static GeneratedUrlSet GetNativeUrls(HttpRequest request)
    {
        var baseUrl = GetRequestBaseUrl(request).TrimEnd('/');
        var routeBase = $"{baseUrl}/Plugins/PlutoSetup";
        var tunerUrls = Enumerable.Range(1, PlutoSetupDefaults.DockerTunerCount)
            .Select(index => $"{routeBase}/tuner-{index}-playlist.m3u")
            .ToArray();

        return new GeneratedUrlSet
        {
            M3uUrl = tunerUrls[0],
            XmltvUrl = $"{routeBase}/epg.xml",
            NativeTunerUrls = tunerUrls,
            ModeNote = "Native no-Docker mode is currently unavailable; these Jellyfin-served URLs return 503 until real Pluto generation is implemented."
        };
    }

    private static string GetExternalHost(PluginConfiguration configuration, IReadOnlyList<string> localIpv4Addresses)
    {
        if (!string.IsNullOrWhiteSpace(configuration.ManualHostOverride))
        {
            return configuration.ManualHostOverride.Trim();
        }

        if (!string.IsNullOrWhiteSpace(configuration.SelectedIpAddress))
        {
            return configuration.SelectedIpAddress.Trim();
        }

        return localIpv4Addresses.FirstOrDefault() ?? "127.0.0.1";
    }
}
