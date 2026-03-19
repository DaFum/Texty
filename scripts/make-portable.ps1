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
$dotnetProgramFiles = if ($env:ProgramFiles) { Join-Path $env:ProgramFiles "dotnet\dotnet.exe" } else { $null }
$dotnetProgramFilesX86 = if (${env:ProgramFiles(x86)}) { Join-Path ${env:ProgramFiles(x86)} "dotnet\dotnet.exe" } else { $null }
$dotnet = @($dotnetUser, $dotnetProgramFiles, $dotnetProgramFilesX86) |
    Where-Object { $_ -and (Test-Path $_) } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($dotnet)) {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -ne $dotnetCommand) {
        $dotnet = $dotnetCommand.Source
    }
    else {
        throw "dotnet executable not found. Install .NET SDK and retry."
    }
}
$rid = $RuntimeIdentifier.ToLowerInvariant()

$platform = switch ($rid) {
    "win-x64" { "x64" }
    "win10-x64" { "x64" }
    "win-x86" { "x86" }
    "win10-x86" { "x86" }
    "win-arm64" { "ARM64" }
    "win10-arm64" { "ARM64" }
    default { throw "Unsupported RuntimeIdentifier '$RuntimeIdentifier'. Supported: win-x86, win-x64, win-arm64, win10-x86, win10-x64, win10-arm64." }
}

Write-Host "Publishing portable build..."
Write-Host "Project : $projectPath"
Write-Host "Runtime : $RuntimeIdentifier"
Write-Host "Platform: $platform"
Write-Host "Config  : $Configuration"
Write-Host "Output  : $OutputDirectory"

if ($KillRunningTexty) {
    Get-Process Texty.App -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

# Intentionally override publish-profile defaults (for example win-x64.pubxml) for unpackaged WinUI reliability.
# PublishReadyToRun stays disabled here to keep portable inner-loop builds fast and predictable across RIDs.
& $dotnet publish $projectPath `
    -c $Configuration `
    -r $RuntimeIdentifier `
    --self-contained true `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -p:PublishSingleFile=false `
    -p:Platform=$platform `
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
