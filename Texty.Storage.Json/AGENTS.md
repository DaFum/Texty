# Texty.Storage.Json Agent Instructions

Purpose: Notes for JSON-first storage implementation and repository code.

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for workspace conventions.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Build JSON storage only:** `& $dotnet build Texty.Storage.Json/Texty.Storage.Json.csproj -m:1`

Key locations
- Repository implementations: [Repositories](Repositories/)
- Serialization defaults: [JsonSerializerDefaults.cs](JsonSerializerDefaults.cs)

When to update
- Update when storage contracts or serialization defaults change.
