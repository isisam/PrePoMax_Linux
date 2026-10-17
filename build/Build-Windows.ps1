param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$installation = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -property installationPath
$msbuild = Join-Path $installation 'MSBuild\Current\Bin\MSBuild.exe'
# VS 2022 cannot automatically select the installed SDK 10 on the owner's PC.
# This process-only override also works on a machine whose resolver already works.
$dotnetRoot = Split-Path (Get-Command dotnet.exe).Source -Parent
$sdkVersion = (& dotnet --version).Trim()
$savedSdk = $env:MSBuildSDKsPath
$savedWorkloads = $env:MSBuildEnableWorkloadResolver
$savedProcessors = $env:DOTNET_PROCESSOR_COUNT
try {
    $env:MSBuildSDKsPath = Join-Path $dotnetRoot "sdk\$sdkVersion\Sdks"
    $env:MSBuildEnableWorkloadResolver = 'false'
    $env:DOTNET_PROCESSOR_COUNT = '8'
    Push-Location $repo
    try {
        & $msbuild PrePoMax.sln /t:Restore,Build "/p:Configuration=$Configuration" /p:Platform=x64 /p:CoreTargetFrameworks=net48 /p:RestorePackagesConfig=true /m:1 /nr:false /v:minimal
        if ($LASTEXITCODE -ne 0) { throw "Windows build failed: $LASTEXITCODE" }
    } finally { Pop-Location }
} finally {
    $env:MSBuildSDKsPath = $savedSdk
    $env:MSBuildEnableWorkloadResolver = $savedWorkloads
    $env:DOTNET_PROCESSOR_COUNT = $savedProcessors
}
