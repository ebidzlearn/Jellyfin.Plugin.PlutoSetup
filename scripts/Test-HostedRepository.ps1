param(
    [string]$ManifestUrl = "http://localhost:8097/manifest.json"
)

$ErrorActionPreference = "Stop"
$expectedName = "Pluto TV Auto Tuner"
$expectedGuid = "0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b"
$tmpZip = Join-Path $env:TEMP "plutotvautotuner_repository_check.zip"
$tmpImage = Join-Path $env:TEMP "plutotvautotuner_repository_check.png"

try {
    $manifest = Invoke-RestMethod -Uri $ManifestUrl -TimeoutSec 20
    $plugin = if ($manifest.name) { $manifest } else { @($manifest)[0] }

    if ($plugin.name -ne $expectedName) {
        throw "Plugin catalog entry was not found. Expected '$expectedName' but saw '$($plugin.name)'."
    }

    if ($plugin.guid -ne $expectedGuid) {
        throw "Plugin GUID mismatch. Expected '$expectedGuid' but saw '$($plugin.guid)'."
    }

    if (-not [Uri]::IsWellFormedUriString($plugin.imageUrl, [UriKind]::Absolute) -or $plugin.imageUrl -notmatch '^https?://') {
        throw "imageUrl must be an absolute HTTP/HTTPS URL."
    }

    Invoke-WebRequest -Uri $plugin.imageUrl -OutFile $tmpImage -TimeoutSec 30
    if ((Get-Item -LiteralPath $tmpImage).Length -le 0) {
        throw "Downloaded artwork image is empty."
    }

    $release = $plugin.versions[0]
    if (-not [Uri]::IsWellFormedUriString($release.sourceUrl, [UriKind]::Absolute) -or $release.sourceUrl -notmatch '^https?://') {
        throw "sourceUrl must be an absolute HTTP/HTTPS URL."
    }

    Invoke-WebRequest -Uri $release.sourceUrl -OutFile $tmpZip -TimeoutSec 30
    $downloadedChecksum = (Get-FileHash -LiteralPath $tmpZip -Algorithm MD5).Hash.ToLowerInvariant()
    if ($downloadedChecksum -ne $release.checksum) {
        throw "Downloaded ZIP checksum mismatch. Manifest has '$($release.checksum)' but downloaded '$downloadedChecksum'."
    }

    [pscustomobject]@{
        ManifestUrl = $ManifestUrl
        PluginName = $plugin.name
        Guid = $plugin.guid
        Version = $release.version
        SourceUrl = $release.sourceUrl
        ImageUrl = $plugin.imageUrl
        Checksum = $downloadedChecksum
        CatalogEntryVisible = $true
    } | ConvertTo-Json
}
finally {
    if (Test-Path -LiteralPath $tmpZip) {
        Remove-Item -LiteralPath $tmpZip -Force
    }

    if (Test-Path -LiteralPath $tmpImage) {
        Remove-Item -LiteralPath $tmpImage -Force
    }
}
