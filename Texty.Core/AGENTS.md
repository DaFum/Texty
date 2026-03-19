# Texty.Core Agent Instructions

Purpose: Instructions for core domain work, contracts, models, and public interfaces.

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for workspace conventions.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Build core only:** `& $dotnet build Texty.Core/Texty.Core.csproj -m:1`

Key locations
- Public contracts: [Interfaces](Interfaces/)
- Domain models: [Models](Models/)

When to update
- Update when domain contracts, public interfaces, or build workflows change.
