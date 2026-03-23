# Texty Agent Instructions

Purpose: Central guidance for workspace-specific agent instructions and conventions.

Primary references: [.github/copilot-instructions.md](.github/copilot-instructions.md) · [README.md](README.md)

Dotnet usage
- Use the local SDK-installed CLI directly: `%USERPROFILE%\.dotnet\dotnet.exe`
- PowerShell helper:
  - `$dotnet = "$env:USERPROFILE\\.dotnet\\dotnet.exe"`
  - `& $dotnet --info`
- If `dotnet` isn't on `PATH`, always run commands with `& $dotnet ...`

Quick commands
- **Build workspace:** `& $dotnet build Texty.slnx -m:1`
- **Run app:** `& $dotnet run --project Texty.App/Texty.App.csproj`
- **Run tests:** `& $dotnet test Texty.Tests/Texty.Tests.csproj -m:1`

Areas (short)
- **Frontend:** [Texty.App](Texty.App/) — WinUI 3 app, UI, and portable packaging
- **Core & Runtime:** [Texty.Core](Texty.Core/), [Texty.Runtime](Texty.Runtime/) — Domain contracts and orchestration
- **Storage & Integrations:** [Texty.Storage.Json](Texty.Storage.Json/), [Texty.Storage.SqlServer](Texty.Storage.SqlServer/), [Texty.Integrations](Texty.Integrations/)
- **AI:** [Texty.AI](Texty.AI/) — AI adapters and translation providers
- **Tests:** [Texty.Tests](Texty.Tests/) — unit and contract tests

Agent principles
- Reference central docs rather than duplicating content
- Keep area `AGENTS.md` concise with quick commands and important links
- Update these files when workflows, build commands, or conventions change

Keep this file focused. Add detailed guidance in area-specific `AGENTS.md` files.
