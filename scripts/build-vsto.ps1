param(
    [string]$Configuration = "Debug",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\Texty.OutlookVsto\Texty.OutlookVsto.csproj"
if (!(Test-Path $projectPath)) {
    throw "VSTO project not found: $projectPath"
}

. (Join-Path $PSScriptRoot "msbuild-utils.ps1")

$msbuild = Resolve-MSBuild
Write-Host "Using MSBuild: $msbuild"

& $msbuild $projectPath /t:Build /p:Configuration=$Configuration /p:Platform=$Platform /m
if ($LASTEXITCODE -ne 0) {
    throw "VSTO build failed with exit code $LASTEXITCODE."
}

Write-Host "VSTO build succeeded."
