using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.PlutoSetup.Api;

[ApiController]
[Route("Plugins/PlutoSetup")]
public sealed class PlutoSetupNativeController : ControllerBase
{
    [HttpGet("tuner-{tunerNumber:int}-playlist.m3u")]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ContentResult GetNativeTunerPlaylist(int tunerNumber)
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Content(
            $"Native no-Docker Pluto playlist generation is unavailable. Tuner {tunerNumber} has no generated playlist because real Pluto logic has not been implemented and tested.",
            "text/plain");
    }

    [HttpGet("epg.xml")]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ContentResult GetNativeEpg()
    {
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Content(
            "Native no-Docker Pluto XMLTV generation is unavailable because real Pluto logic has not been implemented and tested.",
            "text/plain");
    }
}
