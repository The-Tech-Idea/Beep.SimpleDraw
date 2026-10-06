param([int]$Port = 5188, [string]$ReleasePath)
# A local preview server for the published static artifact; not a production hosting requirement.
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $ReleasePath) { $ReleasePath = Join-Path $taskRoot 'artifacts/candidate' }
$releaseRoot = [IO.Path]::GetFullPath($ReleasePath)
$taskSite = Join-Path $releaseRoot 'publish/wwwroot'
$report = Join-Path $releaseRoot 'asset-report.json'
if (-not (Test-Path -LiteralPath (Join-Path $taskSite 'index.html')) -or -not (Test-Path -LiteralPath $report)) { throw 'Build a verified candidate with scripts/build.ps1 first, or pass its ReleasePath.' }
& node (Join-Path $PSScriptRoot 'serve-static.mjs') $taskSite $report $Port
if ($LASTEXITCODE -ne 0) { throw 'The verified static preview failed.' }
