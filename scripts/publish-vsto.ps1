param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [string]$PublishDir = ""
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\Texty.OutlookVsto\Texty.OutlookVsto.csproj"
if (!(Test-Path $projectPath)) {
    throw "VSTO project not found: $projectPath"
}

. (Join-Path $PSScriptRoot "msbuild-utils.ps1")

$certPath = $env:TEXTY_VSTO_CERT_PATH
$certPassword = $env:TEXTY_VSTO_CERT_PASSWORD
if ([string]::IsNullOrWhiteSpace($certPath) -or !(Test-Path $certPath)) {
    throw "Missing signing certificate. Set TEXTY_VSTO_CERT_PATH to a valid .pfx path."
}

if ([string]::IsNullOrWhiteSpace($certPassword)) {
    throw "Missing signing certificate password. Set TEXTY_VSTO_CERT_PASSWORD."
}

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = Join-Path $PSScriptRoot "..\portable\Texty.OutlookVsto"
}
$PublishDir = [System.IO.Path]::GetFullPath($PublishDir)
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

$msbuild = Resolve-MSBuild
Write-Host "Using MSBuild: $msbuild"
Write-Host "Publishing signed ClickOnce manifest to: $PublishDir"

& $msbuild $projectPath `
    /t:Publish `
    /p:Configuration=$Configuration `
    /p:Platform=$Platform `
    /p:PublishDir="$PublishDir\" `
    /p:SignManifests=true `
    /p:ManifestKeyFile="$certPath" `
    /p:ManifestCertificatePassword="$certPassword" `
    /m

if ($LASTEXITCODE -ne 0) {
    throw "VSTO publish failed with exit code $LASTEXITCODE."
}

Write-Host "VSTO publish completed."
