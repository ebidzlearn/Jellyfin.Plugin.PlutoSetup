using Jellyfin.Plugin.PlutoSetup.Api;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public sealed class NativeProviderAvailabilityService
{
    public NativeAvailabilityDto GetAvailability()
    {
        return new NativeAvailabilityDto
        {
            IsAvailable = false,
            Status = "Unavailable",
            Message = "Native no-Docker mode is disabled. The real Pluto authentication, playlist, and XMLTV logic has not been ported to C# and tested."
        };
    }
}
