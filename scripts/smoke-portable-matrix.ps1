param(
    [string]$Configuration = "Release",
    [string]$OutputRoot = "",
    [string[]]$RuntimeIdentifiers = @("win-x86", "win-x64", "win-arm64", "win10-x86", "win10-x64", "win10-arm64")
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $PSScriptRoot "..\portable\smoke-matrix"
}

$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$makePortableScript = Join-Path $PSScriptRoot "make-portable.ps1"
if (!(Test-Path $makePortableScript)) {
    throw "Missing script: $makePortableScript"
}

$hostArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$rids = $RuntimeIdentifiers
$results = New-Object System.Collections.Generic.List[object]

function Test-CanRunRid([string]$rid, [string]$architecture) {
    if ($rid -like "*arm64") {
        return $architecture -eq "arm64"
    }

    # x86/x64 builds may run on ARM64 only when Windows-on-ARM emulation is available.
    return $true
}

foreach ($rid in $rids) {
    $entry = [ordered]@{
        runtimeIdentifier = $rid
        build = "failed"
        smoke = "not-run"
        reason = ""
        outputDir = [System.IO.Path]::GetFullPath((Join-Path $OutputRoot "Texty-$rid"))
    }

    try {
        & $makePortableScript -RuntimeIdentifier $rid -Configuration $Configuration -OutputDirectory $entry.outputDir -KillRunningTexty
        $entry.build = "passed"
    }
    catch {
        $entry.reason = "publish failed: $($_.Exception.Message)"
        $results.Add([pscustomobject]$entry)
        continue
    }

    if (-not (Test-CanRunRid $rid $hostArch)) {
        $entry.smoke = "skipped"
        $entry.reason = "host architecture '$hostArch' cannot execute '$rid'."
        $results.Add([pscustomobject]$entry)
        continue
    }

    $exePath = Join-Path $entry.outputDir "Texty.App.exe"
    if (!(Test-Path $exePath)) {
        $entry.smoke = "failed"
        $entry.reason = "missing executable: $exePath"
        $results.Add([pscustomobject]$entry)
        continue
    }

    $process = $null
    try {
        $process = Start-Process -FilePath $exePath -WorkingDirectory $entry.outputDir -PassThru
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds(15)
        $windowSeen = $false
        do {
            Start-Sleep -Milliseconds 300
            if ($process.HasExited) {
                break
            }

            $process.Refresh()
            if ($process.MainWindowHandle -ne 0) {
                $windowSeen = $true
                break
            }
        }
        while ([DateTimeOffset]::UtcNow -lt $deadline)

        if ($process.HasExited) {
            $entry.smoke = "failed"
            $entry.reason = "process exited early with code $($process.ExitCode)"
        }
        elseif ($windowSeen) {
            $entry.smoke = "passed"
        }
        else {
            $entry.smoke = "failed"
            $entry.reason = "no top-level window detected within timeout"
        }
    }
    catch {
        $entry.smoke = "failed"
        $entry.reason = "smoke run failed: $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            try {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
            catch {
                # Intentionally ignoring cleanup failures; process may already be gone.
            }
        }
    }

    $results.Add([pscustomobject]$entry)
}

$reportPath = Join-Path $OutputRoot "portable-smoke-report.json"
$results | ConvertTo-Json -Depth 5 | Set-Content -Path $reportPath -Encoding utf8

Write-Host "Portable smoke report: $reportPath"
$failures = $results | Where-Object { $_.build -ne "passed" -or $_.smoke -eq "failed" }
if ($failures.Count -gt 0) {
    throw "Portable matrix smoke failed for $($failures.Count) runtime identifiers. See $reportPath"
}

Write-Host "Portable matrix smoke passed."
