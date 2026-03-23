# Texty.Runtime Agent Instructions

Purpose: Notes for runtime orchestration, triggers, macros, security, sync, and insertion pipeline.

See [../.github/copilot-instructions.md](../.github/copilot-instructions.md) for workspace agent guidance.

Dotnet usage
- Preferred CLI: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper: `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Build runtime only:** `& $dotnet build Texty.Runtime/Texty.Runtime.csproj -m:1`

Key locations
- Runtime orchestration: [Bootstrap](Bootstrap/)
- Triggers and templating: [Triggering](Triggering/) · [Templating](Templating/)
- Insertion and clipboard: [Insertion](Insertion/) · [Clipboard](Clipboard/)

When to update
- Update when orchestration flows, macro engine, trigger plumbing, or sync behavior changes.
