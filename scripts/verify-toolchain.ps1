param([string]$Output, [switch]$AfterRestore, [string]$PackagesPath)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$pin = Get-Content -LiteralPath (Join-Path $taskRoot 'dependencies/toolchain.lock.json') -Raw | ConvertFrom-Json
$sdkPin = Get-Content -LiteralPath (Join-Path $taskRoot 'global.json') -Raw | ConvertFrom-Json
Push-Location $taskRoot
try {
    $sdk = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdk -ne $pin.sdk -or $sdkPin.sdk.version -ne $pin.sdk -or $sdkPin.sdk.rollForward -ne 'disable') { throw 'The selected .NET SDK differs from the reviewed toolchain lock.' }
    $nodeVersion = (& node --version).Trim().TrimStart('v')
    if ($LASTEXITCODE -ne 0 -or $nodeVersion -ne $pin.node) { throw 'The selected Node version differs from the reviewed toolchain lock.' }
    if (-not [OperatingSystem]::IsWindows()) { throw 'This build/test gate currently requires Windows for the retained Skia native tests. The published app is browser portable.' }
    $properties = & dotnet msbuild src/Beep.SimpleDraw.Web/Beep.SimpleDraw.Web.csproj -p:Configuration=Release -p:BeepSkiaPortableTargetFramework=net10.0 -getProperty:NetCoreRoot,WasmAppBuilderTasksAssemblyPath,EmscriptenVersion,EmscriptenUpstreamEmscriptenPath,EmscriptenNodeBinPath,EmscriptenCacheSdkCacheDir,RunAOTCompilation,WasmStripILAfterAOT -getItem:NativeFileReference
    if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the selected WASM toolchain.' }
    $properties = ($properties -join "`n") | ConvertFrom-Json
    $dotnetRoot = $properties.Properties.NetCoreRoot
    $selectedWasmTask = [IO.Path]::GetFullPath($properties.Properties.WasmAppBuilderTasksAssemblyPath)
    $expectedWasmTask = [IO.Path]::GetFullPath((Join-Path $dotnetRoot "packs/Microsoft.NET.Runtime.WebAssembly.Sdk/$($pin.wasmPack)/tasks/net10.0/WasmAppBuilder.dll"))
    if ($selectedWasmTask -ne $expectedWasmTask -or -not (Test-Path -LiteralPath $selectedWasmTask)) { throw 'The selected WASM SDK differs from the reviewed pack.' }
    $manifestFile = Join-Path $dotnetRoot "sdk-manifests/10.0.100/microsoft.net.workload.mono.toolchain.current/$($pin.wasmManifest)/WorkloadManifest.json"
    $manifest = Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json
    if ($manifest.version -ne $pin.wasmManifest) { throw 'WASM manifest version differs from its pin.' }
    foreach ($pack in $manifest.workloads.'wasm-tools'.packs) {
        if ($manifest.packs.$pack.version -ne $pin.wasmPack) { throw "Unexpected WASM pack version: $pack" }
    }
    foreach ($pack in @('Microsoft.NET.Runtime.WebAssembly.Sdk','Microsoft.NETCore.App.Runtime.Mono.browser-wasm','Microsoft.NETCore.App.Runtime.AOT.win-x64.Cross.browser-wasm','Microsoft.NET.Runtime.MonoAOTCompiler.Task','Microsoft.NET.Runtime.MonoTargets.Sdk')) {
        if (-not (Test-Path -LiteralPath (Join-Path $dotnetRoot "packs/$pack/$($pin.wasmPack)"))) { throw "Missing pinned WASM build pack: $pack" }
    }
    if ($properties.Properties.EmscriptenVersion -ne $pin.emscripten) { throw 'The selected Emscripten version differs from its pin.' }
    foreach ($selection in @(
        @{ Property='EmscriptenUpstreamEmscriptenPath'; Pack='Sdk'; Suffix='tools/emscripten' },
        @{ Property='EmscriptenNodeBinPath'; Pack='Node'; Suffix='tools/bin' },
        @{ Property='EmscriptenCacheSdkCacheDir'; Pack='Cache'; Suffix='tools/emscripten/cache' }
    )) {
        $actual = [IO.Path]::GetFullPath($properties.Properties.($selection.Property)).TrimEnd('\','/')
        $expected = [IO.Path]::GetFullPath((Join-Path $dotnetRoot "packs/Microsoft.NET.Runtime.Emscripten.$($pin.emscripten).$($selection.Pack).win-x64/$($pin.emscriptenPack)/$($selection.Suffix)")).TrimEnd('\','/')
        if ($actual -ne $expected -or -not (Test-Path -LiteralPath $expected)) { throw "The selected Emscripten $($selection.Pack) pack differs from its pin." }
    }
    $result = [ordered]@{ sdk=$sdk; node=$nodeVersion; wasmManifest=$manifest.version; wasmPack=$pin.wasmPack; emscripten=$pin.emscripten; emscriptenPack=$pin.emscriptenPack; configuration=$pin.configuration; framework=$pin.framework; nugetLockFile=$pin.nugetLockFile; runAOTCompilation=$properties.Properties.RunAOTCompilation; stripILAfterAOT=$properties.Properties.WasmStripILAfterAOT }
    if ($AfterRestore) {
        if (-not $PackagesPath) { throw 'The restored package directory is required to verify native Skia selection.' }
        $expectedNative = [IO.Path]::GetFullPath((Join-Path $PackagesPath 'skiasharp.nativeassets.webassembly/4.150.1/buildTransitive/netstandard1.0/libSkiaSharp.a/3.1.56/st,simd/libSkiaSharp.a'))
        $native = @($properties.Items.NativeFileReference | Where-Object { [IO.Path]::GetFullPath($_.FullPath) -eq $expectedNative })
        if ($native.Count -ne 1 -or -not (Test-Path -LiteralPath $expectedNative)) { throw 'Missing exact native Skia reference. Restore imports/framework settings must be corrected before publishing.' }
        $result.nativeSkiaVerified = $true
        $result.nativeSkia = 'SkiaSharp.NativeAssets.WebAssembly/4.150.1; Emscripten 3.1.56; st,simd'
    }
    $json = $result | ConvertTo-Json
    if ($Output) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($Output),$json + "`n") }
    $json
} finally { Pop-Location }
