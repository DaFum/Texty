# Texty.AI Agent Instructions

Purpose: Guidance for AI adapters, translation providers, and AI-related tests.

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for workspace conventions and AI guidelines.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Build AI only:** `& $dotnet build Texty.AI/Texty.AI.csproj -m:1`

Key locations
- AI adapters: [Texty.AI](.)

When to update
- Update when AI provider interfaces, credentials handling, or adapter conventions change.
