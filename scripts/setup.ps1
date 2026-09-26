$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$venv = Join-Path $repoRoot '.venv'

if (-not (Test-Path $venv)) {
    py -m venv $venv
}

$python = Join-Path $venv 'Scripts\python.exe'
$preCommit = Join-Path $venv 'Scripts\pre-commit.exe'

& $python -m pip install --upgrade pip pre-commit
& $preCommit install --install-hooks
& $preCommit install --hook-type pre-push

Write-Host 'Development hooks installed.'
