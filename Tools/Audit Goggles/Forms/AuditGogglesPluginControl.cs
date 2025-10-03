using Formula81.XrmToolBox.Libraries.Core.Extensions;
using Formula81.XrmToolBox.Libraries.Core.Extensions.ComponentModel;
using Formula81.XrmToolBox.Libraries.Parts.Utilities;
using Formula81.XrmToolBox.Libraries.Xrm;
using Formula81.XrmToolBox.Libraries.Xrm.Caches;
using Formula81.XrmToolBox.Tools.AuditGoggles.Caches;
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
using F81Rect = Formula81.XrmToolBox.Libraries.Parts.Components.Rect;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Forms
{
    public partial class AuditGogglesPluginControl : PluginControlBase, IPayPalPlugin, IMessageBusHost
    {
        private const string FetchXmlBuilderSourcePluginName = "FetchXML Builder";

        public event EventHandler<MessageBusEventArgs> OnOutgoingMessage;

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { _isBusy = value; UpdateUI(); } }

        private readonly AuditGogglesView _auditGogglesView;

        public AuditGogglesSettings Settings { get; private set; }
        public CrmServiceClient ServiceClient { get => ConnectionDetail?.ServiceClient; }

        public string DonationDescription => "";
        public string EmailAccount => "";

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
                LoadAuditRecordsFetchXmlAsync(fetchXml);
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

        internal void LoadAuditEntitiesAsync()
        {
            const string message = "Loading Audit Entities";
            if (!(ServiceClient?.IsReady ?? false))
            {
                throw new InvalidOperationException("No connection");
            }
            IsBusy = true;
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var entityMetadatas = ServiceClient.GetAllEntityMetadata()
                        .Where(em => em.IsValidForAdvancedFind ?? false);
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

        internal void LoadAuditRecordsAsync(IEnumerable<EntityReference> entityRefs)
        {
            const string message = "Loading Audit Records";
            if (!(ServiceClient?.IsReady ?? false))
            {
                throw new InvalidOperationException("No connection");
            }
            IsBusy = true;
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    doWorkEventArgs.Result = WorkAuditRecords(entityRefs);
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

        internal void LoadAuditRecordsFetchXmlAsync(string fetchXml)
        {
            const string message = "Loading Audit Records";
            if (!(ServiceClient?.IsReady ?? false))
            {
                throw new InvalidOperationException("No connection");
            }
            IsBusy = true;
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var entityRefs = ServiceClient.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.Select(e => e.ToEntityReference());
                    doWorkEventArgs.Result = WorkAuditRecords(entityRefs);
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

        internal void LoadEntityAuditsAsync(IEnumerable<ConditionExpression> criteriaConditions = null)
        {
            const string message = "Loading Entity Audits";
            if (!(ServiceClient?.IsReady ?? false))
            {
                throw new InvalidOperationException("No connection");
            }
            IsBusy = true;
            var auditRecords = _auditGogglesView.AuditRecordViewModel.AuditRecords;
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var cc = auditRecords.ToDictionary(ar => ar.Id, ar => ar.ColorCombination);
                    var auditList = new List<Audit>();

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
                            auditList.AddRange(RetrieveAudits(auditRecordGroup, criteriaConditions));
                        }
                    }
                    var entityAuditHelper = new EntityAuditHelper(ServiceClient);
                    var entityAudits = auditList.Select(a => entityAuditHelper.ParseAudit(a, data[a.ObjectId.LogicalName][a.ObjectId.Id], entityMetadatas[a.ObjectId.LogicalName], cc[a.ObjectId.Id]));
                    var defaultEntityAudits = auditRecordGroups.Where(g => !(entityMetadatas[g.Key]?.IsAuditEnabled?.Value ?? false))
                            .SelectMany(g => g.SelectMany(i => entityAuditHelper.GetDefaultEntityAudits(data[g.Key][i], entityMetadatas[g.Key], cc[i])
                                .Where(ea => EntityAuditHelper.CheckChangedDate(ea, criteriaConditions))));

                    doWorkEventArgs.Result = entityAudits.Where(ea => ea != null)
                        .Concat(defaultEntityAudits)
                        .ToList();
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        runWorkerCompletedEventArgs.ThrowIfError();
                        _auditGogglesView.EntityAuditViewModel.SetSource(runWorkerCompletedEventArgs.Result as IEnumerable<EntityAudit>);
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
                    foreach (var entityRef in entityRefGroup)
                    {
                        var id = entityRef.Id;
                        auditRecordList.Add(new AuditRecord(entityMetadata.ObjectTypeCode,
                            id,
                            entityNames[id],
                            logicalName,
                            entityDisplayName,
                            entityIconData));
                    }
                }
            }
            return auditRecordList;
        }

        private IEnumerable<Audit> RetrieveAudits(IEnumerable<Guid> objectIds, IEnumerable<ConditionExpression> criteriaConditions = null)
        {
            var auditList = new List<Audit>();
            if (objectIds?.Any() ?? false)
            {
                var objectIdQueue = new Queue<Guid>(objectIds);
                while (objectIdQueue.Count > 0)
                {
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
                                new ConditionExpression(Audit.Columns.ObjectId, ConditionOperator.In, objectIdQueue.DequeueChunk(100).ToArray()),
                                //new ConditionExpression(Audit.Columns.Action, ConditionOperator.In, EntityAuditHelper.SupportedAuditActionValues)
                            }
                        },
                        PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
                    };
                    if (criteriaConditions?.Any() ?? false)
                    {
                        query.Criteria.Conditions.AddRange(criteriaConditions);
                    }
                    while (true)
                    {
                        var result = Service.RetrieveMultiple(query);
                        auditList.AddRange(result.Entities.Select(e => e.ToEntity<Audit>()));
                        if (result.MoreRecords)
                        {
                            query.PageInfo.PageNumber++;
                            query.PageInfo.PagingCookie = result.PagingCookie;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return auditList;
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
            Settings = SettingsManager.Instance.TryLoad(GetType(), out AuditGogglesSettings settings, ConnectionDetail.ConnectionId.ToString()) ? settings : new AuditGogglesSettings();
        }

        internal void SaveSettings()
        {
            SettingsManager.Instance.Save(GetType(), Settings, ConnectionDetail.ConnectionId.ToString());
        }
    }
}