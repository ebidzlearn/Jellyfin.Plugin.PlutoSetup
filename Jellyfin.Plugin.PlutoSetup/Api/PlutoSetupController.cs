using System.Net.Mime;
using Jellyfin.Plugin.PlutoSetup.Configuration;
using Jellyfin.Plugin.PlutoSetup.Services;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PlutoSetup.Api;

[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("Plugins/PlutoSetup")]
[Produces(MediaTypeNames.Application.Json)]
public sealed class PlutoSetupController : ControllerBase
{
    private readonly SystemInfoService _systemInfoService;
    private readonly ModeSelectionService _modeSelectionService;
    private readonly UrlValidationService _urlValidationService;
    private readonly DockerCommandService _dockerCommandService;
    private readonly LiveTvAutoSetupService _liveTvAutoSetupService;
    private readonly NativeProviderAvailabilityService _nativeProviderAvailabilityService;

    public PlutoSetupController(
        SystemInfoService systemInfoService,
        ModeSelectionService modeSelectionService,
        UrlValidationService urlValidationService,
        DockerCommandService dockerCommandService,
        LiveTvAutoSetupService liveTvAutoSetupService,
        NativeProviderAvailabilityService nativeProviderAvailabilityService)
    {
        _systemInfoService = systemInfoService;
        _modeSelectionService = modeSelectionService;
        _urlValidationService = urlValidationService;
        _dockerCommandService = dockerCommandService;
        _liveTvAutoSetupService = liveTvAutoSetupService;
        _nativeProviderAvailabilityService = nativeProviderAvailabilityService;
    }

    [HttpGet("SystemInfo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemInfoResponse>> GetSystemInfo(CancellationToken cancellationToken)
    {
        return Ok(await _systemInfoService.GetSystemInfoAsync(Request, GetConfiguration(), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("SaveConfig")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PlutoSetupConfigDto> SaveConfig([FromBody] SaveConfigRequest request)
    {
        var configuration = _modeSelectionService.Merge(GetConfiguration(), request);
        SaveConfiguration(configuration);
        return Ok(SystemInfoService.ToDto(configuration));
    }

    [HttpPost("ValidateUrls")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidateUrlsResponse>> ValidateUrls([FromBody] ValidateUrlsRequest request, CancellationToken cancellationToken)
    {
        var configuration = _modeSelectionService.Merge(GetConfiguration(), request);

        if (configuration.Mode == PlutoSetupMode.NativeNoDocker && !_nativeProviderAvailabilityService.GetAvailability().IsAvailable)
        {
            return Ok(new ValidateUrlsResponse
            {
                M3u = UnavailableNativeResult("M3U"),
                Xmltv = UnavailableNativeResult("XMLTV"),
                AllReachable = false,
                LastSuccessfulValidationUtc = configuration.LastSuccessfulValidationUtc
            });
        }

        var urls = _modeSelectionService.GetGeneratedUrls(configuration, Request);
        var m3u = await _urlValidationService.ValidateAsync(urls.M3uUrl, UrlContentKind.M3u, request.AllowPrivateNetwork, cancellationToken).ConfigureAwait(false);
        var xmltv = await _urlValidationService.ValidateAsync(urls.XmltvUrl, UrlContentKind.Xmltv, request.AllowPrivateNetwork, cancellationToken).ConfigureAwait(false);
        var allReachable = string.Equals(m3u.Status, "Reachable", StringComparison.Ordinal) && string.Equals(xmltv.Status, "Reachable", StringComparison.Ordinal);

        if (allReachable)
        {
            configuration.LastSuccessfulValidationUtc = DateTimeOffset.UtcNow;
            SaveConfiguration(configuration);
        }

        return Ok(new ValidateUrlsResponse
        {
            M3u = m3u,
            Xmltv = xmltv,
            AllReachable = allReachable,
            LastSuccessfulValidationUtc = configuration.LastSuccessfulValidationUtc
        });
    }

    [HttpPost("AutoAdd")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ActionResultDto> AutoAdd([FromBody] AutoAddRequest request)
    {
        _ = _modeSelectionService.Merge(GetConfiguration(), request);
        return Ok(_liveTvAutoSetupService.AutoAdd());
    }

    [HttpPost("NativeRefresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ActionResultDto> NativeRefresh()
    {
        return Ok(NativeUnavailableAction("Native M3U/XMLTV generation is disabled because real Pluto logic has not been implemented and tested."));
    }

    [HttpPost("NativeTestLogin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ActionResultDto> NativeTestLogin()
    {
        return Ok(NativeUnavailableAction("Native Pluto login testing is disabled because a real Pluto authentication flow has not been implemented and tested."));
    }

    [HttpGet("DockerCommandPreview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<DockerCommandResponse> DockerCommandPreview([FromQuery] SaveConfigRequest request)
    {
        var configuration = _modeSelectionService.Merge(GetConfiguration(), request);
        return Ok(_dockerCommandService.BuildCommand(configuration, new DockerCommandRequest
        {
            UserName = "USER_EMAIL",
            RevealPassword = false
        }));
    }

    [HttpPost("DockerCommandPreview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<DockerCommandResponse> DockerCommandPreviewWithSessionPassword([FromBody] DockerCommandRequest request)
    {
        var configuration = _modeSelectionService.Merge(GetConfiguration(), request);
        return Ok(_dockerCommandService.BuildCommand(configuration, request));
    }

    [HttpPost("TestDockerUrl")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidateUrlsResponse>> TestDockerUrl([FromBody] TestDockerUrlRequest request, CancellationToken cancellationToken)
    {
        if (!request.ConfirmContainerIsRunning)
        {
            var blocked = new ValidationResultDto
            {
                Status = "Blocked by safety validation",
                Message = "Confirm that the optional Docker container is running before testing Docker helper URLs."
            };

            return Ok(new ValidateUrlsResponse
            {
                M3u = blocked,
                Xmltv = blocked,
                AllReachable = false
            });
        }

        var configuration = _modeSelectionService.Merge(GetConfiguration(), request);
        configuration.Mode = PlutoSetupMode.DockerHelper;
        var urls = _modeSelectionService.GetGeneratedUrls(configuration, Request);
        var m3u = await _urlValidationService.ValidateAsync(urls.M3uUrl, UrlContentKind.M3u, request.AllowPrivateNetwork, cancellationToken).ConfigureAwait(false);
        var xmltv = await _urlValidationService.ValidateAsync(urls.XmltvUrl, UrlContentKind.Xmltv, request.AllowPrivateNetwork, cancellationToken).ConfigureAwait(false);

        return Ok(new ValidateUrlsResponse
        {
            M3u = m3u,
            Xmltv = xmltv,
            AllReachable = string.Equals(m3u.Status, "Reachable", StringComparison.Ordinal) && string.Equals(xmltv.Status, "Reachable", StringComparison.Ordinal)
        });
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }

    private static void SaveConfiguration(PluginConfiguration configuration)
    {
        Plugin.Instance?.UpdateConfiguration(configuration);
    }

    private static ValidationResultDto UnavailableNativeResult(string label)
    {
        return new ValidationResultDto
        {
            Status = "Unknown",
            Message = $"{label} validation is disabled because native no-Docker generation is unavailable."
        };
    }

    private static ActionResultDto NativeUnavailableAction(string message)
    {
        return new ActionResultDto
        {
            Success = false,
            Status = "Unavailable",
            Message = message
        };
    }
}
