using System.Net.Sockets;
using Jellyfin.Plugin.PlutoSetup.Configuration;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public sealed class PortScannerService
{
    public async Task<IReadOnlyList<int>> FindAvailablePortsAsync(CancellationToken cancellationToken)
    {
        var ports = new List<int>();

        for (var port = PlutoSetupDefaults.DockerPortStart; port <= PlutoSetupDefaults.DockerPortEnd; port++)
        {
            if (await IsPortAvailableAsync(port, cancellationToken).ConfigureAwait(false))
            {
                ports.Add(port);
            }
        }

        return ports;
    }

    private static async Task<bool> IsPortAvailableAsync(int port, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(200));

        try
        {
            await client.ConnectAsync("127.0.0.1", port, timeout.Token).ConfigureAwait(false);
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }
}
