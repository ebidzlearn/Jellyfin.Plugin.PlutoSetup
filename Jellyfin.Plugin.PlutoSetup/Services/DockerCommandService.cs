using Jellyfin.Plugin.PlutoSetup.Api;
using Jellyfin.Plugin.PlutoSetup.Configuration;

namespace Jellyfin.Plugin.PlutoSetup.Services;

public sealed class DockerCommandService
{
    public DockerCommandResponse BuildCommand(PluginConfiguration configuration, DockerCommandRequest request)
    {
        var username = string.IsNullOrWhiteSpace(request.UserName) ? "USER_EMAIL" : request.UserName.Trim();
        var includePassword = request.RevealPassword;
        var password = includePassword ? request.Password ?? string.Empty : "********";

        if (includePassword && string.IsNullOrEmpty(password))
        {
            return new DockerCommandResponse
            {
                RequiresCurrentPassword = true,
                Message = "Enter the Pluto password in the current session before copying an unmasked Docker command."
            };
        }

        var port = ModeSelectionService.SanitizePort(configuration.DockerPort);
        var start = ModeSelectionService.SanitizeStart(configuration.StartChannelNumber);

        return new DockerCommandResponse
        {
            Bash = BuildBash(port, start, username, password),
            PowerShell = BuildPowerShell(port, start, username, password),
            Cmd = BuildCmd(port, start, username, password),
            PasswordIncluded = includePassword,
            RequiresCurrentPassword = false,
            Message = includePassword
                ? "Password included for this one-time command response only. It was not saved."
                : "Password is masked. Click copy/reveal after entering a current-session password to copy an unmasked command."
        };
    }

    private static string BuildBash(int port, int start, string username, string password)
    {
        return string.Join(
            ' ',
            "docker run -d --restart unless-stopped --name pluto-for-channels",
            $"-p {port}:80",
            $"-e PLUTO_USERNAME='{EscapeBashSingleQuoted(username)}'",
            $"-e PLUTO_PASSWORD='{EscapeBashSingleQuoted(password)}'",
            $"-e START={start}",
            "jonmaddox/pluto-for-channels");
    }

    private static string BuildPowerShell(int port, int start, string username, string password)
    {
        return string.Join(
            ' ',
            "docker run -d --restart unless-stopped --name pluto-for-channels",
            $"-p {port}:80",
            $"-e PLUTO_USERNAME=\"{EscapePowerShellDoubleQuoted(username)}\"",
            $"-e PLUTO_PASSWORD=\"{EscapePowerShellDoubleQuoted(password)}\"",
            $"-e START={start}",
            "jonmaddox/pluto-for-channels");
    }

    private static string BuildCmd(int port, int start, string username, string password)
    {
        return string.Join(
            ' ',
            "docker run -d --restart unless-stopped --name pluto-for-channels",
            $"-p {port}:80",
            $"-e PLUTO_USERNAME=\"{EscapeCmdDoubleQuoted(username)}\"",
            $"-e PLUTO_PASSWORD=\"{EscapeCmdDoubleQuoted(password)}\"",
            $"-e START={start}",
            "jonmaddox/pluto-for-channels");
    }

    private static string EscapeBashSingleQuoted(string value)
    {
        return value.Replace("'", "'\"'\"'", StringComparison.Ordinal);
    }

    private static string EscapePowerShellDoubleQuoted(string value)
    {
        return value
            .Replace("`", "``", StringComparison.Ordinal)
            .Replace("\"", "`\"", StringComparison.Ordinal)
            .Replace("$", "`$", StringComparison.Ordinal);
    }

    private static string EscapeCmdDoubleQuoted(string value)
    {
        return value
            .Replace("^", "^^", StringComparison.Ordinal)
            .Replace("\"", "\"\"", StringComparison.Ordinal)
            .Replace("%", "%%", StringComparison.Ordinal);
    }
}
