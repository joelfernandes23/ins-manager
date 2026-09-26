param(
    [Parameter(Position = 0)]
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path (Split-Path -Parent $repoRoot) '.dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }
$project = Join-Path $repoRoot 'src\InsManager.App\InsManager.App.csproj'
$releaseRoot = Join-Path $repoRoot 'artifacts\release'
$stagingRoot = Join-Path $releaseRoot "INS-Manager-$Version-win-x64"
$publishRoot = Join-Path $stagingRoot 'INS Manager'
$archive = Join-Path $releaseRoot "INS-Manager-$Version-win-x64.zip"
$checksum = "$archive.sha256"

New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

foreach ($path in @($stagingRoot, $archive, $checksum)) {
    if (Test-Path $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

& $dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishRoot `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $publishRoot
Compress-Archive -Path $publishRoot -DestinationPath $archive -CompressionLevel Optimal

$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksum -Value "$hash  $(Split-Path -Leaf $archive)" -Encoding ascii
Remove-Item -LiteralPath $stagingRoot -Recurse -Force

Write-Output "Package: $archive"
Write-Output "SHA256:  $hash"
