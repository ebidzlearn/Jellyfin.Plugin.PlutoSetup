using Jellyfin.Plugin.PlutoSetup.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PlutoSetup;

public sealed class ServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<SystemInfoService>();
        serviceCollection.AddSingleton<PortScannerService>();
        serviceCollection.AddSingleton<ModeSelectionService>();
        serviceCollection.AddSingleton<HostedUrlService>();
        serviceCollection.AddSingleton<UrlValidationService>();
        serviceCollection.AddSingleton<DockerCommandService>();
        serviceCollection.AddSingleton<LiveTvAutoSetupService>();
        serviceCollection.AddSingleton<NativeProviderAvailabilityService>();
    }
}
