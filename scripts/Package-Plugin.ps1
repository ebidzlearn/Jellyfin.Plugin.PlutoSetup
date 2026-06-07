param(
    [string]$RepositoryBaseUrl = "http://localhost:8097",
    [string]$Configuration = "Release",
    [string]$DotNetPath = "dotnet"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $root "Jellyfin.Plugin.PlutoSetup\Jellyfin.Plugin.PlutoSetup.csproj"
$propsPath = Join-Path $root "Directory.Build.props"
$publishDir = Join-Path $root "artifacts\publish"
$packageDir = Join-Path $root "artifacts\package"
$distDir = Join-Path $root "dist"
$repositoryDir = Join-Path $root "repository"
$repositoryReleasesDir = Join-Path $repositoryDir "releases"
$rootManifestPath = Join-Path $root "manifest.json"
$repositoryManifestPath = Join-Path $repositoryDir "manifest.json"

$metadata = [ordered]@{
    category = "Live TV"
    guid = "0d7f2f32-8b2d-4d3f-b6c4-90c5a0b49f1b"
    name = "Pluto TV Auto Tuner"
    description = "Prepare Pluto TV M3U tuner and XMLTV guide setup information for Jellyfin."
    owner = "local"
    overview = "Hosted Pluto M3U/XMLTV setup helper with optional Docker command generation."
    targetAbi = "10.11.6.0"
    changelog = "Initial MVP with hosted no-Docker setup, optional Docker helper command generation, bounded URL validation, manual setup fallback, and disabled native mode that does not fake Pluto data."
}

function Get-ProjectVersion {
    [xml]$props = Get-Content -LiteralPath $propsPath
    $version = $props.Project.PropertyGroup.Version
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Version was not found in $propsPath."
    }

    return $version
}

function Remove-DirectoryIfExists {
    param([string]$Path)

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Write-JsonFile {
    param(
        [object]$Value,
        [string]$Path
    )

    $json = ConvertTo-Json -InputObject $Value -Depth 10
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

function New-MetaJson {
    param(
        [string]$Version,
        [string]$Timestamp
    )

    return [ordered]@{
        category = $metadata.category
        changelog = $metadata.changelog
        description = $metadata.description
        guid = $metadata.guid
        name = $metadata.name
        overview = $metadata.overview
        owner = $metadata.owner
        targetAbi = $metadata.targetAbi
        timestamp = $Timestamp
        version = $Version
    }
}

$version = Get-ProjectVersion
$zipName = "plutotvautotuner_$version.zip"
$zipPath = Join-Path $distDir $zipName
$repositoryZipPath = Join-Path $repositoryReleasesDir $zipName
$sourceUrl = "$($RepositoryBaseUrl.TrimEnd('/'))/releases/$zipName"
$timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

& $DotNetPath publish $projectPath -c $Configuration -o $publishDir

Remove-DirectoryIfExists $packageDir
New-Item -ItemType Directory -Path $packageDir | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
New-Item -ItemType Directory -Path $repositoryReleasesDir -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $publishDir "Jellyfin.Plugin.PlutoSetup.dll") -Destination $packageDir
Copy-Item -LiteralPath (Join-Path $root "LICENSE") -Destination $packageDir
Write-JsonFile -Value (New-MetaJson -Version $version -Timestamp $timestamp) -Path (Join-Path $packageDir "meta.json")

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $packageDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
Copy-Item -LiteralPath $zipPath -Destination $repositoryZipPath -Force

$checksum = (Get-FileHash -LiteralPath $zipPath -Algorithm MD5).Hash.ToLowerInvariant()
$manifest = @(
    [ordered]@{
        category = $metadata.category
        guid = $metadata.guid
        name = $metadata.name
        description = $metadata.description
        owner = $metadata.owner
        overview = $metadata.overview
        versions = @(
            [ordered]@{
                checksum = $checksum
                changelog = $metadata.changelog
                targetAbi = $metadata.targetAbi
                sourceUrl = $sourceUrl
                timestamp = $timestamp
                version = $version
            }
        )
    }
)

Write-JsonFile -Value $manifest -Path $rootManifestPath
Write-JsonFile -Value $manifest -Path $repositoryManifestPath

$indexHtml = @"
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Pluto TV Auto Tuner Jellyfin Repository</title>
</head>
<body>
  <h1>Pluto TV Auto Tuner Jellyfin Repository</h1>
  <p>Repository URL: <code>$($RepositoryBaseUrl.TrimEnd('/'))/manifest.json</code></p>
  <p>Plugin ZIP: <a href="releases/$zipName">releases/$zipName</a></p>
</body>
</html>
"@
[IO.File]::WriteAllText((Join-Path $repositoryDir "index.html"), $indexHtml, [Text.UTF8Encoding]::new($false))

[pscustomobject]@{
    Version = $version
    ZipPath = $zipPath
    RepositoryZipPath = $repositoryZipPath
    ManifestPath = $repositoryManifestPath
    SourceUrl = $sourceUrl
    Checksum = $checksum
} | ConvertTo-Json
