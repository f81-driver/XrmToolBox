using Formula81.XrmToolBox.Shared.Core.Extensions;
using Formula81.XrmToolBox.Shared.Core.Extensions.ComponentModel;
using Formula81.XrmToolBox.Shared.Parts.Utilities;
using Formula81.XrmToolBox.Shared.Xrm;
using Formula81.XrmToolBox.Shared.Xrm.Caches;
using Formula81.XrmToolBox.Tools.AuditGoggles.Caches;
using Formula81.XrmToolBox.Tools.AuditGoggles.Components;
using Formula81.XrmToolBox.Tools.AuditGoggles.Exceptions;
using Formula81.XrmToolBox.Tools.AuditGoggles.Helpers;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Formula81.XrmToolBox.Tools.AuditGoggles.Views;
using Formula81.XrmToolBox.Tools.AuditGoggles.Windows;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using F81Rect = Formula81.XrmToolBox.Shared.Parts.Components.Rect;

namespace Formula81.XrmToolBox.Tools.AuditGoggles
{
    public partial class AuditGogglesPluginControl : PluginControlBase, IMessageBusHost, IGitHubPlugin, IPayPalPlugin
    {
        private const string ServiceClientNotAvailableMessage = "Service Client is not available";

        private const string GitHubRepositoryName = "XrmToolBox";
        private const string GitHubUserName = "f81-driver";
        private const string FetchXmlBuilderSourcePluginName = "FetchXML Builder";

        public const int AuditRecordsMax = 100;

        public event EventHandler<MessageBusEventArgs> OnOutgoingMessage;

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { _isBusy = value; UpdateUI(); } }

        private readonly AuditGogglesView _auditGogglesView;

        public AuditGogglesSettings Settings { get; private set; }

        public string RepositoryName => GitHubRepositoryName;
        public string UserName => GitHubUserName;
        public string DonationDescription => AuditGogglesPlugin.PayPalDonationDescription;
        public string EmailAccount => AuditGogglesPlugin.PayPalEmailAccount;

        private CrmServiceClient ServiceClient { get => (ConnectionDetail?.ServiceClient?.IsReady ?? false ? ConnectionDetail.ServiceClient : null) ?? throw new InvalidOperationException(ServiceClientNotAvailableMessage); }

        public AuditGogglesPluginControl()
        {
            try
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            }
            catch (ArgumentException) { }

            Settings = new AuditGogglesSettings();

            _auditGogglesView = new AuditGogglesView(this);

            InitializeComponent();

            var auditGogglesHost = new ElementHost();
            SuspendLayout();

            auditGogglesHost.Dock = DockStyle.Fill;
            auditGogglesHost.Name = "AuditGogglesHost";
            auditGogglesHost.TabIndex = 0;
            auditGogglesHost.Child = _auditGogglesView;

            Controls.Add(auditGogglesHost);
            Name = "AuditGogglesControl";
            ResumeLayout(false);
        }

        internal void CallFxbPlugin()
        {
            OnOutgoingMessage(this, new MessageBusEventArgs(FetchXmlBuilderSourcePluginName)
            {
                TargetArgument = null
            });
        }

        public void OnIncomingMessage(MessageBusEventArgs message)
        {
            if (message.SourcePlugin.Equals(FetchXmlBuilderSourcePluginName)
                && message.TargetArgument is string fetchXml
                && !string.IsNullOrEmpty(fetchXml))
            {
                LoadAuditRecordsAsync((service) =>
                {
                    return ServiceClient.RetrieveMultiple(new FetchExpression(fetchXml)).Entities
                        .Where(e => !_auditGogglesView.AuditRecordViewModel.ContainsId(e.Id))
                        .Select(e => e.ToEntityReference());
                });
            }
        }

        internal bool HasAuditRecords()
        {
            return _auditGogglesView?.AuditRecordViewModel?.AuditRecords?.Any() ?? false;
        }

        internal IEnumerable<EntityReference> ShowAuditRecordInputDialog(string logicalName = null)
        {
            var auditEntities = _auditGogglesView.AuditEntityViewModel.AuditEntities;
            var window = new AuditRecordInputWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                AuditEntities = auditEntities,
                AuditEntity = string.IsNullOrEmpty(logicalName) ? null : auditEntities.FirstOrDefault(ae => ae.Name.Equals(logicalName))
            };
            var center = WindowUtility.CalculateCenter(window.Width, window.Height, GetWindowBounds());
            window.Left = center.X;
            window.Top = center.Y;
            var helper = new WindowInteropHelper(window)
            {
                Owner = Handle
            };

            var entityRefs = (window.ShowDialog() ?? false)
                && window.AuditEntity != null
                && window.Ids.Any() ? window.Ids.Select(i => new EntityReference(window.AuditEntity.Name, i))
                    : Enumerable.Empty<EntityReference>();

            return entityRefs.ToList();
        }

        internal IEnumerable<ConditionExpression> ShowEntityAuditFilterDialog(IEnumerable<ConditionExpression> criteriaConditions)
        {
            var window = new EntityAuditCritieriaWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Criteria = criteriaConditions
            };
            var center = WindowUtility.CalculateCenter(window.Width, window.Height, GetWindowBounds());
            window.Left = center.X;
            window.Top = center.Y;
            _ = new WindowInteropHelper(window)
            {
                Owner = Handle
            };

            return window.ShowDialog() ?? false ? window.Criteria.ToList() : criteriaConditions;
        }

        internal IDictionary<string, ColumnSet> ShowEntityAuditColumnsDialog(IDictionary<string, ColumnSet> columns)
        {
            var window = new EntityAuditColumnsWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var center = WindowUtility.CalculateCenter(window.Width, window.Height, GetWindowBounds());
            window.Left = center.X;
            window.Top = center.Y;
            _ = new WindowInteropHelper(window)
            {
                Owner = Handle
            };
            var entityMetadataList = new List<EntityMetadata>();
            foreach (var entityLogicalName in _auditGogglesView.AuditRecordViewModel.AuditRecords.Select(ar => ar.EntityLogicalName).Distinct())
            {
                entityMetadataList.Add(ServiceClient.GetEntityMetadata(entityLogicalName, EntityFilters.Attributes));
            }
            window.SetSource(entityMetadataList, columns);

            return window.ShowDialog() ?? false ? window.Get() : columns;
        }

        internal void LoadAuditEntitiesAsync()
        {
            const string message = "Loading Audit Entities";
            IsBusy = true;
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var entityMetadatas = ServiceClient.GetAllEntityMetadata()
                        //.Where(em => em.IsValidForAdvancedFind ?? false);
                        .Where(em => em.IsAuditEnabled?.Value ?? false);
                    var entityIconDatas = EntityIconCache.Instance.GetMany(entityMetadatas);
                    doWorkEventArgs.Result = entityMetadatas
                        .Select(em => new AuditEntity(em.ObjectTypeCode,
                            em.LogicalName,
                            em.DisplayName?.UserLocalizedLabel?.Label ?? em.LogicalName,
                            em.IsAuditEnabled?.Value ?? false,
                            entityIconDatas[em.LogicalName],
                            Settings.IsFavoriteAuditEntity(em.LogicalName)))
                        .OrderBy(ae => ae.DisplayName)
                        .ToList();
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        runWorkerCompletedEventArgs.ThrowIfError();
                        _auditGogglesView.AuditEntityViewModel.SetAuditEntities(runWorkerCompletedEventArgs.Result as IEnumerable<AuditEntity>);
                    }
                    catch (Exception exception)
                    {
                        ShowErrorDialog(exception, message);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                },
            });
        }

        internal void LoadAuditRecordsAsync(Func<CrmServiceClient, IEnumerable<EntityReference>> entityRefFunc)
        {
            const string message = "Loading Audit Records";
            IsBusy = true;
            var auditRecordCount = _auditGogglesView.AuditRecordViewModel.AuditRecords.Count();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var entityRefs = entityRefFunc(ServiceClient);
                    if (entityRefs.Count() + auditRecordCount > AuditRecordsMax)
                    {
                        throw new AuditRecordsLimitException(AuditRecordsMax);
                    }
                    var auditRecords = WorkAuditRecords(entityRefs);
                    doWorkEventArgs.Result = auditRecords;
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        runWorkerCompletedEventArgs.ThrowIfError();
                        var results = runWorkerCompletedEventArgs.Result as IEnumerable<AuditRecord>;
                        foreach (var r in results)
                        {
                            _auditGogglesView.AuditRecordViewModel.Add(r);
                        }
                    }
                    catch (Exception exception)
                    {
                        ShowErrorDialog(exception, message);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }
            });
        }

        internal void LoadEntityAuditsAsync(IEnumerable<ConditionExpression> criteriaConditions, IDictionary<string, ColumnSet> columns, PagingInfo pageInfo, OrderType orderType)
        {
            const string message = "Loading Entity Audits";
            var auditRecords = _auditGogglesView.AuditRecordViewModel.AuditRecords;
            if (auditRecords.Any())
            {
                IsBusy = true;
                WorkAsync(new WorkAsyncInfo
                {
                    Message = message,
                    Work = (backgroundWorker, doWorkEventArgs) =>
                    {
                        var colorCombos = auditRecords.ToDictionary(ar => ar.Id, ar => ar.ColorCombination);
                        var auditList = new List<Audit>();

                        var entityAuditConditionList = new List<EntityAuditConditionPair>();
                        var data = new Dictionary<string, Dictionary<Guid, Entity>>();
                        var entityMetadatas = new Dictionary<string, EntityMetadata>();
                        var auditRecordGroups = auditRecords.GroupBy(er => er.EntityLogicalName, er => er.Id);
                        foreach (var auditRecordGroup in auditRecordGroups)
                        {
                            var entityMetadata = ServiceClient.GetEntityMetadata(auditRecordGroup.Key, EntityFilters.Attributes);
                            entityMetadatas[auditRecordGroup.Key] = entityMetadata;

                            var entities = RetrieveEntities(entityMetadata, auditRecordGroup.Select(g => g), EntityAuditHelper.GetEntityAuditEntityColumns(entityMetadata));
                            data[auditRecordGroup.Key] = entities.ToDictionary(e => e.Id, e => e);
                            EntityNameCache.Instance.Upsert(auditRecordGroup.Key, entities.ToDictionary(e => e.Id, e => e.GetAttributeValue<string>(entityMetadata.PrimaryNameAttribute)));

                            if (entityMetadata?.IsAuditEnabled?.Value ?? false)
                            {
                                var attributes = entityMetadata.Attributes.Where(am => am.ColumnNumber.HasValue)
                                    .ToDictionary(am => am.LogicalName, am => am.ColumnNumber.Value);
                                var columnSet = (columns?.TryGetValue(entityMetadata.LogicalName, out var cs) ?? false) ? cs : null;
                                var attributeMasks = columnSet?.Columns?.Select(c => attributes.TryGetValue(c, out var mask) ? (int?)mask : null)
                                    .Where(m => m.HasValue)
                                    .Select(m => m.Value) ?? Enumerable.Empty<int>();
                                entityAuditConditionList.Add(new EntityAuditConditionPair(auditRecordGroup, attributeMasks));
                            }
                        }

                        auditList.AddRange(RetrieveAudits(entityAuditConditionList, criteriaConditions, orderType, pageInfo,
                            out var pagingCookie, out var moreRecords, out var totalRecordCount, out var totalRecordCountLimitExceeded));
                        var entityAuditHelper = new EntityAuditHelper(ServiceClient);
                        var entityAudits = auditList.Select(a => entityAuditHelper.ParseAudit(a,
                                    data[a.ObjectId.LogicalName][a.ObjectId.Id],
                                    entityMetadatas[a.ObjectId.LogicalName],
                                    (columns?.TryGetValue(a.ObjectId.LogicalName, out var columnSet) ?? false) ? columnSet : null,
                                    colorCombos[a.ObjectId.Id]));
                        /*var defaultEntityAudits = auditRecordGroups.Where(g => !(entityMetadatas[g.Key]?.IsAuditEnabled?.Value ?? false))
                                .SelectMany(g => g.SelectMany(i => entityAuditHelper.GetDefaultEntityAudits(data[g.Key][i], entityMetadatas[g.Key], colorCombos[i])
                                    .Where(ea => EntityAuditHelper.CheckChangedDate(ea, criteriaConditions))));*/

                        doWorkEventArgs.Result = new EntityAuditResult(entityAudits.Where(ea => ea != null)
                            /*.Concat(defaultEntityAudits)*/
                            .ToList(), pagingCookie, moreRecords, totalRecordCount, totalRecordCountLimitExceeded);
                    },
                    PostWorkCallBack = runWorkerCompletedEventArgs =>
                    {
                        try
                        {
                            runWorkerCompletedEventArgs.ThrowIfError();
                            var result = runWorkerCompletedEventArgs.Result as EntityAuditResult;
                            _auditGogglesView.EntityAuditViewModel.ApplyEntityAuditResult(result);
                        }
                        catch (Exception exception)
                        {
                            ShowErrorDialog(exception, message);
                        }
                        finally
                        {
                            IsBusy = false;
                        }
                    },
                });
            }
            else
            {
                _auditGogglesView.EntityAuditViewModel.ApplyEntityAuditResult(EntityAuditResult.Empty);
            }
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            base.ClosingPlugin(info);
            SaveSettings();
        }

        protected override void OnConnectionUpdated(ConnectionUpdatedEventArgs e)
        {
            base.OnConnectionUpdated(e);

            _auditGogglesView.AuditRecordViewModel.Clear();
            _auditGogglesView.EntityAuditViewModel.Clear();

            EntityIconCache.Instance.ServiceClient = e.ConnectionDetail.ServiceClient;
            EntityNameCache.Instance.ServiceClient = e.ConnectionDetail.ServiceClient;

            LoadSettings();
            LoadAuditEntitiesAsync();
        }

        private void UpdateUI()
        {
            _auditGogglesView.IsEnabled = !IsBusy;
            CommandManager.InvalidateRequerySuggested();
        }

        private F81Rect GetWindowBounds()
        {
            var rectangle = new F81Rect();
            return WindowUtility.GetWindowRect(Handle, ref rectangle) ? rectangle : default;
        }

        private IEnumerable<AuditRecord> WorkAuditRecords(IEnumerable<EntityReference> entityRefs)
        {
            var auditRecordList = new List<AuditRecord>();
            foreach (var entityRefGroup in entityRefs.GroupBy(er => er.LogicalName))
            {
                var logicalName = entityRefGroup.Key;
                var entityMetadata = ServiceClient.GetEntityMetadata(logicalName);
                if (entityMetadata != null)
                {
                    var entityNames = EntityNameCache.Instance.GetMany(logicalName, entityRefGroup.Select(er => er.Id));
                    var entityIconData = EntityIconCache.Instance.Get(entityMetadata);
                    var entityDisplayName = entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.LogicalName;
                    foreach (var entityRef in entityRefGroup
                        .Where(er => entityNames.ContainsKey(er.Id)))
                    {
                        var id = entityRef.Id;
                        auditRecordList.Add(new AuditRecord(entityMetadata.ObjectTypeCode,
                            id,
                            (entityNames.TryGetValue(id, out var name) ? name?.NullifyEmptyString() : null) ?? id.ToString(),
                            logicalName,
                            entityDisplayName,
                            entityIconData));
                    }
                }
            }
            return auditRecordList;
        }

        private IEnumerable<Audit> RetrieveAudits(IEnumerable<EntityAuditConditionPair> entityAuditConditionPairs, IEnumerable<ConditionExpression> criteriaConditions, OrderType orderType, PagingInfo pageInfo,
            out string pagingCookie, out bool moreRecords, out int totalRecordCount, out bool totalRecordCountLimitExceeded)
        {
            if (entityAuditConditionPairs?.Any() ?? false)
            {
                pageInfo.ReturnTotalRecordCount = true;
                var query = new QueryExpression(Audit.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(Audit.ColumnNames.AuditId,
                        Audit.ColumnNames.Action,
                        Audit.ColumnNames.AttributeMask,
                        Audit.ColumnNames.ChangeData,
                        Audit.ColumnNames.CreatedOn,
                        Audit.ColumnNames.ObjectId,
                        Audit.ColumnNames.ObjectTypeCode,
                        Audit.ColumnNames.Operation,
                        Audit.ColumnNames.UserId),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression(Audit.ColumnNames.Operation, ConditionOperator.In, EntityAuditHelper.SupportedAuditOperationValues)
                        },
                    },
                    Orders = { new OrderExpression(Audit.ColumnNames.CreatedOn, orderType) },
                    PageInfo = pageInfo
                };
                var objectFilter = query.Criteria.AddFilter(LogicalOperator.Or);
                foreach (var entityAuditConditionPair in entityAuditConditionPairs)
                {
                    var fitler = objectFilter.AddFilter(LogicalOperator.And);
                    fitler.AddCondition(new ConditionExpression(Audit.ColumnNames.ObjectId, ConditionOperator.In, entityAuditConditionPair.ObjectIds.ToArray()));
                    foreach (var attributeMask in entityAuditConditionPair.AttributeMasks)
                    {
                        fitler.AddCondition(new ConditionExpression(Audit.ColumnNames.AttributeMask, ConditionOperator.Like, attributeMask));
                    }
                }

                if (criteriaConditions?.Any() ?? false)
                {
                    query.Criteria.Conditions.AddRange(criteriaConditions);
                }

                var results = Service.RetrieveMultiple(query);
                pagingCookie = results.PagingCookie;
                moreRecords = results.MoreRecords;
                totalRecordCount = results.TotalRecordCount;
                totalRecordCountLimitExceeded = results.TotalRecordCountLimitExceeded;
                return results.Entities
                    .Select(e => e.ToEntity<Audit>())
                    .ToList();
            }
            pagingCookie = null;
            moreRecords = false;
            totalRecordCount = 0;
            totalRecordCountLimitExceeded = false;
            return Enumerable.Empty<Audit>();
        }

        private IEnumerable<Entity> RetrieveEntities(EntityMetadata entityMetadata, IEnumerable<Guid> ids, params string[] columns)
        {
            var entityList = new List<Entity>();
            if (ids?.Any() ?? false)
            {
                var idQueue = new Queue<Guid>(ids);
                while (idQueue.Count > 0)
                {
                    var query = new QueryExpression(entityMetadata.LogicalName)
                    {
                        ColumnSet = new ColumnSet(columns),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression(entityMetadata.PrimaryIdAttribute, ConditionOperator.In, idQueue.DequeueChunk(100).ToArray())
                            }
                        }
                    };
                    var entities = Service.RetrieveMultiple(query).Entities;
                    entityList.AddRange(entities);
                }
            }
            return entityList;
        }

        internal void LoadSettings()
        {
            if (ConnectionDetail?.ConnectionId != null)
            {
                Settings = SettingsManager.Instance.TryLoad(GetType(), out AuditGogglesSettings settings, ConnectionDetail.ConnectionId.ToString()) ? settings : new AuditGogglesSettings();
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