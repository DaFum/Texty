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

function Resolve-MSBuild {
    $candidates = @(
        "$env:ProgramFiles\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "$env:ProgramFiles(x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    ) | Where-Object { $_ -and (Test-Path $_) }

    if ($candidates.Count -gt 0) {
        return $candidates[0]
    }

    $msbuildCmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($null -ne $msbuildCmd) {
        return $msbuildCmd.Source
    }

    throw "MSBuild not found. Install Visual Studio with Office/SharePoint development workload."
}

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
