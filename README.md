# Texty

WinUI-3 Desktop-App fuer systemweite Textbausteine mit JSON-First Persistenz, optionalem SQL-Backend, Trigger- und Makro-Engine, Integrationsresolvern, KI-Adaptern und portabler unpackaged Ausfuehrung.

## Implementierter Stand

- WinUI-3 App (`Texty.App`) als unpackaged Runtime auf `net10.0-windows10.0.19041.0`.
- 3-Spalten Shell (Explorer, Trefferliste, Editor/Preview) als C#-UI mit ViewModel-Bindings.
- Domain- und API-Vertraege in `Texty.Core`:
  - Repositories: `ISnippetRepository`, `IFolderRepository`, `IVersionRepository`, `ITrashRepository`.
  - Trigger/Insertion: `ITriggerProvider`, `ITriggerEvaluator`, `IInsertionPipeline`.
  - Templates/Formulare, Integrationen, KI/Translation, Macro, Security/Auth, Sync.
- JSON-Storage in `Texty.Storage.Json`:
  - Pro-Entity JSON-Dateien (`snippets`, `folders`, `versions`, `trash`, `assets`).
  - Volltextnahe Suche + In-Memory Suchindex.
- Optionales Team-Backend in `Texty.Storage.SqlServer`:
  - Gleiche Repository-Vertraege, aktuell in-memory SQL-ready Struktur.
- Runtime-Orchestrierung in `Texty.Runtime`:
  - Trigger-Evaluator, Template-Renderer, Formularvalidierung.
  - Transaktionale Clipboard-Insertion-Pipeline.
  - DSL-Makroengine + optionaler PowerShell-Runner mit Trust-Gate.
  - DPAPI-Secret-Schutz, Rollen/Lizenz-Baseline, Folder-Sync-Orchestrator.
  - Produktivitaets-, Dokumentgenerator-, Duplikat- und Bulk-Font-Services.
- Integrationen in `Texty.Integrations`:
  - Resolver: `env`, `csv`, `xml` (funktional), `sql`, `ad`, `excel` (defensiver v1-Stub/Fallback).
  - Dateiimport-Service.
- KI in `Texty.AI`:
  - Provideradapter: OpenAI, OpenRouter, Groq, Langdock.
  - Translation: DeepL + OpenAI-Weg.
- Outlook-Modul in `Texty.OutlookAddin`:
  - Separates Add-in-Modul mit Gender-O-Matic Baseline.

## Projektstruktur

- `Texty.App`: WinUI Frontend und Runtime-Bootstrap.
- `Texty.Core`: Domaintypen + Public Interfaces.
- `Texty.Runtime`: Orchestrierung, Trigger, Macro, Security, Sync.
- `Texty.Storage.Json`: JSON Persistenz.
- `Texty.Storage.SqlServer`: Optionales SQL-Backend (Contract-kompatibel).
- `Texty.Integrations`: Datenresolver und Import.
- `Texty.AI`: KI- und Translation-Provider.
- `Texty.OutlookAddin`: Outlook/Gender-O-Matic Modul.
- `Texty.Tests`: Unit- und Contract-Tests.

## Build und Tests

Voraussetzung: lokales `dotnet` SDK in `%USERPROFILE%\\.dotnet` oder global installiert.

```powershell
$dotnet="$env:USERPROFILE\\.dotnet\\dotnet.exe"
& $dotnet build Texty.slnx -m:1
& $dotnet test Texty.Tests\\Texty.Tests.csproj -m:1
```

App starten:

```powershell
$dotnet="$env:USERPROFILE\\.dotnet\\dotnet.exe"
& $dotnet run --project Texty.App\\Texty.App.csproj
```

## Offene Folgearbeit

- Reale globale Hotkey/Hook Provider statt NoOp-Insertion-Emitter.
- SQL-Backend von in-memory auf echte SQL-Implementierung mit Migrationen.
- Vollstaendige Excel/AD/SQL Resolver-Backends.
- Outlook VSTO Host-Integration.
- Hardening fuer Rechteverwaltung, Lizenzierung und Telemetrie.
