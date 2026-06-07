using Jellyfin.Plugin.PlutoSetup.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace Jellyfin.Plugin.PlutoSetup.Tests;

public sealed class SafetyAndFallbackTests
{
    [Fact]
    public void NativeProviderIsUnavailableByDefault()
    {
        var availability = new NativeProviderAvailabilityService().GetAvailability();

        Assert.False(availability.IsAvailable);
        Assert.Equal("Unavailable", availability.Status);
    }

    [Fact]
    public void AutoAddDoesNotPretendToSucceed()
    {
        var result = new LiveTvAutoSetupService().AutoAdd();

        Assert.False(result.Success);
        Assert.Equal("Unavailable", result.Status);
        Assert.Contains("not attempted", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UrlValidationBlocksNonHttpSchemes()
    {
        var service = new UrlValidationService();

        var result = await service.ValidateAsync("file:///tmp/playlist.m3u", UrlContentKind.M3u, false, CancellationToken.None);

        Assert.Equal("Blocked by safety validation", result.Status);
    }

    [Fact]
    public async Task UrlValidationBlocksLoopbackWithoutConfirmation()
    {
        var service = new UrlValidationService();

        var result = await service.ValidateAsync("http://127.0.0.1:8090/epg.xml", UrlContentKind.Xmltv, false, CancellationToken.None);

        Assert.Equal("Blocked by safety validation", result.Status);
        Assert.True(result.IsPrivateOrLocalAddress);
    }

    [Fact]
    public async Task UrlValidationAcceptsM3uPreview()
    {
        await WithHttpResponseAsync("#EXTM3U\n#EXTINF:-1,tvg-id=\"one\",One\nhttp://example.test/stream.m3u8\n", async url =>
        {
            var service = new UrlValidationService();

            var result = await service.ValidateAsync(url, UrlContentKind.M3u, true, CancellationToken.None);

            Assert.Equal("Reachable", result.Status);
        });
    }

    [Fact]
    public async Task UrlValidationAcceptsXmltvPreview()
    {
        await WithHttpResponseAsync("<tv><channel id=\"one\"><display-name>One</display-name></channel></tv>", async url =>
        {
            var service = new UrlValidationService();

            var result = await service.ValidateAsync(url, UrlContentKind.Xmltv, true, CancellationToken.None);

            Assert.Equal("Reachable", result.Status);
        });
    }

    private static async Task WithHttpResponseAsync(string body, Func<string, Task> test)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            var requestBuffer = new byte[1024];
            _ = await stream.ReadAsync(requestBuffer);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var header = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: " + bodyBytes.Length + "\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header);
            await stream.WriteAsync(bodyBytes);
        });

        try
        {
            await test("http://127.0.0.1:" + port + "/preview");
        }
        finally
        {
            listener.Stop();
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
