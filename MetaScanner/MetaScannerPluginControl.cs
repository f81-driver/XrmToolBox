using Formula81.XrmToolBox.Shared.Core.Extensions.ComponentModel;
using Formula81.XrmToolBox.Shared.Xrm.Caches;
using Formula81.XrmToolBox.Tools.MetaScanner.Helpers;
using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using Formula81.XrmToolBox.Tools.MetaScanner.Views;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Markup;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Formula81.XrmToolBox.Tools.MetaScanner
{
    public partial class MetaScannerPluginControl : PluginControlBase, IGitHubPlugin, IPayPalPlugin
    {
        private const string ServiceClientNotAvailableMessage = "Service Client is not available";

        private const string GitHubRepositoryName = "XrmToolBox";
        private const string GitHubUserName = "f81-driver";
        private const string MakerPortalUrl = "https://make.powerapps.com";

        private int _busyCount;
        public bool IsBusy => _busyCount > 0;

        private readonly MetaScannerView _metaScannerView;

        private readonly Dictionary<string, EntityDetail> _entityDetailCache;
        private List<EntityMetadata> _scanEntityMetadatas;
        private int _entityDetailRequestId;
        // Bumped on every connection change or reload, so results of loads started before are dropped
        private int _loadGeneration;

        public MetaScannerSettings Settings { get; private set; }

        public string RepositoryName => GitHubRepositoryName;
        public string UserName => GitHubUserName;
        public string DonationDescription => MetaScannerPlugin.PayPalDonationDescription;
        public string EmailAccount => MetaScannerPlugin.PayPalEmailAccount;

        private CrmServiceClient ServiceClient { get => (ConnectionDetail?.ServiceClient?.IsReady ?? false ? ConnectionDetail.ServiceClient : null) ?? throw new InvalidOperationException(ServiceClientNotAvailableMessage); }

        public MetaScannerPluginControl()
        {
            try
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            }
            catch (ArgumentException) { }

            Settings = new MetaScannerSettings();
            _entityDetailCache = new Dictionary<string, EntityDetail>(StringComparer.OrdinalIgnoreCase);

            _metaScannerView = new MetaScannerView(this);

            InitializeComponent();

            var metaScannerHost = new ElementHost();
            SuspendLayout();

            metaScannerHost.Dock = DockStyle.Fill;
            metaScannerHost.Name = "MetaScannerHost";
            metaScannerHost.TabIndex = 0;
            metaScannerHost.Child = _metaScannerView;

            Controls.Add(metaScannerHost);
            Name = "MetaScannerControl";
            ResumeLayout(false);
        }

        internal void LoadEntitiesAsync()
        {
            const string message = "Loading Tables";
            var selectedLogicalName = _metaScannerView.EntityListViewModel.SelectedEntity?.LogicalName;
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var response = (RetrieveAllEntitiesResponse)ServiceClient.Execute(new RetrieveAllEntitiesRequest
                    {
                        EntityFilters = EntityFilters.Entity,
                        RetrieveAsIfPublished = true
                    });
                    var entityMetadatas = response.EntityMetadata;
                    var entityIconDatas = EntityIconCache.Instance.GetMany(entityMetadatas);
                    doWorkEventArgs.Result = entityMetadatas
                        .Select(em => MetadataHelper.CreateEntity(em,
                            entityIconDatas.TryGetValue(em.LogicalName, out var iconData) ? iconData : null,
                            Settings.IsFavoriteEntity(em.LogicalName)))
                        .ToList();
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        runWorkerCompletedEventArgs.ThrowIfError();
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        var entityListViewModel = _metaScannerView.EntityListViewModel;
                        entityListViewModel.SetEntities(runWorkerCompletedEventArgs.Result as IEnumerable<MetaEntity>);
                        if (!string.IsNullOrEmpty(selectedLogicalName))
                        {
                            entityListViewModel.Select(entityListViewModel.Find(selectedLogicalName), null);
                        }
                    }
                    catch (Exception exception)
                    {
                        ShowErrorDialog(exception, message);
                    }
                    finally
                    {
                        EndBusy();
                    }
                }
            });
        }

        internal void ShowEntityDetail(MetaEntity metaEntity, string attributeLogicalName)
        {
            const string message = "Loading Table Metadata";
            _metaScannerView.ScanViewModel.IsActive = false;
            var entityDetailViewModel = _metaScannerView.EntityDetailViewModel;
            var requestId = ++_entityDetailRequestId;
            var loadGeneration = _loadGeneration;
            if (_entityDetailCache.TryGetValue(metaEntity.LogicalName, out var cachedEntityDetail))
            {
                entityDetailViewModel.SetEntityDetail(cachedEntityDetail, attributeLogicalName);
                // A table that was still loading is superseded by this one
                entityDetailViewModel.EndLoad();
                return;
            }

            BeginBusy();
            entityDetailViewModel.BeginLoad();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var response = (RetrieveEntityResponse)ServiceClient.Execute(new RetrieveEntityRequest
                    {
                        LogicalName = metaEntity.LogicalName,
                        EntityFilters = EntityFilters.All,
                        RetrieveAsIfPublished = true
                    });
                    doWorkEventArgs.Result = MetadataHelper.CreateEntityDetail(metaEntity, response.EntityMetadata);
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        runWorkerCompletedEventArgs.ThrowIfError();
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        var entityDetail = runWorkerCompletedEventArgs.Result as EntityDetail;
                        _entityDetailCache[metaEntity.LogicalName] = entityDetail;
                        // The user may have picked another table while this one was loading
                        if (requestId == _entityDetailRequestId)
                        {
                            entityDetailViewModel.SetEntityDetail(entityDetail, attributeLogicalName);
                        }
                    }
                    catch (Exception exception)
                    {
                        ShowErrorDialog(exception, message);
                    }
                    finally
                    {
                        if (requestId == _entityDetailRequestId)
                        {
                            entityDetailViewModel.EndLoad();
                        }
                        EndBusy();
                    }
                }
            });
        }

        internal void ScanAsync(string text)
        {
            const string message = "Scanning Metadata";
            var entities = _metaScannerView.EntityListViewModel.Entities.ToDictionary(me => me.LogicalName, StringComparer.OrdinalIgnoreCase);
            var scanEntityMetadatas = _scanEntityMetadatas;
            var limitExceeded = false;
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                // Columns of all tables are only retrieved on the first scan, which takes a while on large environments
                Message = scanEntityMetadatas == null ? "Scanning Metadata, the first scan loads all columns and may take a minute" : message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    if (scanEntityMetadatas == null)
                    {
                        var response = (RetrieveAllEntitiesResponse)ServiceClient.Execute(new RetrieveAllEntitiesRequest
                        {
                            EntityFilters = EntityFilters.Entity | EntityFilters.Attributes,
                            RetrieveAsIfPublished = true
                        });
                        scanEntityMetadatas = response.EntityMetadata.ToList();
                    }
                    doWorkEventArgs.Result = ScanHelper.Scan(scanEntityMetadatas, entities, text, out limitExceeded);
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        runWorkerCompletedEventArgs.ThrowIfError();
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        _scanEntityMetadatas = scanEntityMetadatas;
                        _metaScannerView.ScanViewModel.SetResults(text, runWorkerCompletedEventArgs.Result as IList<ScanResult>, limitExceeded);
                    }
                    catch (Exception exception)
                    {
                        ShowErrorDialog(exception, message);
                    }
                    finally
                    {
                        EndBusy();
                    }
                }
            });
        }

        internal bool HasScanResults()
        {
            // CanExecute is queried while the view is still being constructed
            return _metaScannerView?.ScanViewModel.HasResults ?? false;
        }

        internal void ShowScanResults()
        {
            _metaScannerView.ScanViewModel.IsActive = true;
        }

        internal void GoToScanResult(ScanResult scanResult)
        {
            _metaScannerView.EntityListViewModel.Select(scanResult.Entity, scanResult.AttributeLogicalName);
        }

        internal void GoToEntity(string logicalName)
        {
            var entityListViewModel = _metaScannerView.EntityListViewModel;
            var metaEntity = entityListViewModel.Find(logicalName);
            if (metaEntity == null)
            {
                throw new InvalidOperationException($"Table '{logicalName}' was not found");
            }
            entityListViewModel.Select(metaEntity, null);
        }

        internal bool CanOpenInMakerPortal(MetaEntity metaEntity)
        {
            return metaEntity?.MetadataId != null
                && !string.IsNullOrEmpty(ConnectionDetail?.EnvironmentId);
        }

        internal void OpenInMakerPortal(MetaEntity metaEntity)
        {
            if (CanOpenInMakerPortal(metaEntity))
            {
                Process.Start(new ProcessStartInfo($"{MakerPortalUrl}/environments/{ConnectionDetail.EnvironmentId}/entities/{metaEntity.MetadataId}") { UseShellExecute = true });
            }
        }

        internal bool CanOpenRecords()
        {
            return !string.IsNullOrEmpty(ConnectionDetail?.WebApplicationUrl);
        }

        internal void OpenRecords(MetaEntity metaEntity)
        {
            if (metaEntity != null
                && CanOpenRecords())
            {
                var url = $"{ConnectionDetail.WebApplicationUrl.TrimEnd('/')}/main.aspx?pagetype=entitylist&etn={metaEntity.LogicalName}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        internal void Reload()
        {
            ResetCaches();
            _metaScannerView.ScanViewModel.Clear();
            LoadEntitiesAsync();
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            base.ClosingPlugin(info);
            SaveSettings();
        }

        protected override void OnConnectionUpdated(ConnectionUpdatedEventArgs e)
        {
            base.OnConnectionUpdated(e);

            ResetCaches();
            _metaScannerView.EntityListViewModel.Clear();
            _metaScannerView.EntityDetailViewModel.Clear();
            _metaScannerView.ScanViewModel.Clear();

            EntityIconCache.Instance.Clear();
            EntityIconCache.Instance.ServiceClient = e.ConnectionDetail.ServiceClient;

            LoadSettings();
            _metaScannerView.EntityDetailViewModel.ShowHelperColumns = Settings.ShowHelperColumns;
            _metaScannerView.EntityListViewModel.NotifySortByChanged();
            LoadEntitiesAsync();
        }

        private void ResetCaches()
        {
            _loadGeneration++;
            _entityDetailCache.Clear();
            _scanEntityMetadatas = null;
        }

        private void BeginBusy()
        {
            _busyCount++;
            UpdateUI();
        }

        private void EndBusy()
        {
            _busyCount = Math.Max(0, _busyCount - 1);
            UpdateUI();
        }

        private void UpdateUI()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        internal void LoadSettings()
        {
            if (ConnectionDetail?.ConnectionId != null)
            {
                Settings = SettingsManager.Instance.TryLoad(GetType(), out MetaScannerSettings settings, ConnectionDetail.ConnectionId.ToString()) ? settings : new MetaScannerSettings();
            }
        }

        internal void SaveSettings()
        {
            if (ConnectionDetail?.ConnectionId != null)
            {
                SettingsManager.Instance.Save(GetType(), Settings, ConnectionDetail.ConnectionId.ToString());
            }
        }
    }
}
