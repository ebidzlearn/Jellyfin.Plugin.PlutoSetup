# Pluto TV Auto Tuner

Pluto TV Auto Tuner is a Jellyfin plugin MVP that helps administrators prepare M3U tuner and XMLTV guide provider information for Pluto TV Live TV setup.

The default path is hosted no-Docker mode. It uses third-party hosted Pluto playlist and EPG URLs, does not require Docker, does not require Pluto credentials, and does not persist passwords.

## Compatibility

- Target Jellyfin ABI: 10.11.0.0 for Jellyfin 10.11.x servers.
- Target framework: net9.0, matching current Jellyfin 10.11 package assets.
- Jellyfin packages: Jellyfin.Controller 10.11.6 and Jellyfin.Model 10.11.6 with runtime assets excluded.
- Build SDK: .NET 9 SDK.

The current official plugin template was checked before this project was created. Jellyfin 10.11 NuGet packages target net9.0, so this plugin uses net9.0. If you need Jellyfin 10.9 compatibility, retarget to the 10.9.11 packages and net8.0.

## Modes

### Hosted No-Docker

This is the MVP and default mode.

- M3U default: `https://pluto.freechannels.me/playlist.m3u`
- XMLTV default: `https://pluto.freechannels.me/epg.xml`
- These are third-party hosted URLs, not official Pluto or Jellyfin URLs.
- The hosted provider says files update every 3 hours.
- The URLs may change, stop working, or be rate-limited outside this plugin's control.
- Pluto credentials are not required or used.
- START channel number does not affect hosted mode unless playlist rewriting is implemented and tested.

### Optional Docker Helper

This mode only generates Docker commands and matching URLs for `jonmaddox/pluto-for-channels`.

- Docker is optional and is never run by this plugin.
- Pluto credentials are required by the container through `PLUTO_USERNAME` and `PLUTO_PASSWORD`.
- The plugin does not save the Pluto password.
- Passwords are masked by default in command previews.
- Bash/Linux/macOS, Windows PowerShell, and Windows CMD command variants are generated because quoting differs by shell.

### Native No-Docker

Native mode is disabled in this MVP.

It will remain unavailable unless the real Pluto/pluto-for-channels logic can be inspected, ported to C#, compiled, and tested. This plugin does not invent Pluto API endpoints, fake login, or serve placeholder playlist/guide data.

## Build

From this directory:

```powershell
dotnet restore .\Jellyfin.Plugin.PlutoSetup.sln
dotnet test .\Jellyfin.Plugin.PlutoSetup.sln
dotnet publish .\Jellyfin.Plugin.PlutoSetup\Jellyfin.Plugin.PlutoSetup.csproj -c Release -o .\artifacts\publish
```

If .NET 9 is not installed globally, use a local SDK path:

```powershell
& "$env:TEMP\dotnet9-sdk\dotnet.exe" test .\Jellyfin.Plugin.PlutoSetup.sln
```

## Packaging

JPRM was checked during development and was not available on this machine. The repository package is therefore created manually with the same manifest shape used by Jellyfin's hosted plugin repositories.

Create the Release DLL, plugin ZIP, repository folder, checksum, `meta.json`, and `manifest.json`:

```powershell
.\scripts\Package-Plugin.ps1 -DotNetPath "$env:TEMP\dotnet9-sdk\dotnet.exe" -RepositoryBaseUrl "http://localhost:8097"
.\scripts\Test-Package.ps1 -RepositoryBaseUrl "http://localhost:8097"
```

Outputs:

- `artifacts/publish/Jellyfin.Plugin.PlutoSetup.dll`
- `dist/plutotvautotuner_0.1.0.0.zip`
- `repository/manifest.json`
- `repository/releases/plutotvautotuner_0.1.0.0.zip`
- `repository/VERIFICATION.md`

The plugin ZIP contains only the Jellyfin runtime plugin payload: `Jellyfin.Plugin.PlutoSetup.dll`, `meta.json`, and `LICENSE`. It intentionally excludes source files, `obj`, `bin`, PDB files, XML docs, development scripts, test files, secrets, local paths, and user-specific settings.

## Manual Local Install

For a manual install:

1. Stop Jellyfin.
2. Create a plugin folder under the Jellyfin data directory, for example:
   - Windows: `%LOCALAPPDATA%\jellyfin\plugins\Pluto TV Auto Tuner`
   - Linux: `/var/lib/jellyfin/plugins/Pluto TV Auto Tuner`
3. Extract `dist/plutotvautotuner_0.1.0.0.zip` into that folder, or copy `artifacts/publish/Jellyfin.Plugin.PlutoSetup.dll` there.
4. Start Jellyfin.
5. Open Dashboard > Plugins > Pluto TV Auto Tuner.

## Repository-Based Install

The `repository` folder is ready to serve from GitHub Pages or another static web host. The manifest `sourceUrl` must be an absolute HTTP/HTTPS URL that points to the compiled ZIP, not the source repository.

For local HTTP verification on the same machine as Jellyfin:

```powershell
.\scripts\Start-RepositoryServer.ps1 -Prefix "http://localhost:8097/"
.\scripts\Test-HostedRepository.ps1 -ManifestUrl "http://localhost:8097/manifest.json"
```

Then add this repository URL in Jellyfin:

```text
http://localhost:8097/manifest.json
```

For GitHub Pages or another hosted static site, regenerate the package with the final public base URL before publishing:

```powershell
.\scripts\Package-Plugin.ps1 -DotNetPath "dotnet" -RepositoryBaseUrl "https://YOUR_HOST/YOUR_REPOSITORY_PATH"
.\scripts\Test-Package.ps1 -RepositoryBaseUrl "https://YOUR_HOST/YOUR_REPOSITORY_PATH"
```

Upload the full `repository` folder. Jellyfin must be able to reach both:

- `https://YOUR_HOST/YOUR_REPOSITORY_PATH/manifest.json`
- `https://YOUR_HOST/YOUR_REPOSITORY_PATH/releases/plutotvautotuner_0.1.0.0.zip`

## Catalog Verification Checklist

After hosting `repository`:

1. Open the manifest URL in a browser from the Jellyfin server host and confirm it returns JSON.
2. Run `.\scripts\Test-Package.ps1 -RepositoryBaseUrl "https://YOUR_HOST/YOUR_REPOSITORY_PATH"` and confirm checksum validation passes.
3. Run `.\scripts\Test-HostedRepository.ps1 -ManifestUrl "https://YOUR_HOST/YOUR_REPOSITORY_PATH/manifest.json"` and confirm it reports `CatalogEntryVisible: true`.
4. In Jellyfin, go to Dashboard > Plugins > Repositories.
5. Add the hosted manifest URL.
6. Go to Dashboard > Plugins > Catalog.
7. Confirm `Pluto TV Auto Tuner` appears in the Catalog.
8. Install it, restart Jellyfin, and open Dashboard > Plugins > Pluto TV Auto Tuner.

`repository/VERIFICATION.md` records the local static-repository checks performed by the packaging verifier.

## Using Hosted No-Docker Mode

1. Open the plugin page.
2. Keep mode set to Hosted no-Docker.
3. Optionally override the hosted M3U or XMLTV URL.
4. Click Validate URLs.
5. Copy the generated URLs or use Auto-add if the plugin reports safe auto-add support.

If auto-add is unavailable, add the URLs manually:

Step 1: Go to Dashboard > Live TV > Tuner Devices > Add Tuner Device. Set Tuner Type to M3U Tuner. Paste the generated M3U URL into File or URL.

Step 2: Go to Dashboard > Live TV > TV Guide Data Providers > Add Provider. Select XMLTV. Paste the generated XMLTV URL into File or URL.

Step 3: Save and refresh guide data.

Step 4: Map channels if Jellyfin requires guide/channel mapping.

Jellyfin may require channel mapping after XMLTV data is added. XMLTV may conflict with an existing Schedules Direct guide provider; this plugin warns when safe detection is available and does not overwrite providers.

## Security Notes

- Pluto passwords are not saved in plain text.
- MVP behavior is stricter: Pluto passwords are not persisted at all.
- Hosted mode does not require Pluto credentials.
- Docker helper mode requires credentials only to generate a user-copied Docker command.
- The plugin does not log passwords.
- URL validation is bounded by scheme checks, timeouts, redirect limits, streaming reads, and maximum bytes.
- Auto-add is best effort and manual setup is always supported.
- The compiled plugin links Jellyfin GPL packages; keep the included license notice with redistributed ZIPs.

## Known Limitations

- Native no-Docker mode is disabled until real Pluto logic is implemented and tested.
- Auto-add is disabled unless a safe supported Jellyfin API/service is available in the running server.
- The plugin does not edit Jellyfin configuration files directly.
- The plugin does not scan ports in hosted mode.
- Optional Docker port checks are limited and only for helper mode.
