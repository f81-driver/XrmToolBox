# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

Formula81 XrmToolBox tools (GitHub: `f81-driver/XrmToolBox`, GPL-3.0). Two tools ship from this repo:

- **Audit Goggles**: explores Dataverse / Dynamics 365 audit history across up to 100 selected records, with
  filtering, column selection, color coding, and FetchXML Builder integration.
- **Meta Scanner**: a friendlier metadata browser. Table list with favorites, table details (columns, choices,
  relationships, keys, privileges, properties) and a scan that searches every table, column and choice value at once.

This folder is its **own git repo** (default branch `main`), separate from the sibling `coachandthegame` repo.

## Layout

```
Tools.sln                 Main solution: AuditGoggles + MetaScanner + 5 shared projects
AuditGoggles/             The plugin (old-style csproj, .NET Framework 4.8.1)
  AuditGogglesPlugin.cs          MEF export ([Export(IXrmToolBoxPlugin)]), metadata, icons
  AuditGogglesPluginControl.cs   PluginControlBase: all Dataverse calls, WorkAsync, dialogs, settings, message bus
  AuditGogglesSettings.cs        Per-connection settings (favorite entities)
  Views/ Windows/ Controls/      WPF (XAML) UI, hosted in WinForms via ElementHost
  ViewModels/                    AuditEntity / AuditRecord / EntityAudit view models
  Models/ Components/ Events/    Plain data types, result containers, custom event delegates
  Helpers/                       EntityAuditHelper (audit parsing), FetchXmlHelper
  Caches/                        EntityNameCache (singleton)
  Mocks/                         Design-time XAML data only (d:ItemsSource), not tests
  version.json                   major/minor used by CI to build the version number
  AuditGoggles.nuspec            NuGet package for the XrmToolBox plugin store
  CHANGELOG.md                   Release notes (linked from the nuspec)
MetaScanner/              The metadata plugin, same structure and conventions as AuditGoggles
  MetaScannerPluginControl.cs    All Dataverse calls (WorkAsync), metadata caches, navigation, settings
  MetaScannerSettings.cs         Per-connection settings (favorite tables, helper columns, table sort)
  Views/MetaScannerView.xaml     The whole UI: table list, table details tabs, scan results
  ViewModels/                    EntityList / EntityDetail / Scan view models
  Models/                        MetaEntity, MetaAttribute, MetaRelationship, ScanResult, ...
  Helpers/                       MetadataHelper (friendly type names, details), PropertyHelper (all properties
                                 via reflection), ScanHelper (search), CsvExportHelper
  version.json, MetaScanner.nuspec, CHANGELOG.md   As for AuditGoggles
Shared/                   Shared projects (.shproj/.projitems), compiled INTO the plugin assembly
  Core/      ObservableObject, Queue/String extensions, EnumHelper
  Json/      JToken extensions
  Parts/     WPF building blocks: RelayCommand, converters, FilterInputControl, TimePicker, WindowUtility
  Xrm/       Early-bound entities (Audit, WebResource, GlobalOptionSets), ColumnNames partials,
             metadata extensions, EntityIconCache, embedded entity icons
  XrmParts/  IIconifiable + converter
EarlyBound/               Early-bound generation (separate EarlyBound.sln)
  Generator/XrmContext/          Delegate XrmContext binaries + Run.ps1 / Run.fsx
docs/images/              Screenshots used by the README / wiki, and MetaScanner.png (nuspec iconUrl)
.github/workflows/        build-/publish-auditgoggles.yml, build-/publish-metascanner.yml
```

Namespaces: `Formula81.XrmToolBox.Tools.<Tool>.*` for a plugin, `Formula81.XrmToolBox.Shared.<Project>.*` for shared code.
Output assemblies: `Formula81.XrmToolBox.Tools.AuditGoggles.dll` and `Formula81.XrmToolBox.Tools.MetaScanner.dll`.
Shared code is compiled into both plugins, so a change in `Shared/` affects both: build both.

## Build

Windows only, .NET Framework 4.8.1, MSBuild (Visual Studio 2022). No `dotnet build`, no test project.

```powershell
nuget restore AuditGoggles/AuditGoggles.csproj
msbuild AuditGoggles/AuditGoggles.csproj /p:Configuration=Release

nuget restore MetaScanner/MetaScanner.csproj
msbuild MetaScanner/MetaScanner.csproj /p:Configuration=Release
```

The post-build event copies `Formula81.*.dll` / `.config` (and `.pdb` in Debug) into `bin/<Config>/Plugins`.
To debug, point XrmToolBox at that output or copy the DLL into the XrmToolBox `Plugins` folder.

## Project file rules (important)

- `AuditGoggles.csproj` and `MetaScanner.csproj` are **old-style**: every new `.cs`, `.xaml` and image must be added explicitly
  (`<Compile>`, `<Page>` with `MSBuild:Compile`, `<Resource>`). XAML code-behind needs `<DependentUpon>`.
- Files in `Shared/*` must be registered in that project's `.projitems` using `$(MSBuildThisFileDirectory)`.
  Shared code is compiled into the plugin, so it can reference the same NuGet packages.
- Do not hand-edit versions in `AssemblyInfo.cs` or the nuspec `<version>`; CI overwrites them.
  Bump `major`/`minor` in the tool's `version.json` instead.

## Early-bound classes

- Always generate with **Delegate XrmContext** (`EarlyBound/Generator/XrmContext/XrmContext.exe`, run via `Run.ps1`).
  Do not use CrmSvcUtil, `pac modelbuilder` or the Early Bound Generator plugin.
- Connection and entity settings live in `XrmContext.exe.config`, which is **gitignored** because it holds
  credentials. Never commit it or print its contents.
- Generated files (`Shared/Xrm/Audit.cs`, `WebResource.cs`, `GlobalOptionSets.cs`, `XrmExtensions.cs`) are
  auto-generated: do not edit them by hand. Put additions in partial classes, as in
  `Shared/Xrm/ColumnNames/*.cs`, which expose column names via `GetColumnName<T>(x => x.Prop)`.
- Use `Audit.ColumnNames.*` / `WebResource.ColumnNames.*` instead of string literals in queries.

## Architecture and conventions

- **Entry point**: `AuditGogglesPlugin` (MEF) creates `AuditGogglesPluginControl`, which hosts the WPF
  `AuditGogglesView` in an `ElementHost`. The view creates the three view models and wires buttons to their commands.
- **All Dataverse access goes through `AuditGogglesPluginControl`** using XrmToolBox `WorkAsync(new WorkAsyncInfo { Work, PostWorkCallBack })`.
  Pattern: set `IsBusy = true`, do service calls in `Work`, call `runWorkerCompletedEventArgs.ThrowIfError()` in
  the callback, push results into the view model, show errors with `ShowErrorDialog`, and reset `IsBusy` in `finally`.
  Never call the service on the UI thread.
- View models take the plugin control in their constructor and call its `internal` methods; commands are
  `RelayCommand(Execute, CanExecute)` and `CanExecute` checks `!IsBusy`. `UpdateUI()` calls
  `CommandManager.InvalidateRequerySuggested()`.
- Bindable models derive from `ObservableObject` and use `SetValue(nameof(Prop), value, ref _field)`.
- Toolbar icons are Lucide icons stored as `Geometry` resources in each tool's `Resources/LucideIcons.xaml`
  (no PNGs). Add new ones from `lucide-static`, converting circle/rect to path data, and keep the license header.
- Both tools use the orange `#FF8000` as tile color and as `AccentBrush` in the view.
- Dialogs are WPF `Window`s owned by the plugin handle via `WindowInteropHelper` and centered with `WindowUtility.CalculateCenter`.
- Large `In` queries are chunked to 100 ids with `Queue.DequeueChunk(100)`.
- Entity audits load with paging (`PagingInfo` + paging cookie) for infinite append scrolling; results come back as `EntityAuditResult`.
- Limits: `AuditRecordsMax = 100` selected records (throws `AuditRecordsLimitException`). Only audit-enabled entities are listed.
  "Default" entity audits (created on / modified on for non-audited entities) are intentionally commented out for now.
- FetchXML Builder integration uses the XrmToolBox message bus (`IMessageBusHost`, source plugin name `"FetchXML Builder"`).
- Settings are stored per connection with `SettingsManager`, loaded on connection change and saved on close.
- Caches (`EntityIconCache`, `EntityNameCache`) are singletons whose `ServiceClient` is reset in `OnConnectionUpdated`.
- Meta Scanner specifics:
  - Uses a busy counter (`BeginBusy`/`EndBusy`) instead of disabling the view, so the table list stays usable.
  - Table details load with `RetrieveEntityRequest` (`EntityFilters.All`) and are cached per table. The first scan
    loads all tables with columns (`RetrieveAllEntitiesRequest`, `Entity | Attributes`) once and caches that too.
  - Every load captures `_loadGeneration` and drops its result when it changed (connection change or Refresh), and
    table details also check `_entityDetailRequestId` so only the last clicked table is shown.
  - Scan results are capped at `ScanHelper.ScanResultsMax` (5000).
- Style: `_camelCase` private fields, expression-bodied properties, `?.`/`??` null handling, sparse comments.
  Keep existing identifiers as they are even where misspelled (for example `EntityAuditCritieriaWindow`) unless asked to rename.

## CI / release

- `build-auditgoggles.yml`: on push/PR to `main` (and manual). Version = `major.minor.yyDDD.runNumber`,
  stamps AssemblyInfo + nuspec, builds Release, packs the nupkg, uploads artifacts. Manual run with
  `create_release = true` also creates a GitHub Release with the nupkg and a zipped DLL.
- `publish-auditgoggles.yml`: manual, takes a release tag and pushes the nupkg to nuget.org (environment `Nuget Publish`). Uses NuGet Trusted Publishing: `NuGet/login@v1`
  exchanges the GitHub OIDC token for a 1-hour API key, so no long-lived key is stored. Needs the secret
  `NUGET_USER` (nuget.org profile name) and a Trusted Publishing policy on nuget.org per workflow file.
- `build-metascanner.yml` / `publish-metascanner.yml`: the same for Meta Scanner, except the build only runs
  automatically when `MetaScanner/**` or `Shared/**` changes, and its releases are tagged `metascanner-<version>`
  (run numbers are per workflow, so plain version tags could collide with Audit Goggles releases).
- When shipping user-facing changes, add an entry to the tool's `CHANGELOG.md`. A `Shared/` change that is
  visible in both tools goes in both changelogs.

## Git workflow

Branch off a fresh `main` for each change: `feature/<name>` or `fix/<name>`. Ask for the name if unsure.
