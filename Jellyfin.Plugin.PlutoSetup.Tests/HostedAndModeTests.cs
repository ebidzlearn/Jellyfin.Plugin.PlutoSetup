using Jellyfin.Plugin.PlutoSetup.Configuration;
using Jellyfin.Plugin.PlutoSetup.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jellyfin.Plugin.PlutoSetup.Tests;

public sealed class HostedAndModeTests
{
    [Fact]
    public void HostedUrlServiceUsesDefaultsForEmptyOverrides()
    {
        var service = new HostedUrlService();

        Assert.Equal(PlutoSetupDefaults.HostedM3uUrl, service.GetM3uUrl(" "));
        Assert.Equal(PlutoSetupDefaults.HostedXmltvUrl, service.GetXmltvUrl(null));
    }

    [Fact]
    public void HostedModeIgnoresStartAndUsesHostedUrls()
    {
        var service = new ModeSelectionService(new HostedUrlService());
        var config = new PluginConfiguration
        {
            Mode = PlutoSetupMode.HostedNoDocker,
            StartChannelNumber = 12345
        };

        var urls = service.GetGeneratedUrls(config, BuildRequest());

        Assert.Equal(PlutoSetupDefaults.HostedM3uUrl, urls.M3uUrl);
        Assert.Equal(PlutoSetupDefaults.HostedXmltvUrl, urls.XmltvUrl);
        Assert.Contains("START", urls.ModeNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DockerModeGeneratesTwelveTunerUrlsAndEpgUrl()
    {
        var service = new ModeSelectionService(new HostedUrlService());
        var config = new PluginConfiguration
        {
            Mode = PlutoSetupMode.DockerHelper,
            ManualHostOverride = "192.168.1.25",
            DockerPort = 8091
        };

        var urls = service.GetGeneratedUrls(config, BuildRequest(), ["10.0.0.5"]);

        Assert.Equal("http://192.168.1.25:8091/tuner-1-playlist.m3u", urls.M3uUrl);
        Assert.Equal("http://192.168.1.25:8091/epg.xml", urls.XmltvUrl);
        Assert.Equal(12, urls.DockerTunerUrls.Count);
    }

    [Fact]
    public void NativeModeUsesJellyfinRoutePreview()
    {
        var service = new ModeSelectionService(new HostedUrlService());
        var config = new PluginConfiguration
        {
            Mode = PlutoSetupMode.NativeNoDocker
        };

        var urls = service.GetGeneratedUrls(config, BuildRequest());

        Assert.Equal("http://jellyfin.local:8096/Plugins/PlutoSetup/tuner-1-playlist.m3u", urls.M3uUrl);
        Assert.Equal("http://jellyfin.local:8096/Plugins/PlutoSetup/epg.xml", urls.XmltvUrl);
    }

    private static HttpRequest BuildRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("jellyfin.local", 8096);
        return context.Request;
    }
}
