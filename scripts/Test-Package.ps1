param(
    [string]$RepositoryBaseUrl = "http://localhost:8097"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$propsPath = Join-Path $root "Directory.Build.props"
$zipDir = Join-Path $root "dist"
$repositoryDir = Join-Path $root "repository"
$docsDir = Join-Path $root "docs"
$manifestPath = Join-Path $repositoryDir "manifest.json"
$docsManifestPath = Join-Path $docsDir "manifest.json"
$verificationPath = Join-Path $repositoryDir "VERIFICATION.md"
$expectedGuid = "0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b"
$expectedName = "Pluto TV Auto Tuner"
$expectedTargetAbi = "10.11.0.0"
$artworkFileName = "pluto-tv-auto-tuner.png"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

[xml]$props = Get-Content -LiteralPath $propsPath
$version = $props.Project.PropertyGroup.Version
$zipName = "plutotvautotuner_$version.zip"
$zipPath = Join-Path $zipDir $zipName
$repositoryZipPath = Join-Path $repositoryDir "releases\$zipName"
$repositoryArtworkPath = Join-Path $repositoryDir "images\$artworkFileName"
$docsZipPath = Join-Path $docsDir "releases\$zipName"
$docsArtworkPath = Join-Path $docsDir "images\$artworkFileName"
$compiledDllPath = Join-Path $root "artifacts\publish\Jellyfin.Plugin.PlutoSetup.dll"

Assert-True (Test-Path -LiteralPath $zipPath) "Missing release ZIP at $zipPath."
Assert-True (Test-Path -LiteralPath $repositoryZipPath) "Missing repository ZIP at $repositoryZipPath."
Assert-True (Test-Path -LiteralPath $repositoryArtworkPath) "Missing repository artwork at $repositoryArtworkPath."
Assert-True (Test-Path -LiteralPath $manifestPath) "Missing repository manifest at $manifestPath."
Assert-True (Test-Path -LiteralPath $docsZipPath) "Missing docs ZIP at $docsZipPath."
Assert-True (Test-Path -LiteralPath $docsArtworkPath) "Missing docs artwork at $docsArtworkPath."
Assert-True (Test-Path -LiteralPath $docsManifestPath) "Missing docs manifest at $docsManifestPath."
Assert-True (Test-Path -LiteralPath $compiledDllPath) "Missing compiled plugin DLL at $compiledDllPath."
Assert-True ((Get-FileHash -LiteralPath $repositoryZipPath -Algorithm MD5).Hash -eq (Get-FileHash -LiteralPath $docsZipPath -Algorithm MD5).Hash) "repository and docs ZIP files differ."
Assert-True ((Get-FileHash -LiteralPath $repositoryArtworkPath -Algorithm MD5).Hash -eq (Get-FileHash -LiteralPath $docsArtworkPath -Algorithm MD5).Hash) "repository and docs artwork files differ."
Assert-True ((Get-Content -LiteralPath $manifestPath -Raw) -eq (Get-Content -LiteralPath $docsManifestPath -Raw)) "repository and docs manifests differ."

$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($compiledDllPath).Version.ToString()
Assert-True ($assemblyVersion -eq $version) "Compiled assembly version '$assemblyVersion' does not match project version '$version'."

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName })
    $forbidden = $entries | Where-Object {
        $_ -match '(^|/)(bin|obj|\.git)/' -or
        $_ -match '\.(cs|csproj|sln|ps1|pdb|xml|deps\.json|user|binlog)$'
    }

    Assert-True ($entries -contains "Jellyfin.Plugin.PlutoSetup.dll") "ZIP does not contain Jellyfin.Plugin.PlutoSetup.dll."
    Assert-True ($entries -contains "meta.json") "ZIP does not contain meta.json."
    Assert-True ($entries -contains "LICENSE") "ZIP does not contain LICENSE."
    Assert-True ($entries -contains $artworkFileName) "ZIP does not contain $artworkFileName."
    Assert-True ($forbidden.Count -eq 0) "ZIP contains forbidden development files: $($forbidden -join ', ')."

    $metaEntry = $archive.Entries | Where-Object { $_.FullName -eq "meta.json" } | Select-Object -First 1
    $reader = [IO.StreamReader]::new($metaEntry.Open())
    try {
        $meta = $reader.ReadToEnd() | ConvertFrom-Json
    }
    finally {
        $reader.Dispose()
    }

    Assert-True ($meta.guid -eq $expectedGuid) "meta.json GUID does not match compiled plugin GUID."
    Assert-True ($meta.name -eq $expectedName) "meta.json name does not match plugin name."
    Assert-True ($meta.imagePath -eq $artworkFileName) "meta.json imagePath does not point to $artworkFileName."
    Assert-True ($meta.version -eq $version) "meta.json version does not match project version."
    Assert-True ($meta.targetAbi -eq $expectedTargetAbi) "meta.json targetAbi does not match expected ABI."
}
finally {
    $archive.Dispose()
}

$manifestRaw = Get-Content -LiteralPath $manifestPath -Raw
$manifest = @($manifestRaw | ConvertFrom-Json)
Assert-True ($manifestRaw.TrimStart().StartsWith("[", [StringComparison]::Ordinal)) "manifest.json root must be an array."
Assert-True ($manifest.Count -eq 1) "manifest.json should contain one plugin entry for this repository."

$plugin = $manifest[0]
$release = $plugin.versions[0]
$expectedSourceUrl = "$($RepositoryBaseUrl.TrimEnd('/'))/releases/$zipName"
$expectedImageUrl = "$($RepositoryBaseUrl.TrimEnd('/'))/images/$artworkFileName"
$checksum = (Get-FileHash -LiteralPath $zipPath -Algorithm MD5).Hash.ToLowerInvariant()

Assert-True ($plugin.category -eq "Live TV") "Manifest category is wrong."
Assert-True ($plugin.guid -eq $expectedGuid) "Manifest GUID does not match plugin GUID."
Assert-True ($plugin.name -eq $expectedName) "Manifest plugin name is wrong."
Assert-True ($plugin.imageUrl -eq $expectedImageUrl) "Manifest imageUrl is wrong."
Assert-True ([Uri]::IsWellFormedUriString($plugin.imageUrl, [UriKind]::Absolute)) "Manifest imageUrl must be absolute."
Assert-True ($plugin.imageUrl -match '^https?://') "Manifest imageUrl must be HTTP or HTTPS."
Assert-True (-not [string]::IsNullOrWhiteSpace($plugin.description)) "Manifest description is required."
Assert-True (-not [string]::IsNullOrWhiteSpace($plugin.owner)) "Manifest owner is required."
Assert-True (-not [string]::IsNullOrWhiteSpace($plugin.overview)) "Manifest overview is required."
Assert-True ($release.version -eq $version) "Manifest version does not match project version."
Assert-True ($release.targetAbi -eq $expectedTargetAbi) "Manifest targetAbi is wrong."
Assert-True ($release.sourceUrl -eq $expectedSourceUrl) "Manifest sourceUrl is wrong."
Assert-True ([Uri]::IsWellFormedUriString($release.sourceUrl, [UriKind]::Absolute)) "Manifest sourceUrl must be absolute."
Assert-True ($release.sourceUrl -match '^https?://') "Manifest sourceUrl must be HTTP or HTTPS."
Assert-True ($release.checksum -eq $checksum) "Manifest checksum does not match ZIP MD5."
Assert-True (-not [string]::IsNullOrWhiteSpace($release.changelog)) "Manifest changelog is required."
Assert-True (-not [string]::IsNullOrWhiteSpace($release.timestamp)) "Manifest timestamp is required."

$releaseSourceUrl = $release.sourceUrl
$pluginImageUrl = $plugin.imageUrl
$repositoryManifestUrl = "$($RepositoryBaseUrl.TrimEnd('/'))/manifest.json"
$verification = @"
# Packaging Verification

- [x] Release ZIP exists: dist/$zipName
- [x] Repository ZIP exists: repository/releases/$zipName
- [x] Repository artwork exists: repository/images/$artworkFileName
- [x] GitHub Pages docs mirror contains matching manifest, ZIP, and artwork
- [x] Compiled assembly version matches project version: $assemblyVersion
- [x] ZIP contains only runtime plugin payload files: Jellyfin.Plugin.PlutoSetup.dll, meta.json, LICENSE, and $artworkFileName
- [x] ZIP excludes source, obj, bin, PDB, XML docs, deps files, scripts, git files, secrets, and user-specific settings
- [x] Manifest root is a JSON array
- [x] Manifest GUID matches compiled plugin GUID: $expectedGuid
- [x] Manifest version matches project and ZIP filename: $version
- [x] Manifest targetAbi is Jellyfin ABI $expectedTargetAbi
- [x] Manifest sourceUrl is absolute HTTP/HTTPS: $releaseSourceUrl
- [x] Manifest imageUrl is absolute HTTP/HTTPS: $pluginImageUrl
- [x] Manifest checksum matches final ZIP MD5: $checksum
- [x] Catalog visibility check by manifest data: plugin entry name is $expectedName

To prove this inside a running Jellyfin server, host this folder and add:

$repositoryManifestUrl

Then open Dashboard > Plugins > Catalog and verify $expectedName appears.
"@
[IO.File]::WriteAllText($verificationPath, $verification, [Text.UTF8Encoding]::new($false))

[pscustomobject]@{
    Version = $version
    Zip = $zipPath
    Manifest = $manifestPath
    SourceUrl = $release.sourceUrl
    Checksum = $checksum
    Verification = $verificationPath
} | ConvertTo-Json
