param(
    [string]$ServerRelease = 'artifacts/mcp-local-v1-final',
    [string]$Output = 'artifacts/simpledraw-plugins-v1'
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$releaseRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot $ServerRelease))
$outputRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot $Output))
$archive = "$outputRoot.zip"
foreach ($target in @($outputRoot, $archive, "$archive.sha256")) {
    if (Test-Path -LiteralPath $target) { throw "Release target already exists: $target" }
}
foreach ($required in @('Beep.SimpleDraw.Mcp.dll', 'Beep.SimpleDraw.Mcp.runtimeconfig.json', 'Beep.Skia.dll', 'mcp-tool-manifest.json', 'mcp-component-schemas.json')) {
    if (!(Test-Path -LiteralPath (Join-Path $releaseRoot $required))) { throw "Missing published payload: $required" }
}
$tools = Get-Content -LiteralPath (Join-Path $releaseRoot 'mcp-tool-manifest.json') -Raw | ConvertFrom-Json
$schemas = Get-Content -LiteralPath (Join-Path $releaseRoot 'mcp-component-schemas.json') -Raw | ConvertFrom-Json
if ($tools.tools.Count -ne 31 -or $schemas.Count -ne 249) { throw 'Expected 31 tools and 249 component schemas.' }
function Write-Json($path, $value) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
    $value | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $path -Encoding utf8NoBOM
}
foreach ($client in @('codex', 'claude')) {
    $pluginRoot = Join-Path $outputRoot "plugins/simpledraw-$client"
    [IO.Directory]::CreateDirectory($pluginRoot) | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $taskRoot "distribution/mcp-plugins/$client") -Force | Copy-Item -Destination $pluginRoot -Recurse
    Copy-Item -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/shared/skills') -Destination $pluginRoot -Recurse
    $serverRoot = Join-Path $pluginRoot 'server'
    [IO.Directory]::CreateDirectory($serverRoot) | Out-Null
    # Exclude machine-specific client snippets, acceptance paths and old manifests.
    Get-ChildItem -LiteralPath $releaseRoot -File | Where-Object { $_.Extension -in @('.dll','.json','.xml') -and $_.Name -notin @('acceptance.json','manifest.json','claude-desktop.json') } | Copy-Item -Destination $serverRoot
    Copy-Item -LiteralPath (Join-Path $releaseRoot 'runtimes') -Destination $serverRoot -Recurse
    Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/mcp/PLUGINS.md') -Destination (Join-Path $pluginRoot 'README.md')
}
Write-Json (Join-Path $outputRoot '.agents/plugins/marketplace.json') @{
    name='beep-tools'; interface=@{displayName='Beep Tools'};
    plugins=@(@{name='simpledraw';source=@{source='local';path='./plugins/simpledraw-codex'};policy=@{installation='AVAILABLE';authentication='ON_INSTALL'};category='Productivity'})
}
Write-Json (Join-Path $outputRoot '.claude-plugin/marketplace.json') @{
    name='beep-tools';owner=@{name='The Tech Idea'};description='Beep portable drawing tools';
    plugins=@(@{name='simpledraw';source='./plugins/simpledraw-claude';description='Draw with all embedded Beep.Skia components';version='1.0.0'})
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'docs/mcp/PLUGINS.md') -Destination (Join-Path $outputRoot 'README.md')
$files = @(Get-ChildItem -LiteralPath $outputRoot -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
    @{path=[IO.Path]::GetRelativePath($outputRoot,$_.FullName).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant();bytes=$_.Length}
})
Write-Json (Join-Path $outputRoot 'manifest.json') @{version='1.0.0';tools=31;components=249;serverSha256=(Get-FileHash -LiteralPath (Join-Path $releaseRoot 'Beep.SimpleDraw.Mcp.dll')).Hash.ToLowerInvariant();files=$files}
# ZipFile includes dot directories; Compress-Archive can omit hidden entries.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($outputRoot,$archive)
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath "$archive.sha256" -Encoding utf8NoBOM
Write-Output "Plugin marketplace: $outputRoot"
Write-Output "Installable archive: $archive"
Write-Output "SHA256: $hash"
