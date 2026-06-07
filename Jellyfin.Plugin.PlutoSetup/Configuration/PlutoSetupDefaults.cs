namespace Jellyfin.Plugin.PlutoSetup.Configuration;

public static class PlutoSetupDefaults
{
    public const string HostedM3uUrl = "https://pluto.freechannels.me/playlist.m3u";
    public const string HostedXmltvUrl = "https://pluto.freechannels.me/epg.xml";
    public const int DefaultDockerPort = 8080;
    public const int DefaultStartChannelNumber = 10000;
    public const int MaxValidationBytes = 131072;
    public const int ValidationTimeoutSeconds = 10;
    public const int MaxRedirects = 3;
    public const int DockerPortStart = 8080;
    public const int DockerPortEnd = 8099;
    public const int DockerTunerCount = 12;
}
