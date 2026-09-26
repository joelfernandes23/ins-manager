param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('format', 'test')]
    [string]$Task
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path (Split-Path -Parent $repoRoot) '.dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }
$solution = Join-Path $repoRoot '727.InsManager.sln'

switch ($Task) {
    'format' { & $dotnet format $solution --verify-no-changes --no-restore }
    'test' { & $dotnet test $solution --configuration Release --no-restore }
}

exit $LASTEXITCODE
