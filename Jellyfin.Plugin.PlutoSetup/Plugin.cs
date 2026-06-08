using System.Globalization;
using System.Text.Json.Nodes;
using Jellyfin.Plugin.PlutoSetup.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PlutoSetup;

public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private const string PluginFolderPrefix = "Pluto TV Auto Tuner_";
    private const string PluginAssemblyFileName = "Jellyfin.Plugin.PlutoSetup.dll";
    private const string PluginId = "0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b";

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Pluto TV Auto Tuner";

    public override Guid Id => Guid.Parse("0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b");

    public override void OnUninstalling()
    {
        foreach (var directory in GetSiblingVersionDirectoriesForRemoval(AssemblyFilePath))
        {
            TryRemoveOrMarkDeleted(directory);
        }
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        ];
    }

    internal static IReadOnlyList<string> GetSiblingVersionDirectoriesForRemoval(string? assemblyFilePath)
    {
        if (string.IsNullOrWhiteSpace(assemblyFilePath))
        {
            return [];
        }

        var currentDirectory = Path.GetDirectoryName(assemblyFilePath);
        if (string.IsNullOrWhiteSpace(currentDirectory))
        {
            return [];
        }

        var pluginRoot = Directory.GetParent(currentDirectory)?.FullName;
        if (string.IsNullOrWhiteSpace(pluginRoot) || !Directory.Exists(pluginRoot))
        {
            return [];
        }

        var normalizedRoot = NormalizeDirectory(pluginRoot);
        var normalizedCurrent = NormalizeDirectory(currentDirectory);

        return Directory.EnumerateDirectories(pluginRoot, PluginFolderPrefix + "*", SearchOption.TopDirectoryOnly)
            .Where(path => IsChildDirectory(normalizedRoot, path))
            .Where(path => !string.Equals(NormalizeDirectory(path), normalizedCurrent, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void TryRemoveOrMarkDeleted(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MarkPluginFolderDeleted(directory);
        }
    }

    private static void MarkPluginFolderDeleted(string directory)
    {
        var manifestPath = Path.Combine(directory, "meta.json");
        var manifest = ReadManifest(manifestPath);
        manifest["category"] ??= "Live TV";
        manifest["guid"] = PluginId;
        manifest["name"] = "Pluto TV Auto Tuner";
        manifest["description"] ??= "Prepare Pluto TV M3U tuner and XMLTV guide setup information for Jellyfin.";
        manifest["owner"] ??= "local";
        manifest["overview"] ??= "Hosted Pluto M3U/XMLTV setup helper with optional Docker command generation.";
        manifest["targetAbi"] ??= "10.11.0.0";
        manifest["timestamp"] ??= DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        manifest["version"] ??= TryGetVersionFromDirectory(directory);
        manifest["status"] = "Deleted";
        manifest["autoUpdate"] = false;
        manifest["assemblies"] = new JsonArray(PluginAssemblyFileName);

        File.WriteAllText(manifestPath, manifest.ToJsonString());
    }

    private static JsonObject ReadManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return [];
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject ?? [];
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static string TryGetVersionFromDirectory(string directory)
    {
        var directoryName = Path.GetFileName(directory);
        return directoryName.StartsWith(PluginFolderPrefix, StringComparison.OrdinalIgnoreCase)
            ? directoryName[PluginFolderPrefix.Length..]
            : "0.0.0.1";
    }

    private static bool IsChildDirectory(string normalizedRoot, string directory)
    {
        var normalizedDirectory = NormalizeDirectory(directory);
        return normalizedDirectory.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectory(string directory)
    {
        return Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
