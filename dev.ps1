param(
    [Parameter(Position = 0)]
    [ValidateSet('setup', 'run', 'scan', 'build', 'test', 'format', 'check')]
    [string]$Task = 'run',

    [Parameter(ValueFromRemainingArguments)]
    [string[]]$Arguments = @()
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$localDotnet = Join-Path (Split-Path -Parent $repoRoot) '.dotnet\dotnet.exe'
$dotnet = if (Test-Path $localDotnet) { $localDotnet } else { 'dotnet' }
$solution = Join-Path $repoRoot 'InsManager.sln'
$app = Join-Path $repoRoot 'src\InsManager.App\InsManager.App.csproj'
$diagnostics = Join-Path $repoRoot 'src\InsManager.Diagnostics\InsManager.Diagnostics.csproj'

function Invoke-DotNet {
    & $dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE"
    }
}

switch ($Task) {
    'setup' {
        & (Join-Path $repoRoot 'scripts\setup.ps1')
        Invoke-DotNet restore $solution
    }
    'run' {
        Invoke-DotNet run --project $app
    }
    'scan' {
        Invoke-DotNet run --project $diagnostics -- @Arguments
    }
    'build' {
        Invoke-DotNet build $solution --configuration Release
    }
    'test' {
        Invoke-DotNet test $solution --configuration Release
    }
    'format' {
        Invoke-DotNet format $solution
    }
    'check' {
        Invoke-DotNet restore $solution
        Invoke-DotNet format $solution --verify-no-changes --no-restore
        Invoke-DotNet build $solution --configuration Release --no-restore
        Invoke-DotNet test $solution --configuration Release --no-build
    }
}
