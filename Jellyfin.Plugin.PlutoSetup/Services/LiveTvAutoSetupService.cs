using Jellyfin.Plugin.PlutoSetup.Api;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public sealed class LiveTvAutoSetupService
{
    public LiveTvSnapshot GetSnapshot()
    {
        return new LiveTvSnapshot
        {
            ExistingTuners = [],
            ExistingGuideProviders = [],
            AutoAddAvailability = new AutoAddAvailabilityDto
            {
                IsAvailable = false,
                Status = "Unavailable",
                Message = "This MVP does not auto-add Live TV configuration or report configured tuner/guide providers because a safe, stable read/write workflow was not enabled. Review existing Live TV guide providers manually before adding XMLTV.",
                ExistingGuideProviderWarning = false
            }
        };
    }

    public ActionResultDto AutoAdd()
    {
        var snapshot = GetSnapshot();
        var guideWarning = snapshot.AutoAddAvailability.ExistingGuideProviderWarning
            ? " A guide provider service is present; Jellyfin XMLTV can conflict with Schedules Direct, so review existing guide configuration before adding XMLTV."
            : string.Empty;

        return new ActionResultDto
        {
            Success = false,
            Status = "Unavailable",
            Message = "Auto-add was not attempted. Safe supported Jellyfin Live TV auto-configuration is not enabled in this MVP, so no tuner or guide provider was changed." + guideWarning
        };
    }

}

public sealed class LiveTvSnapshot
{
    public IReadOnlyList<LiveTvItemDto> ExistingTuners { get; set; } = [];

    public IReadOnlyList<LiveTvItemDto> ExistingGuideProviders { get; set; } = [];

    public AutoAddAvailabilityDto AutoAddAvailability { get; set; } = new();
}
