using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using Jellyfin.Plugin.PlutoSetup.Api;
using Jellyfin.Plugin.PlutoSetup.Configuration;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public enum UrlContentKind
{
    M3u,
    Xmltv
}

public sealed class UrlValidationService
{
    private static readonly HttpClient HttpClient = new(
        new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(PlutoSetupDefaults.ValidationTimeoutSeconds)
        })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<ValidationResultDto> ValidateAsync(
        string url,
        UrlContentKind contentKind,
        bool allowPrivateNetwork,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Result(url, "Blocked by safety validation", "Enter an absolute http or https URL.", false);
        }

        if (!IsHttpScheme(uri))
        {
            return Result(url, "Blocked by safety validation", "Only http and https URLs are allowed.", false);
        }

        var isPrivateOrLocal = await IsPrivateOrLocalAddressAsync(uri, cancellationToken).ConfigureAwait(false);
        if (isPrivateOrLocal && !allowPrivateNetwork)
        {
            return Result(uri.ToString(), "Blocked by safety validation", "The URL points to a loopback, private, or local address. Confirm before validating it.", true);
        }

        try
        {
            var response = await FetchPreviewBytesAsync(uri, allowPrivateNetwork, cancellationToken).ConfigureAwait(false);
            if (response.Status != "Reachable")
            {
                return Result(response.Url, response.Status, response.Message, isPrivateOrLocal);
            }

            var body = Encoding.UTF8.GetString(response.Bytes);
            var correctFormat = contentKind == UrlContentKind.M3u
                ? LooksLikeM3u(body)
                : LooksLikeXmltv(body);

            if (!correctFormat)
            {
                return Result(uri.ToString(), "Wrong format", contentKind == UrlContentKind.M3u ? "The response did not contain #EXTM3U." : "The response did not look like XMLTV.", isPrivateOrLocal);
            }

            return Result(uri.ToString(), "Reachable", "The URL is reachable and the response format looks correct.", isPrivateOrLocal);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result(uri.ToString(), "Timed out", "Validation timed out before enough data could be read.", isPrivateOrLocal);
        }
        catch (HttpRequestException ex)
        {
            return Result(uri.ToString(), "Not reachable", ex.Message, isPrivateOrLocal);
        }
        catch (IOException ex)
        {
            return Result(uri.ToString(), "Unknown", ex.Message, isPrivateOrLocal);
        }
    }

    private static async Task<PreviewResponse> FetchPreviewBytesAsync(Uri startingUri, bool allowPrivateNetwork, CancellationToken cancellationToken)
    {
        var current = startingUri;

        for (var redirect = 0; redirect <= PlutoSetupDefaults.MaxRedirects; redirect++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(PlutoSetupDefaults.ValidationTimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            request.Headers.Range = new RangeHeaderValue(0, PlutoSetupDefaults.MaxValidationBytes - 1);
            request.Headers.UserAgent.ParseAdd("Jellyfin.Plugin.PlutoSetup/0.1");

            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (IsRedirect(response.StatusCode))
            {
                if (redirect == PlutoSetupDefaults.MaxRedirects)
                {
                    return new PreviewResponse(current.ToString(), "Not reachable", "Too many redirects.", []);
                }

                var nextUri = ResolveRedirect(current, response.Headers.Location);
                if (nextUri is null || !IsHttpScheme(nextUri))
                {
                    return new PreviewResponse(current.ToString(), "Blocked by safety validation", "Redirect target was missing or not http/https.", []);
                }

                var privateRedirect = await IsPrivateOrLocalAddressAsync(nextUri, cancellationToken).ConfigureAwait(false);
                if (privateRedirect && !allowPrivateNetwork)
                {
                    return new PreviewResponse(nextUri.ToString(), "Blocked by safety validation", "Redirect target points to a loopback, private, or local address.", []);
                }

                current = nextUri;
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return new PreviewResponse(current.ToString(), "Not reachable", $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}", []);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            var bytes = await ReadLimitedAsync(stream, PlutoSetupDefaults.MaxValidationBytes, timeout.Token).ConfigureAwait(false);
            return new PreviewResponse(current.ToString(), "Reachable", "Fetched a bounded response preview.", bytes);
        }

        return new PreviewResponse(current.ToString(), "Not reachable", "Too many redirects.", []);
    }

    private static async Task<byte[]> ReadLimitedAsync(Stream stream, int maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var output = new MemoryStream(capacity: Math.Min(maxBytes, buffer.Length));

        while (output.Length < maxBytes)
        {
            var bytesToRead = Math.Min(buffer.Length, maxBytes - (int)output.Length);
            var read = await stream.ReadAsync(buffer.AsMemory(0, bytesToRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    private static bool LooksLikeM3u(string body)
    {
        return body.IndexOf("#EXTM3U", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeXmltv(string body)
    {
        return body.IndexOf("<tv", StringComparison.OrdinalIgnoreCase) >= 0
            && (body.IndexOf("<channel", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("<programme", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsHttpScheme(Uri uri)
    {
        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code is >= 300 and <= 399;
    }

    private static Uri? ResolveRedirect(Uri current, Uri? location)
    {
        if (location is null)
        {
            return null;
        }

        return location.IsAbsoluteUri ? location : new Uri(current, location);
    }

    private static async Task<bool> IsPrivateOrLocalAddressAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(uri.Host, out var parsed))
        {
            return IsPrivateOrLocalAddress(parsed);
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.IdnHost).WaitAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            return addresses.Any(IsPrivateOrLocalAddress);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or TimeoutException)
        {
            return false;
        }
    }

    private static bool IsPrivateOrLocalAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254)
                || bytes[0] == 127;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
        }

        return false;
    }

    private static ValidationResultDto Result(string url, string status, string message, bool isPrivateOrLocal)
    {
        return new ValidationResultDto
        {
            Url = url,
            Status = status,
            Message = message,
            IsPrivateOrLocalAddress = isPrivateOrLocal
        };
    }

    private sealed record PreviewResponse(string Url, string Status, string Message, byte[] Bytes);
}
