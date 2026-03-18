param(
    [string]$RuntimeIdentifier = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "",
    [switch]$Zip,
    [switch]$KillRunningTexty
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot "..\portable\Texty-$RuntimeIdentifier"
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Texty.App\Texty.App.csproj"))
$dotnetUser = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
$dotnet = if (Test-Path $dotnetUser) { $dotnetUser } else { "dotnet" }

Write-Host "Publishing portable build..."
Write-Host "Project : $projectPath"
Write-Host "Runtime : $RuntimeIdentifier"
Write-Host "Config  : $Configuration"
Write-Host "Output  : $OutputDirectory"

if ($KillRunningTexty) {
    Get-Process Texty.App -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

& $dotnet publish $projectPath `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:PublishSingleFile=false `
    -p:Platform=x64 `
    -o $OutputDirectory

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$startScript = Join-Path $OutputDirectory "Start-Texty.cmd"
"@echo off`r`nsetlocal`r`nstart "" ""%~dp0Texty.App.exe""`r`n" | Set-Content -Path $startScript -Encoding ascii

if ($Zip) {
    $zipPath = "$OutputDirectory.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $OutputDirectory "*") -DestinationPath $zipPath
    Write-Host "Portable zip created: $zipPath"
}

Write-Host "Portable build ready."
Write-Host "Run: $startScript"
