param([string]$Output = "artifacts/mcp-local-v1-final")
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$outputRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot $Output))
if (Test-Path -LiteralPath $outputRoot) { throw 'MCP output must be absent; existing releases are retained.' }
Push-Location $taskRoot
try {
    $properties = @('-p:BeepSkiaPortableTargetFramework=net10.0','-p:GeneratePackageOnBuild=false','-p:SkipMinVer=true','-p:BeepSkiaLocalBuildCopies=false','-p:TreatWarningsAsErrors=true','-p:NuGetLockFilePath=packages.net10.lock.json','-p:RestorePackagesWithLockFile=true')
    & dotnet restore Beep.SimpleDraw.Mcp.slnx --locked-mode @properties
    if ($LASTEXITCODE -ne 0) { throw 'MCP locked restore failed.' }
    & dotnet test tests/Beep.SimpleDraw.Mcp.Tests -c Release --no-restore @properties
    if ($LASTEXITCODE -ne 0) { throw 'MCP verification failed.' }
    & dotnet publish src/Beep.SimpleDraw.Mcp -c Release --no-restore --self-contained false -o $outputRoot @properties
    if ($LASTEXITCODE -ne 0) { throw 'MCP publication failed.' }
    Copy-Item -LiteralPath docs/mcp/README.md -Destination (Join-Path $outputRoot 'README.md')
    Write-Output "Published MCP server: $outputRoot"
} finally { Pop-Location }
