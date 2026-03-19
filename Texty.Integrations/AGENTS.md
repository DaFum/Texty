# Texty.Integrations Agent Instructions

Purpose: Notes for data resolvers, importers, and integration adapters.

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for workspace-wide guidance.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Build integrations only:** `& $dotnet build Texty.Integrations/Texty.Integrations.csproj -m:1`

Key locations
- Resolver implementations: [Resolvers](Resolvers/)
- Import services: [FileImportService.cs](FileImportService.cs)

When to update
- Update when integration contracts, resolver patterns, or build steps change.
