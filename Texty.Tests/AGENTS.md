# Texty.Tests Agent Instructions

Purpose: Test suite guidance and commands for running unit and contract tests.

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for testing conventions.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`
- Fallback (if the file is missing): `$dotnet = (Test-Path $dotnet) ? $dotnet : "dotnet"`

Quick commands
- **Run all tests:** `& $dotnet test Texty.Tests/Texty.Tests.csproj -m:1`
- **Build tests only:** `& $dotnet build Texty.Tests/Texty.Tests.csproj -m:1`
- Note: `-m:1` is intentional to keep test runs deterministic on this workspace.

When to update
- Update when test project layout, test targets, or CI commands change.
