param([Parameter(Mandatory)][string]$Archive, [Parameter(Mandatory)][string]$Wwwroot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = [IO.Path]::GetFullPath($Wwwroot)
$expected = @{}
foreach ($file in Get-ChildItem -LiteralPath $root -File -Recurse) {
    $relative = [IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
    $expected[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
}
$zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($Archive))
$seen = @{}
try {
    foreach ($entry in $zip.Entries) {
        $relative = $entry.FullName.Replace('\','/')
        if ($relative.EndsWith('/')) { continue }
        if (-not $expected.ContainsKey($relative) -or $seen.ContainsKey($relative)) { throw "Unexpected or duplicate release ZIP entry: $relative" }
        $stream = $entry.Open()
        try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() } finally { $stream.Dispose() }
        if ($hash -ne $expected[$relative]) { throw "Release ZIP differs from verified wwwroot: $relative" }
        $seen[$relative] = $true
    }
    if ($seen.Count -ne $expected.Count) { throw 'Release ZIP is missing verified files.' }
} finally { $zip.Dispose() }
Write-Output "Verified all $($seen.Count) ZIP payload entries against wwwroot."
