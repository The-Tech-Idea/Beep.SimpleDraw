param([string]$Output, [string]$BasePath = '/', [string]$PackagesPath)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if (-not $Output) { $Output = Join-Path $taskRoot ('artifacts/release-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$outputRoot = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $outputRoot) { throw 'Release output must be absent. Existing artifacts are never overwritten.' }
if (-not $PackagesPath) { $PackagesPath = Join-Path $taskRoot 'artifacts/packages' }
$packageRoot = [IO.Path]::GetFullPath($PackagesPath)
if ($BasePath -notmatch '^/(?:[A-Za-z0-9_-]+/)*$') { throw 'Base path must be / or slash-terminated ASCII segments, such as /draw/.' }
Push-Location $taskRoot
try {
    # Preparation changes only an absent dependency directory. Existing source is checked only.
    & (Join-Path $PSScriptRoot 'prepare-dependency.ps1')
    $toolchain = & (Join-Path $PSScriptRoot 'verify-toolchain.ps1')
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
    [IO.File]::WriteAllText((Join-Path $outputRoot 'toolchain.json'),($toolchain -join "`n") + "`n")
    function Invoke-Checked([string]$Command, [string[]]$Arguments, [string]$Log) {
        & $Command @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $outputRoot $Log)
        if ($LASTEXITCODE -ne 0) { throw "$Command failed ($LASTEXITCODE). See $Log; publish/package is stopped." }
    }
    $noticeSdkRoot = Split-Path (Get-Command dotnet).Source
    Invoke-Checked 'node' @('scripts/prepare-notices.mjs','verify',$noticeSdkRoot) 'notices-inputs.log'
    # Select only the four Beep multi-target projects; single-target app projects
    # must retain their normal NuGet props imports and one MSBuild instance per output.
    $properties = @('-p:Configuration=Release','-p:BeepSkiaPortableTargetFramework=net10.0','-p:GeneratePackageOnBuild=false','-p:SkipMinVer=true','-p:BeepSkiaLocalBuildCopies=false','-p:TreatWarningsAsErrors=true')
    Invoke-Checked 'dotnet' (@('restore','Beep.SimpleDraw.slnx','--configfile','NuGet.Config','--packages',$packageRoot,'--locked-mode','-p:RestorePackagesWithLockFile=true','-p:NuGetLockFilePath=packages.net10.lock.json')+$properties) 'restore.log'
    # Recheck SDK/pack selection after NuGet-generated imports exist.
    & (Join-Path $PSScriptRoot 'verify-toolchain.ps1') -AfterRestore -PackagesPath $packageRoot -Output (Join-Path $outputRoot 'toolchain.json') | Tee-Object -FilePath (Join-Path $outputRoot 'toolchain-selection.log')
    & (Join-Path $PSScriptRoot 'verify-package-cache.ps1') -PackagesPath $packageRoot | Tee-Object -FilePath (Join-Path $outputRoot 'package-cache.log')
    # Compile and test managed code first. Link the deployment's native WASM once, during publish.
    Invoke-Checked 'dotnet' (@('build','Beep.SimpleDraw.slnx','-c','Release','--no-restore','-p:WasmBuildNative=false','-v:minimal')+$properties) 'build.log'
    Invoke-Checked 'dotnet' (@('test','tests/Beep.SimpleDraw.Tests','-c','Release','--no-restore','--no-build','--logger','trx;LogFileName=tests.trx','--results-directory',(Join-Path $outputRoot 'tests'),'-v:minimal')+$properties) 'csharp-tests.log'
    # dotnet test may exit zero when a missing adapter discovers zero tests.
    # Require a complete executed suite, not merely the command's exit status.
    $xmlSettings = [Xml.XmlReaderSettings]::new()
    $xmlSettings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $xmlSettings.XmlResolver = $null
    $reader = [Xml.XmlReader]::Create((Join-Path $outputRoot 'tests/tests.trx'),$xmlSettings)
    $testResult = [Xml.XmlDocument]::new()
    try { $testResult.Load($reader) } finally { $reader.Dispose() }
    $counters = $testResult.SelectSingleNode('/*[local-name()="TestRun"]/*[local-name()="ResultSummary"]/*[local-name()="Counters"]')
    if (-not $counters -or [int]$counters.total -le 0 -or [int]$counters.executed -ne [int]$counters.total -or [int]$counters.passed -ne [int]$counters.total) { throw 'C# verification did not execute and pass every discovered test. Publish/package is stopped.' }
    $jsTests = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'tests') -Filter '*.test.mjs' -File | Sort-Object Name | ForEach-Object FullName)
    if ($jsTests.Count -eq 0) { throw 'No JavaScript verification tests found.' }
    Invoke-Checked 'node' (@('--test')+$jsTests) 'javascript-tests.log'
    # AOT IL stripping mutates linked managed assemblies. A subsequent interpreter publish
    # can reuse those cached outputs even when every source/package check passes.
    # Preserve the managed build cache as evidence and publish from fresh web intermediates.
    $webIntermediateRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot 'src/Beep.SimpleDraw.Web/obj'))
    $preservedIntermediateRoot = [IO.Path]::GetFullPath((Join-Path $outputRoot 'managed-web-intermediates'))
    if (-not $webIntermediateRoot.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar) -or
        -not $preservedIntermediateRoot.StartsWith($outputRoot + [IO.Path]::DirectorySeparatorChar) -or
        -not (Test-Path -LiteralPath $webIntermediateRoot -PathType Container) -or
        (Test-Path -LiteralPath $preservedIntermediateRoot)) { throw 'Unexpected web intermediate paths; publish/package is stopped.' }
    Move-Item -LiteralPath $webIntermediateRoot -Destination $preservedIntermediateRoot -ErrorAction Stop
    Invoke-Checked 'dotnet' (@('restore','src/Beep.SimpleDraw.Web','--configfile','NuGet.Config','--packages',$packageRoot,'--locked-mode','-p:RestorePackagesWithLockFile=true','-p:NuGetLockFilePath=packages.net10.lock.json')+$properties) 'publish-restore.log'
    & (Join-Path $PSScriptRoot 'verify-toolchain.ps1') -AfterRestore -PackagesPath $packageRoot -Output (Join-Path $outputRoot 'toolchain.json') | Tee-Object -FilePath (Join-Path $outputRoot 'publish-toolchain-selection.log')
    Invoke-Checked 'dotnet' (@('publish','src/Beep.SimpleDraw.Web','-c','Release','--no-restore','-o',(Join-Path $outputRoot 'publish'),'-p:WasmBuildNative=true','-v:minimal')+$properties) 'publish.log'
    $wwwroot = Join-Path $outputRoot 'publish/wwwroot'
    Invoke-Checked 'node' @('scripts/release-assets.mjs','finalize',$wwwroot,$BasePath,(Join-Path $outputRoot 'asset-report.json')) 'assets.log'
    Invoke-Checked 'node' @('scripts/release-notices.mjs',$wwwroot,$noticeSdkRoot,(Join-Path $outputRoot 'runtime-notices.json')) 'runtime-notices.log'
    Invoke-Checked 'node' @('scripts/release-provenance.mjs',$outputRoot,$packageRoot) 'provenance.log'
    Compress-Archive -Path (Join-Path $wwwroot '*') -DestinationPath (Join-Path $outputRoot 'simpledraw-static.zip') -CompressionLevel Optimal
    & (Join-Path $PSScriptRoot 'verify-static-archive.ps1') -Archive (Join-Path $outputRoot 'simpledraw-static.zip') -Wwwroot $wwwroot | Tee-Object -FilePath (Join-Path $outputRoot 'archive.log')
    $archiveHash = (Get-FileHash -LiteralPath (Join-Path $outputRoot 'simpledraw-static.zip') -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText((Join-Path $outputRoot 'simpledraw-static.zip.sha256'),$archiveHash + "  simpledraw-static.zip`n")
    Write-Output "Verified static release: $outputRoot"
} finally { Pop-Location }
