param(
    [string]$ServerRelease = 'artifacts/mcp-local-v1-final',
    [string]$Output = 'artifacts/simpledraw-plugins-v1',
    [string]$Discovery = '',
    [Parameter(Mandatory=$true)][string]$Notices
)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$releaseRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot $ServerRelease))
$outputRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot $Output))
$archive = "$outputRoot.zip"
$discoveryRoot = if ($Discovery) { [IO.Path]::GetFullPath((Join-Path $taskRoot $Discovery)) } else { $releaseRoot }
$noticeRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot $Notices))
foreach ($target in @($outputRoot, $archive, "$archive.sha256")) {
    if (Test-Path -LiteralPath $target) { throw "Release target already exists: $target" }
}
foreach ($required in @('Beep.SimpleDraw.Mcp.dll', 'Beep.SimpleDraw.Mcp.runtimeconfig.json', 'Beep.SimpleDraw.Mcp.deps.json', 'Beep.Skia.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $releaseRoot $required))) { throw "Missing published payload: $required" }
}
$tools = Get-Content -LiteralPath (Join-Path $discoveryRoot 'mcp-tool-manifest.json') -Raw | ConvertFrom-Json
$schemas = Get-Content -LiteralPath (Join-Path $discoveryRoot 'mcp-component-schemas.json') -Raw | ConvertFrom-Json
# distribution/mcp-plugins/shared/tools.json owns the expected tool set.
$expectedTools = @((Get-Content -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/shared/tools.json') -Raw | ConvertFrom-Json).tools | Sort-Object)
$actualTools = @($tools.tools.name | Sort-Object)
if (($actualTools -join ',') -ne ($expectedTools -join ',') -or $schemas.Count -ne 249) { throw "Expected the $($expectedTools.Count) tools in tools.json and 249 component schemas; the server lists $($actualTools.Count)." }
foreach ($tool in $tools.tools) {
    foreach ($hint in @('readOnlyHint','destructiveHint','openWorldHint')) {
        if ($tool.annotations.$hint -isnot [bool]) { throw "Missing explicit tool annotation: $($tool.name).$hint" }
    }
    if (!$tool.annotations.title) { throw "Missing human-readable tool title: $($tool.name)" }
}
$codexManifest = Get-Content -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/codex/.codex-plugin/plugin.json') -Raw | ConvertFrom-Json
$claudeManifest = Get-Content -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/claude/.claude-plugin/plugin.json') -Raw | ConvertFrom-Json
$version = $codexManifest.version
if ($version -ne $claudeManifest.version -or $version -notmatch '^\d+\.\d+\.\d+$') { throw 'Client plugin versions must match and use semantic versioning.' }
if ($codexManifest.interface.shortDescription.Length -gt 30) { throw 'Plugin subtitle must fit in 30 characters.' }
$licensePath = Join-Path $taskRoot 'LICENSE.txt'
if ((Get-Item -LiteralPath $licensePath).Length -lt 100 -or $codexManifest.license -ne 'Apache-2.0' -or $claudeManifest.license -ne 'Apache-2.0') { throw 'The owner-selected Apache-2.0 product licence is required.' }
$depsPath = Join-Path $releaseRoot 'Beep.SimpleDraw.Mcp.deps.json'
$deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json -AsHashtable
$noticeAudit = Get-Content -LiteralPath (Join-Path $noticeRoot 'audit.json') -Raw | ConvertFrom-Json
if ($noticeAudit.runtime -ne 'win-x64' -or $noticeAudit.depsSha256 -ne (Get-FileHash -LiteralPath $depsPath -Algorithm SHA256).Hash.ToLowerInvariant()) { throw 'Notice audit does not match this Windows x64 payload.' }
foreach ($item in $noticeAudit.files) {
    $path = [IO.Path]::GetFullPath((Join-Path $noticeRoot $item.path))
    if (![IO.Path]::GetRelativePath($noticeRoot,$path).StartsWith('..') -and (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -eq $item.sha256) { continue }
    throw "Notice file is outside its directory or differs from the audit: $($item.path)"
}
foreach ($item in $noticeAudit.verifiedRuntimeFiles) {
    if ((Get-FileHash -LiteralPath (Join-Path $releaseRoot $item.path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $item.sha256) { throw "Audited runtime file differs: $($item.path)" }
}
# Resolve the exact runtime DLL paths from the publish's dependency manifest.
# Never copy arbitrary JSON, machine settings, PDBs or other platforms' native files.
$runtimeFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($library in $deps.targets[$deps.runtimeTarget.name].Values) {
    if ($library.runtime) {
        foreach ($path in $library.runtime.Keys) { if ($path.EndsWith('.dll')) { [void]$runtimeFiles.Add([IO.Path]::GetFileName($path)) } }
    }
    if ($library.runtimeTargets) {
        foreach ($path in $library.runtimeTargets.Keys) {
            if ($library.runtimeTargets[$path].rid -in @('win','win-x64') -and $path.EndsWith('.dll')) { [void]$runtimeFiles.Add($path) }
        }
    }
}
foreach ($path in $runtimeFiles) { if (!(Test-Path -LiteralPath (Join-Path $releaseRoot $path) -PathType Leaf)) { throw "Runtime DLL missing: $path" } }
foreach ($icon in @('simpledraw-icon.png','simpledraw-icon-dark.png')) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $taskRoot "distribution/mcp-plugins/shared/assets/$icon"))
    if ($bytes.Length -gt 5MB -or [Convert]::ToHexString($bytes[0..7]) -ne '89504E470D0A1A0A') { throw "Invalid PNG icon: $icon" }
    $width = [uint32]$bytes[16] * 16777216 + [uint32]$bytes[17] * 65536 + [uint32]$bytes[18] * 256 + $bytes[19]
    $height = [uint32]$bytes[20] * 16777216 + [uint32]$bytes[21] * 65536 + [uint32]$bytes[22] * 256 + $bytes[23]
    if ($width -ne $height -or $width -lt 256 -or $width -gt 4096) { throw "Icon must be square, between 256 and 4096 pixels: $icon" }
}
function Write-Json($path, $value) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
    $value | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $path -Encoding utf8NoBOM
}
foreach ($client in @('codex', 'claude')) {
    $pluginRoot = Join-Path $outputRoot "plugins/simpledraw-$client"
    [IO.Directory]::CreateDirectory($pluginRoot) | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $taskRoot "distribution/mcp-plugins/$client") -Force | Copy-Item -Destination $pluginRoot -Recurse
    Copy-Item -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/shared/skills') -Destination $pluginRoot -Recurse
    Copy-Item -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/shared/assets') -Destination $pluginRoot -Recurse
    Copy-Item -LiteralPath $licensePath -Destination (Join-Path $pluginRoot 'LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $taskRoot 'NOTICE.txt') -Destination $pluginRoot
    Copy-Item -LiteralPath $noticeRoot -Destination (Join-Path $pluginRoot 'notices') -Recurse
    $serverRoot = Join-Path $pluginRoot 'server'
    [IO.Directory]::CreateDirectory($serverRoot) | Out-Null
    foreach ($path in $runtimeFiles) {
        $destination = Join-Path $serverRoot $path
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        Copy-Item -LiteralPath (Join-Path $releaseRoot $path) -Destination $destination
    }
    foreach ($name in @('Beep.SimpleDraw.Mcp.deps.json','Beep.SimpleDraw.Mcp.runtimeconfig.json')) { Copy-Item -LiteralPath (Join-Path $releaseRoot $name) -Destination $serverRoot }
    foreach ($name in @('mcp-tool-manifest.json','mcp-component-schemas.json')) { Copy-Item -LiteralPath (Join-Path $discoveryRoot $name) -Destination $serverRoot }
    Copy-Item -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/shared/README.md') -Destination (Join-Path $pluginRoot 'README.md')
}
Write-Json (Join-Path $outputRoot '.agents/plugins/marketplace.json') @{
    name='beep-tools'; interface=@{displayName='Beep Tools'};
    plugins=@(@{name='simpledraw';source=@{source='local';path='./plugins/simpledraw-codex'};policy=@{installation='AVAILABLE';authentication='ON_INSTALL'};category='Productivity'})
}
Write-Json (Join-Path $outputRoot '.claude-plugin/marketplace.json') @{
    name='beep-tools';owner=@{name='The Tech Idea'};description='Beep portable drawing tools';
    plugins=@(@{name='simpledraw';source='./plugins/simpledraw-claude';description='Draw with all embedded Beep.Skia components';version=$version})
}
Copy-Item -LiteralPath (Join-Path $taskRoot 'distribution/mcp-plugins/shared/README.md') -Destination (Join-Path $outputRoot 'README.md')
Copy-Item -LiteralPath $licensePath -Destination $outputRoot
Copy-Item -LiteralPath (Join-Path $taskRoot 'NOTICE.txt') -Destination $outputRoot
$files = @(Get-ChildItem -LiteralPath $outputRoot -File -Recurse -Force | Sort-Object FullName | ForEach-Object {
    @{path=[IO.Path]::GetRelativePath($outputRoot,$_.FullName).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant();bytes=$_.Length}
})
Write-Json (Join-Path $outputRoot 'manifest.json') @{version=$version;runtime='win-x64';license='Apache-2.0';repository=$codexManifest.repository;tools=$expectedTools.Count;components=249;serverSha256=(Get-FileHash -LiteralPath (Join-Path $releaseRoot 'Beep.SimpleDraw.Mcp.dll')).Hash.ToLowerInvariant();files=$files}
# ZipFile includes dot directories; Compress-Archive can omit hidden entries.
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($outputRoot,$archive)
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath "$archive.sha256" -Encoding utf8NoBOM
Write-Output "Plugin marketplace: $outputRoot"
Write-Output "Installable archive: $archive"
Write-Output "SHA256: $hash"
