param([Parameter(Mandatory)][string]$PackagesPath)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$packageRoot = [IO.Path]::GetFullPath($PackagesPath)
& node (Join-Path $PSScriptRoot 'pin-packages.mjs') verify $packageRoot
if ($LASTEXITCODE -ne 0) { throw 'Package archive verification failed.' }
$pin = Get-Content -LiteralPath (Join-Path $taskRoot 'dependencies/nuget-archives.lock.json') -Raw | ConvertFrom-Json
Add-Type -AssemblyName System.IO.Compression.FileSystem
$verifiedFiles = 0
foreach ($pkg in $pin.packages) {
    $name = $pkg.id.ToLowerInvariant()
    $directory = Join-Path $packageRoot "$name/$($pkg.version)"
    $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $directory "$name.$($pkg.version).nupkg"))
    try {
        foreach ($entry in $archive.Entries) {
            $relative = $entry.FullName
            if ($relative.EndsWith('/')) { continue }
            # OPC/signature metadata is retained inside the pinned archive, not NuGet's extracted payload.
            if ($relative -eq '[Content_Types].xml' -or $relative -eq '_rels/.rels' -or $relative -eq '.signature.p7s' -or $relative.StartsWith('package/services/metadata/core-properties/')) { continue }
            if ($relative.Contains('\') -or [IO.Path]::IsPathRooted($relative) -or @($relative.Split('/') | Where-Object { $_ -eq '..' -or $_ -eq '.' -or $_ -eq '' }).Count -gt 0) { throw 'Unsafe path in pinned package.' }
            $file = [IO.Path]::GetFullPath((Join-Path $directory $relative))
            if (-not $file.StartsWith([IO.Path]::GetFullPath($directory) + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Package payload escapes its cache directory.' }
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing extracted package payload: $($pkg.id)/$relative" }
            if ((Get-Item -LiteralPath $file).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Extracted package payload may not be a symlink.' }
            $stream = $entry.Open()
            try { $expected = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() } finally { $stream.Dispose() }
            $actual = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actual -ne $expected) { throw "Changed extracted package payload: $($pkg.id)/$relative" }
            $verifiedFiles++
        }
    } finally { $archive.Dispose() }
}
Write-Output "Verified $verifiedFiles extracted package payload files against pinned archives."
