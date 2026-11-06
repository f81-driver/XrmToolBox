# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

Formula81 XrmToolBox tools (GitHub: `f81-driver/XrmToolBox`, GPL-3.0). Three tools ship from this repo:

- **Audit Goggles**: explores Dataverse / Dynamics 365 audit history across up to 100 selected records, with
  filtering, column selection, color coding, and FetchXML Builder integration.
- **Meta Scanner**: a friendlier metadata browser. Table list with favorites, table details (columns, choices,
  relationships, keys, privileges, properties) and a scan that searches every table, column and choice value at once.
- **View Driver**: manages personal views (`userquery`). Table list as in Meta Scanner, the views of a table (personal and
  system, also of other users by impersonation), an editor for columns (drag and drop), filter, sort and FetchXML, a live
  data preview, clone and FetchXML Builder integration.

This folder is its **own git repo** (default branch `main`), separate from the sibling `coachandthegame` repo.

## Layout

```
Tools.sln                 Main solution: AuditGoggles + MetaScanner + ViewDriver + 5 shared projects
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
ViewDriver/               The personal view plugin, same structure and conventions as MetaScanner
  ViewDriverPluginControl.cs     All Dataverse calls (WorkAsync), impersonation, metadata cache, FetchXML Builder message bus
  ViewDriverSettings.cs          Per-connection settings (favorite and last selected table, table criteria and sort, system views, preview refresh/page size)
  Views/ViewDriverView.xaml      The whole UI: tables, views, editor tabs, data preview; drag and drop and grid sync in code-behind
  Views/CloneViewWindow.xaml     Name and owner of a cloned view
  ViewModels/                    EntityList / ViewList / ViewEditor / Preview view models
  Models/ViewDefinition.cs       FetchXML + LayoutXML of a view as XDocuments; every edit is made on the XML
  Models/                        ViewItem, ViewColumn, ViewCondition, ViewOrder, ConditionOperator, EntityDetail, ...
  Helpers/                       MetadataHelper, PreviewHelper (paged query, formatted values), CsvExportHelper
  Controls/InsertionAdorner.cs   Drop position line for dragged columns
  version.json, ViewDriver.nuspec, CHANGELOG.md   As for AuditGoggles
Shared/                   Shared projects (.shproj/.projitems), compiled INTO the plugin assembly
  Core/      ObservableObject, Queue/String extensions, EnumHelper
  Json/      JToken extensions
  Parts/     WPF building blocks: RelayCommand, converters, FilterInputControl, TimePicker, WindowUtility
  Xrm/       Early-bound entities (Audit, SavedQuery, SystemUser, UserQuery, WebResource, GlobalOptionSets, XrmContext), ColumnNames partials,
             metadata extensions, EntityIconCache, embedded entity icons
  XrmParts/  IIconifiable + converter
docs/images/              Screenshots used by the README / wiki, and MetaScanner.png / ViewDriver.png (nuspec iconUrl)
.github/workflows/        build-/publish-auditgoggles.yml, build-/publish-metascanner.yml, build-/publish-viewdriver.yml
```

Namespaces: `Formula81.XrmToolBox.Tools.<Tool>.*` for a plugin, `Formula81.XrmToolBox.Shared.<Project>.*` for shared code.
Output assemblies: `Formula81.XrmToolBox.Tools.AuditGoggles.dll`, `Formula81.XrmToolBox.Tools.MetaScanner.dll` and
`Formula81.XrmToolBox.Tools.ViewDriver.dll`. Shared code is compiled into every plugin, so a change in `Shared/` affects
all of them: build all (`msbuild Tools.sln`).

## Build

Windows only, .NET Framework 4.8.1, MSBuild (Visual Studio 2022). No `dotnet build`, no test project.

```powershell
nuget restore AuditGoggles/AuditGoggles.csproj
msbuild AuditGoggles/AuditGoggles.csproj /p:Configuration=Release

nuget restore MetaScanner/MetaScanner.csproj
msbuild MetaScanner/MetaScanner.csproj /p:Configuration=Release

nuget restore ViewDriver/ViewDriver.csproj
msbuild ViewDriver/ViewDriver.csproj /p:Configuration=Release
```

The post-build event copies `Formula81.*.dll` / `.config` (and `.pdb` in Debug) into `bin/<Config>/Plugins`.
To debug, point XrmToolBox at that output or copy the DLL into the XrmToolBox `Plugins` folder.

## Project file rules (important)

- `AuditGoggles.csproj`, `MetaScanner.csproj` and `ViewDriver.csproj` are **old-style**: every new `.cs`, `.xaml` and image must be added explicitly
  (`<Compile>`, `<Page>` with `MSBuild:Compile`, `<Resource>`). XAML code-behind needs `<DependentUpon>`.
- Files in `Shared/*` must be registered in that project's `.projitems` using `$(MSBuildThisFileDirectory)`.
  Shared code is compiled into the plugin, so it can reference the same NuGet packages.
- Do not hand-edit versions in `AssemblyInfo.cs` or the nuspec `<version>`; CI overwrites them.
  Bump `major`/`minor` in the tool's `version.json` instead.

## Early-bound classes

- Always generate with **Delegate XrmContext** (`Generator/XrmContext/XrmContext.exe` in the separate [XrmToolBox.Xrm](https://github.com/f81-driver/XrmToolBox.Xrm) repo, cloned next to this one as `../XrmToolBox.Xrm`; run via `Generate.ps1`, which asks for the environment URL and signs in interactively).
  Do not use CrmSvcUtil, `pac modelbuilder` or the Early Bound Generator plugin.
- Entity, output and namespace settings live in the committed `XrmContext.exe.config`. It must never hold a URL,
  credentials or secrets: authentication is interactive OAuth only.
- Generated files (`Shared/Xrm/Audit.cs`, `SavedQuery.cs`, `SystemUser.cs`, `UserQuery.cs`, `WebResource.cs`, `GlobalOptionSets.cs`,
  `XrmContext.cs`, `XrmExtensions.cs`) are
  auto-generated: do not edit them by hand. Put additions in partial classes, as in
  `Shared/Xrm/ColumnNames/*.cs`, which expose column names via `GetColumnName<T>(x => x.Prop)`.
- Use `Audit.ColumnNames.*` / `WebResource.ColumnNames.*` (and so on) instead of string literals in queries.
- A newly generated entity must be added to `Shared/Xrm/Xrm.projitems`, and the generated relationship properties of the
  existing entities may refer to it (for example `SystemUser`), so regenerate and register them together.
- After every generation, before building (these are the only allowed hand edits to the generated files):
  - Comment out `[assembly: ProxyTypesAssembly()]` (in `GlobalOptionSets.cs`). XrmToolBox warns that a tool with it does
    unsupported things, and it can make the SDK return our early-bound types to other tools. The tools do not need it.
  - Remove everything custom: columns, relationships and option set enums whose table or field name carries a publisher
    prefix (`<prefix>_`, including Microsoft solution prefixes such as `msdyn_`), from the entities and from
    `GlobalOptionSets.cs`. Ideally generate from a vanilla org so there is nothing to strip.

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
- View Driver specifics:
  - Busy counter as in Meta Scanner. The data preview runs `WorkAsync` without a `Message`, so no overlay blocks the tool,
    and shows its errors inline; `_previewRequestId` drops stale results, `_viewsRequestId` stale view lists.
  - Other users' personal views are read and saved through a `CrmServiceClient.Clone()` with `CallerId` set (cached per user,
    disposed on connection change), never by setting `CallerId` on the shared `ConnectionDetail.ServiceClient`.
  - `ViewDefinition` keeps FetchXML and LayoutXML as XDocuments; conditions and orders are edited on their own XElements, so
    anything the editor does not show (nested filters, link-entity filters, unknown attributes) survives a save.
  - Only the top level filter of the view table is edited in the UI; the rest is edited on the FetchXML tab or in FetchXML Builder.
  - Applying FetchXML (typed or returned by FetchXML Builder) drops layout cells of columns that are no longer queried and adds
    cells for newly queried ones (`ViewDefinition.ReplaceFetch`).
  - Columns of related tables (many-to-one) are added as an outer `link-entity` with an `a_<lookup>` alias; the cell name is
    `alias.column`. An outer link without columns is removed again.
  - The preview grid columns follow the view columns; reordering or resizing them in the grid writes back to the layout.
  - System views are never saved; they can be edited for the preview and cloned into a personal view.
  - Unsaved changes are confirmed (`ConfirmDiscardChanges`) before switching table, owner or view, and on close.
  - Sharing (`ShareViewAsync`) runs as the owner of the view (impersonated when it is another user): it reads the shares with
    `RetrieveSharedPrincipalsAndAccess` and applies only the differences with `GrantAccess`, `ModifyAccess` and `RevokeAccess`,
    keeping rights the dialog does not show (such as append).
  - New views start from a `ViewTemplate` (`ViewDefinition.CreateFromTemplate`). Templates only hold the primary name
    (`{primaryname}` placeholder) and the system columns in `TemplateColumns`, so they apply to any table; columns a table lacks
    are skipped. User templates are stored once for all connections (`SettingsManager` name `Templates`), not per connection.
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
- `build-viewdriver.yml` / `publish-viewdriver.yml`: the same for View Driver (`ViewDriver/**` or `Shared/**`), tagged
  `viewdriver-<version>`. A Trusted Publishing policy on nuget.org is needed for `publish-viewdriver.yml` as well.
- When shipping user-facing changes, add an entry to the tool's `CHANGELOG.md`. A `Shared/` change that is
  visible in both tools goes in both changelogs.

## Git workflow

Branch off a fresh `main` for each change: `feature/<name>` or `fix/<name>`. Ask for the name if unsure.
