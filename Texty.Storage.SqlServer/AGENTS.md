# Texty.Storage.SqlServer Agent Instructions

Purpose: Guidance for the SQL Server-backed storage layer (contract-compatible with JSON storage).

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for workspace conventions.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Build SQL storage only:** `& $dotnet build Texty.Storage.SqlServer/Texty.Storage.SqlServer.csproj -m:1`

Key locations
- SQL options/state: [SqlServerStorageOptions.cs](SqlServerStorageOptions.cs)
- Repositories: [Repositories](Repositories/)

When to update
- Update when SQL migration, schema, or repository contracts change.
