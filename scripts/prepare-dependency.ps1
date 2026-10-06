param([string]$Path)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$pin = Get-Content -LiteralPath (Join-Path $taskRoot 'dependencies/beep-skia.lock.json') -Raw | ConvertFrom-Json
if (-not $Path) { $Path = Join-Path (Split-Path $taskRoot) 'Beep.Skia' }
$dependencyRoot = [IO.Path]::GetFullPath($Path)
function Invoke-Git([string[]]$Arguments) {
    & git @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git preparation failed with exit code $LASTEXITCODE." }
}
if (-not (Test-Path -LiteralPath $dependencyRoot)) {
    Invoke-Git @('clone','--filter=blob:none','--no-checkout','--config','core.autocrlf=false',$pin.repository,$dependencyRoot)
    Invoke-Git @('-C',$dependencyRoot,'sparse-checkout','init','--cone')
    Invoke-Git (@('-C',$dependencyRoot,'sparse-checkout','set') + @($pin.sourceRoots) + 'Beep.Skia.Tests')
    Invoke-Git @('-C',$dependencyRoot,'checkout','--detach',$pin.baseCommit)
    Invoke-Git @('-C',$dependencyRoot,'apply','--check',(Join-Path $taskRoot 'dependencies/beep-skia.patch'))
    Invoke-Git @('-C',$dependencyRoot,'apply',(Join-Path $taskRoot 'dependencies/beep-skia.patch'))
    foreach ($relative in $pin.addedFiles) {
        $target = [IO.Path]::GetFullPath((Join-Path $dependencyRoot $relative))
        if (-not $target.StartsWith($dependencyRoot + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Pinned added source escapes its checkout.' }
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        Copy-Item -LiteralPath (Join-Path $taskRoot "dependencies/beep-skia-added/$relative") -Destination $target
    }
}
# Existing user checkouts are checked only. Never reset, clean, patch or overwrite one.
& node (Join-Path $PSScriptRoot 'pin-inputs.mjs') verify $dependencyRoot
if ($LASTEXITCODE -ne 0) { throw 'Dependency source verification failed. The existing checkout has not been changed.' }
