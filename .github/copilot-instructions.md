# Copilot Agent Instructions for Texty

## Overview
This workspace is for the Texty WinUI-3 Desktop App, which provides system-wide text snippet management with JSON-first persistence, trigger/macro engine, integrations, AI adapters, and portable unpackaged execution.

## Key Documentation
- [README.md](../README.md): Contains architecture, build/test commands, and project structure. **Link, don't duplicate**.

## Build & Test Commands
- Build: `dotnet build Texty.slnx -m:1`
- Test: `dotnet test Texty.Tests/Texty.Tests.csproj -m:1`
- Run App: `dotnet run --project Texty.App/Texty.App.csproj`
- Portable build: See [scripts/make-portable.ps1](../scripts/make-portable.ps1)

## Project Structure
- `Texty.App`: WinUI frontend and runtime bootstrap
- `Texty.Core`: Domain types and public interfaces
- `Texty.Runtime`: Orchestration, triggers, macros, security, sync
- `Texty.Storage.Json`: JSON persistence
- `Texty.Storage.SqlServer`: SQL future layer (not wired in runtime default)
- `Texty.Integrations`: Data resolvers and import
- `Texty.AI`: AI and translation providers
- `Texty.OutlookAddin`: Outlook/Gender-O-Matic module
- `Texty.Tests`: Unit and contract tests

## Conventions & Pitfalls
- JSON-first storage is the runtime default
- Domain contracts in `Texty.Core` (repositories, triggers, insertion, templates, integrations, macro, security, sync)
- Portable builds require correct runtime identifier (see script)
- Runtime includes real Windows keyboard-hook and clipboard trigger providers
- SQL backend project is currently in-memory; migration needed for real SQL
- Excel/AD/SQL resolvers are stubs
- Outlook VSTO integration is pending
- Hardening for rights, licensing, telemetry is planned

## Agent Principles
- **Link, don't embed**: Reference [README.md](../README.md) and scripts for details
- Preserve valuable content, update outdated sections, remove duplication
- Suggest applyTo-based instructions for frontend/backend/tests if workspace grows

## Example Prompts
"Build and run the Texty app"
"Run all unit tests"
"Generate portable build for win-x64"
"Show architecture of Texty.Core"

## ApplyTo-Based Instructions

### Frontend (Texty.App)
- Build: `dotnet build Texty.slnx -m:1`
- Run: `dotnet run --project Texty.App/Texty.App.csproj`
- Portable build: See [scripts/make-portable.ps1](../scripts/make-portable.ps1)
- Reference: [Texty.App/](../Texty.App/)

### Backend (Core, Runtime, Storage, Integrations, AI)
- Build: `dotnet build Texty.slnx -m:1`
- Key contracts: [Texty.Core/Interfaces/](../Texty.Core/Interfaces/)
- Orchestration: [Texty.Runtime/](../Texty.Runtime/)
- Storage: [Texty.Storage.Json/](../Texty.Storage.Json/), [Texty.Storage.SqlServer/](../Texty.Storage.SqlServer/)
- Integrations: [Texty.Integrations/](../Texty.Integrations/)
- AI: [Texty.AI/](../Texty.AI/)

### Tests (Texty.Tests)
- Run all tests: `dotnet test Texty.Tests/Texty.Tests.csproj -m:1`
- Reference: [Texty.Tests/](../Texty.Tests/)

---
Update instructions if new documentation or conventions are added
