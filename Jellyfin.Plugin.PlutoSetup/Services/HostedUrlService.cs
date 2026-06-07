using Jellyfin.Plugin.PlutoSetup.Configuration;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public sealed class HostedUrlService
{
    public string GetM3uUrl(string? overrideUrl)
    {
        return NormalizeOrDefault(overrideUrl, PlutoSetupDefaults.HostedM3uUrl);
    }

    public string GetXmltvUrl(string? overrideUrl)
    {
        return NormalizeOrDefault(overrideUrl, PlutoSetupDefaults.HostedXmltvUrl);
    }

    private static string NormalizeOrDefault(string? value, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.Trim();
    }
}
