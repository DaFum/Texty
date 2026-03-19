# Texty.App Agent Instructions

Purpose: Guidance for frontend work (WinUI 3 app) and local/portable runs.

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for workspace-wide conventions.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Run app locally:** `& $dotnet run --project Texty.App/Texty.App.csproj`
- **Portable packaging:** [../scripts/make-portable.ps1](../scripts/make-portable.ps1)

Relevant paths
- Pages: [Pages](Pages/)
- Services: [Services](Services/)
- ViewModels: [ViewModels](ViewModels/)

When to update
- Edit this file when frontend commands, packaging steps, or runtime bootstrap change.
