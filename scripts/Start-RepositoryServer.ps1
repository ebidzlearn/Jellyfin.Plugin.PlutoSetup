param(
    [string]$Prefix = "http://localhost:8097/",
    [string]$RepositoryPath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepositoryPath)) {
    $RepositoryPath = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")) "repository"
}

$repositoryRoot = [IO.Path]::GetFullPath((Resolve-Path $RepositoryPath))
$listener = [Net.HttpListener]::new()
$listener.Prefixes.Add($Prefix)
$listener.Start()

Write-Host "Serving $repositoryRoot at $Prefix"
Write-Host "Repository URL: $($Prefix.TrimEnd('/'))/manifest.json"
Write-Host "Press Ctrl+C to stop."

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        try {
            $relative = [Net.WebUtility]::UrlDecode($context.Request.Url.AbsolutePath.TrimStart("/"))
            if ([string]::IsNullOrWhiteSpace($relative)) {
                $relative = "index.html"
            }

            $candidate = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $relative))
            if (-not $candidate.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
                $context.Response.StatusCode = 404
                $bytes = [Text.Encoding]::UTF8.GetBytes("Not found")
                $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
                continue
            }

            $extension = [IO.Path]::GetExtension($candidate).ToLowerInvariant()
            $context.Response.ContentType = switch ($extension) {
                ".json" { "application/json; charset=utf-8" }
                ".zip" { "application/zip" }
                ".html" { "text/html; charset=utf-8" }
                default { "application/octet-stream" }
            }

            $bytes = [IO.File]::ReadAllBytes($candidate)
            $context.Response.ContentLength64 = $bytes.Length
            $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        }
        finally {
            $context.Response.OutputStream.Close()
        }
    }
}
finally {
    $listener.Stop()
    $listener.Close()
}
