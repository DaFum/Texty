# Texty

WinUI-3 Desktop-App fuer systemweite Textbausteine mit JSON-First Persistenz, Trigger- und Makro-Engine, Integrationsresolvern, KI-Adaptern und portabler unpackaged Ausfuehrung.

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
- `Texty.Storage.SqlServer` bleibt als nicht-verdrahtete Zukunftsschicht im Repo vorhanden.
- Runtime-Orchestrierung in `Texty.Runtime`:
  - Trigger-Evaluator, Template-Renderer, Formularvalidierung.
  - Komponierte Triggerquellen (Hotkey, Clipboard, Autotext/Regex-Textfluss via Keyboard-Hook).
  - Transaktionale Clipboard-Insertion-Pipeline.
  - DSL-Makroengine + optionaler PowerShell-Runner mit Trust-Gate.
  - DPAPI-Secret-Schutz, Rollen/Lizenz-Baseline, Folder-Sync-Orchestrator.
  - Produktivitaets-, Dokumentgenerator-, Duplikat- und Bulk-Font-Services.
- Integrationen in `Texty.Integrations`:
  - Resolver: `env`, `csv`, `xml` (funktional), `sql`, `ad`, `excel` (defensiver v1-Stub/Fallback).
  - Dateiimport-Service.
- KI in `Texty.AI`:
  - Provideradapter: OpenAI, OpenRouter, Groq, Langdock.
  - Translation: OpenAI-Weg.
- Outlook-Modul in `Texty.OutlookAddin`:
  - Separates Add-in-Modul mit Gender-O-Matic Baseline.

## Projektstruktur

- `Texty.App`: WinUI Frontend und Runtime-Bootstrap.
- `Texty.Core`: Domaintypen + Public Interfaces.
- `Texty.Runtime`: Orchestrierung, Trigger, Macro, Security, Sync.
- `Texty.Storage.Json`: JSON Persistenz.
- `Texty.Storage.SqlServer`: SQL-Zukunftsschicht (derzeit nicht im Runtime-Default verdrahtet).
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

## Outlook VSTO Host (x64)

Das produktive Outlook-Add-in liegt im Projekt `Texty.OutlookVsto` (VSTO, .NET Framework 4.8, x64).

Build:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\\build-vsto.ps1 -Configuration Debug -Platform x64
```

Signiertes ClickOnce-Publish:

```powershell
$env:TEXTY_VSTO_CERT_PATH = "C:\\certs\\texty-vsto.pfx"
$env:TEXTY_VSTO_CERT_PASSWORD = "<passwort>"
powershell -ExecutionPolicy Bypass -File scripts\\publish-vsto.ps1 -Configuration Release -Platform x64
```

Ohne Visual-Studio-Office-Tooling oder Zertifikat schlägt der Publish bewusst mit klarer Fehlermeldung fehl (fail-fast).

## Portable Matrix Smoke

Für reproduzierbare portable Smokes über alle Ziel-RIDs:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\\smoke-portable-matrix.ps1 -Configuration Release
```

Der Lauf erzeugt einen maschinenlesbaren Report unter `portable\\smoke-matrix\\portable-smoke-report.json` und liefert einen Fehlercode bei fehlgeschlagenen Builds/Smokes.

## Offene Folgearbeit

- SQL-Backend von in-memory auf echte SQL-Implementierung mit Migrationen (außerhalb Single-User-v1).
- Vollstaendige Excel/AD/SQL Resolver-Backends.
- Outlook VSTO Host-Integration.
- Hardening fuer Rechteverwaltung, Lizenzierung und Telemetrie.
