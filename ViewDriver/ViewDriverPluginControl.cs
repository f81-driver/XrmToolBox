using Formula81.XrmToolBox.Shared.Core.Extensions.ComponentModel;
using Formula81.XrmToolBox.Shared.Core.Helpers;
using Formula81.XrmToolBox.Shared.Parts.Utilities;
using Formula81.XrmToolBox.Shared.Xrm;
using Formula81.XrmToolBox.Shared.Xrm.Caches;
using Formula81.XrmToolBox.Tools.ViewDriver.Helpers;
using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using Formula81.XrmToolBox.Tools.ViewDriver.Views;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using F81Rect = Formula81.XrmToolBox.Shared.Parts.Components.Rect;
using ConditionOperator = Microsoft.Xrm.Sdk.Query.ConditionOperator;
using MessageBox = System.Windows.MessageBox;

namespace Formula81.XrmToolBox.Tools.ViewDriver
{
    public partial class ViewDriverPluginControl : PluginControlBase, IMessageBusHost, IGitHubPlugin, IPayPalPlugin
    {
        private const string ServiceClientNotAvailableMessage = "Service Client is not available";

        private const string GitHubRepositoryName = "XrmToolBox";
        private const string GitHubUserName = "f81-driver";
        private const string FetchXmlBuilderSourcePluginName = "FetchXML Builder";

        // Personal views (userquery) and system views (savedquery) as entity type codes for the view id in a URL
        private const int UserQueryTypeCode = 4230;
        private const int SavedQueryTypeCode = 1039;

        // System view types that show records: public, advanced find, associated, quick find and lookup
        private static readonly int[] SystemQueryTypes = new[] { 0, 1, 2, 4, 64 };

        public event EventHandler<MessageBusEventArgs> OnOutgoingMessage;

        private int _busyCount;
        public bool IsBusy => _busyCount > 0;

        private readonly ViewDriverView _viewDriverView;

        private readonly Dictionary<string, EntityDetail> _entityDetailCache;
        private readonly Dictionary<Guid, CrmServiceClient> _impersonatedServiceClients;
        private readonly object _impersonatedServiceClientsLock = new object();
        // Impersonated clients no longer handed out, disposed once no work uses a service (_servicesInUse is 0)
        private readonly List<CrmServiceClient> _retiredServiceClients = new List<CrmServiceClient>();
        private int _servicesInUse;
        private Guid _currentUserId;
        private int _viewsRequestId;
        private int _previewRequestId;
        // Bumped on every connection change or reload, so results of loads started before are dropped
        private int _loadGeneration;
        // The view that was sent to FetchXML Builder, the FetchXML that comes back is for this one
        private ViewItem _fetchXmlBuilderView;

        public ViewDriverSettings Settings { get; private set; }

        public string RepositoryName => GitHubRepositoryName;
        public string UserName => GitHubUserName;
        public string DonationDescription => ViewDriverPlugin.PayPalDonationDescription;
        public string EmailAccount => ViewDriverPlugin.PayPalEmailAccount;

        private CrmServiceClient ServiceClient { get => (ConnectionDetail?.ServiceClient?.IsReady ?? false ? ConnectionDetail.ServiceClient : null) ?? throw new InvalidOperationException(ServiceClientNotAvailableMessage); }

        public ViewDriverPluginControl()
        {
            try
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            }
            catch (ArgumentException) { }

            Settings = new ViewDriverSettings();
            _entityDetailCache = new Dictionary<string, EntityDetail>(StringComparer.OrdinalIgnoreCase);
            _impersonatedServiceClients = new Dictionary<Guid, CrmServiceClient>();

            _viewDriverView = new ViewDriverView(this);

            InitializeComponent();

            var viewDriverHost = new ElementHost();
            SuspendLayout();

            viewDriverHost.Dock = DockStyle.Fill;
            viewDriverHost.Name = "ViewDriverHost";
            viewDriverHost.TabIndex = 0;
            viewDriverHost.Child = _viewDriverView;

            Controls.Add(viewDriverHost);
            Name = "ViewDriverControl";
            ResumeLayout(false);
        }

        #region Tables and owners

        internal void LoadEntitiesAsync()
        {
            const string message = "Loading Tables";
            // On a refresh the table selected now, on opening the one selected last time
            var selectedLogicalName = _viewDriverView.EntityListViewModel.SelectedEntity?.LogicalName ?? Settings.SelectedEntity;
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
                    // All tables: which ones are listed is up to the table criteria (by default only those valid for advanced find)
                    var entityMetadatas = response.EntityMetadata.ToList();
                    var entityIconDatas = EntityIconCache.Instance.GetMany(entityMetadatas);
                    var entities = entityMetadatas
                        .Select(em => MetadataHelper.CreateEntity(em,
                            entityIconDatas.TryGetValue(em.LogicalName, out var iconData) ? iconData : null,
                            Settings.IsFavoriteEntity(em.LogicalName)))
                        .ToList();
                    var currentUserId = ((WhoAmIResponse)ServiceClient.Execute(new WhoAmIRequest())).UserId;
                    doWorkEventArgs.Result = (entities, currentUserId, RetrieveOwners(currentUserId));
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        // A load for an earlier connection or reload is dropped, also when it failed
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        runWorkerCompletedEventArgs.ThrowIfError();
                        var (entities, currentUserId, owners) = ((List<ViewEntity>, Guid, List<ViewOwner>))runWorkerCompletedEventArgs.Result;
                        _currentUserId = currentUserId;
                        _viewDriverView.ViewListViewModel.SetOwners(owners);
                        var entityListViewModel = _viewDriverView.EntityListViewModel;
                        entityListViewModel.SetEntities(entities);
                        if (!string.IsNullOrEmpty(selectedLogicalName))
                        {
                            entityListViewModel.Select(entityListViewModel.Find(selectedLogicalName));
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

        private List<ViewOwner> RetrieveOwners(Guid currentUserId)
        {
            var ownerList = new List<ViewOwner>();
            try
            {
                var query = new QueryExpression(SystemUser.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(SystemUser.ColumnNames.FullName, SystemUser.ColumnNames.DomainName),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression(SystemUser.ColumnNames.IsDisabled, ConditionOperator.Equal, false),
                            // Application and support users have no personal views
                            new ConditionExpression(SystemUser.ColumnNames.AccessMode, ConditionOperator.NotIn, (int)SystemUser_AccessMode.SupportUser, (int)SystemUser_AccessMode.Noninteractive)
                        }
                    },
                    PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
                };
                query.AddOrder(SystemUser.ColumnNames.FullName, OrderType.Ascending);
                EntityCollection entityCollection;
                do
                {
                    entityCollection = ServiceClient.RetrieveMultiple(query);
                    ownerList.AddRange(entityCollection.Entities
                        .Select(e => e.ToEntity<SystemUser>())
                        .Select(su => new ViewOwner(su.Id, su.FullName, su.DomainName, su.Id == currentUserId)));
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = entityCollection.PagingCookie;
                }
                while (entityCollection.MoreRecords);
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                // Without read access on users only the own views can be managed
            }
            var currentOwner = ownerList.FirstOrDefault(vo => vo.IsCurrentUser);
            if (currentOwner == null)
            {
                var currentUser = SystemUser.Retrieve(ServiceClient, currentUserId, su => su.FullName, su => su.DomainName);
                currentOwner = new ViewOwner(currentUserId, currentUser.FullName, currentUser.DomainName, true);
            }
            ownerList.Remove(currentOwner);
            ownerList.Insert(0, currentOwner);
            return ownerList;
        }

        #endregion

        #region Views

        internal void SelectEntity(ViewEntity viewEntity)
        {
            _viewDriverView.ViewEditorViewModel.Clear();
            _viewDriverView.PreviewViewModel.Clear();
            _viewDriverView.ViewListViewModel.Clear();
            if (viewEntity != null)
            {
                // Selected again the next time the tool opens on this connection
                Settings.SelectedEntity = viewEntity.LogicalName;
                SaveSettings();
                LoadViewsAsync(null);
            }
        }

        /// <summary>
        /// Loads the views of the selected table for the selected owner, with the metadata of the table when it is not cached yet
        /// </summary>
        internal void LoadViewsAsync(Guid? selectViewId)
        {
            const string message = "Loading Views";
            var viewEntity = _viewDriverView.EntityListViewModel.SelectedEntity;
            var viewOwner = _viewDriverView.ViewListViewModel.SelectedOwner;
            if (viewEntity == null || viewOwner == null)
            {
                return;
            }
            _entityDetailCache.TryGetValue(viewEntity.LogicalName, out var entityDetail);
            var entityDisplayNames = GetEntityDisplayNames();
            var requestId = ++_viewsRequestId;
            var loadGeneration = _loadGeneration;
            BeginBusy();
            _viewDriverView.ViewListViewModel.IsLoading = true;
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    if (entityDetail == null)
                    {
                        entityDetail = RetrieveEntityDetail(viewEntity.LogicalName, entityDisplayNames);
                    }
                    var viewItems = RetrieveSystemViews(viewEntity.LogicalName).ToList();
                    try
                    {
                        viewItems.AddRange(RetrievePersonalViews(viewEntity.LogicalName, viewOwner));
                    }
                    catch (FaultException<OrganizationServiceFault> faultException) when (!viewOwner.IsCurrentUser)
                    {
                        throw new InvalidOperationException($"The personal views of {viewOwner.Name} cannot be read. Showing the views of another user requires the 'Act on Behalf of Another User' privilege.\n\n{faultException.Message}", faultException);
                    }
                    doWorkEventArgs.Result = viewItems;
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        if (requestId != _viewsRequestId || loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        _viewDriverView.ViewListViewModel.IsLoading = false;
                        runWorkerCompletedEventArgs.ThrowIfError();
                        _entityDetailCache[entityDetail.LogicalName] = entityDetail;
                        _viewDriverView.ViewListViewModel.SetViews(runWorkerCompletedEventArgs.Result as IEnumerable<ViewItem>, selectViewId);
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

        private IEnumerable<ViewItem> RetrieveSystemViews(string entityLogicalName)
        {
            var query = new QueryExpression(SavedQuery.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(SavedQuery.ColumnNames.Name, SavedQuery.ColumnNames.Description, SavedQuery.ColumnNames.FetchXml, SavedQuery.ColumnNames.LayoutXml,
                    SavedQuery.ColumnNames.QueryType, SavedQuery.ColumnNames.IsDefault, SavedQuery.ColumnNames.StateCode, SavedQuery.ColumnNames.ModifiedOn),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(SavedQuery.ColumnNames.ReturnedTypeCode, ConditionOperator.Equal, entityLogicalName),
                        new ConditionExpression(SavedQuery.ColumnNames.QueryType, ConditionOperator.In, SystemQueryTypes.Cast<object>().ToArray()),
                        new ConditionExpression(SavedQuery.ColumnNames.FetchXml, ConditionOperator.NotNull)
                    }
                }
            };
            query.AddOrder(SavedQuery.ColumnNames.Name, OrderType.Ascending);
            return ServiceClient.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<SavedQuery>())
                .Select(sq => new ViewItem(ViewKind.System, sq.Id, entityLogicalName, sq.Name, sq.Description, sq.FetchXml, sq.LayoutXml,
                    sq.QueryType ?? 0, sq.IsDefault ?? false, sq.StateCode == SavedQueryState.Inactive, null, null, false, sq.ModifiedOn));
        }

        private IEnumerable<ViewItem> RetrievePersonalViews(string entityLogicalName, ViewOwner viewOwner)
        {
            var query = new QueryExpression(UserQuery.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(UserQuery.ColumnNames.Name, UserQuery.ColumnNames.Description, UserQuery.ColumnNames.FetchXml, UserQuery.ColumnNames.LayoutXml,
                    UserQuery.ColumnNames.QueryType, UserQuery.ColumnNames.OwnerId, UserQuery.ColumnNames.StateCode, UserQuery.ColumnNames.ModifiedOn),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(UserQuery.ColumnNames.ReturnedTypeCode, ConditionOperator.Equal, entityLogicalName),
                        new ConditionExpression(UserQuery.ColumnNames.QueryType, ConditionOperator.Equal, 0)
                    }
                }
            };
            query.AddOrder(UserQuery.ColumnNames.Name, OrderType.Ascending);
            DataCollection<Entity> entities;
            using (var lease = AcquireService(viewOwner))
            {
                entities = lease.Service.RetrieveMultiple(query).Entities;
            }
            return entities
                .Select(e => e.ToEntity<UserQuery>())
                .Select(uq => new ViewItem(ViewKind.Personal, uq.Id, entityLogicalName, uq.Name, uq.Description, uq.FetchXml, uq.LayoutXml,
                    uq.QueryType ?? 0, false, uq.StateCode == UserQueryState.Inactive, uq.OwnerId?.Id, uq.OwnerId?.Name, uq.OwnerId != null && uq.OwnerId.Id != viewOwner.Id, uq.ModifiedOn))
                .ToList();
        }

        internal void OpenView(ViewItem viewItem)
        {
            var editorViewModel = _viewDriverView.ViewEditorViewModel;
            var previewViewModel = _viewDriverView.PreviewViewModel;
            if (viewItem == null)
            {
                editorViewModel.Clear();
                previewViewModel.Clear();
                return;
            }
            try
            {
                if (!_entityDetailCache.TryGetValue(viewItem.EntityLogicalName, out var entityDetail))
                {
                    throw new InvalidOperationException($"The metadata of table '{viewItem.EntityLogicalName}' is not loaded");
                }
                editorViewModel.Load(viewItem, entityDetail);
            }
            catch (Exception exception)
            {
                editorViewModel.Clear();
                previewViewModel.Clear();
                ShowErrorDialog(exception, "Open View");
                return;
            }
            previewViewModel.Clear();
            previewViewModel.Run();
        }

        internal ViewItem CreateNewView(ViewTemplate viewTemplate)
        {
            var viewEntity = _viewDriverView.EntityListViewModel.SelectedEntity;
            var viewOwner = _viewDriverView.ViewListViewModel.SelectedOwner;
            if (viewEntity == null
                || viewOwner == null
                || !_entityDetailCache.TryGetValue(viewEntity.LogicalName, out var entityDetail))
            {
                return null;
            }
            var viewDefinition = ViewDefinition.CreateFromTemplate(entityDetail, viewTemplate ?? ViewTemplate.BuiltIn[0]);
            return new ViewItem(ViewKind.Personal, null, viewEntity.LogicalName, $"My {viewEntity.DisplayName} view", null,
                viewDefinition.GetFetchXml(false), viewDefinition.GetLayoutXml(false), 0, false, false, viewOwner.Id, viewOwner.Name, false, null);
        }

        #region Templates

        private const string TemplateSettingsName = "Templates";

        private ViewTemplateSettings _templateSettings;

        /// <summary>
        /// The built-in templates and the ones people made, which are kept once for all connections
        /// </summary>
        internal IReadOnlyList<ViewTemplate> GetTemplates()
        {
            return ViewTemplate.BuiltIn.Concat(LoadTemplateSettings().Templates).ToList();
        }

        private ViewTemplateSettings LoadTemplateSettings()
        {
            if (_templateSettings == null)
            {
                _templateSettings = SettingsManager.Instance.TryLoad(GetType(), out ViewTemplateSettings templateSettings, TemplateSettingsName)
                    ? templateSettings ?? new ViewTemplateSettings()
                    : new ViewTemplateSettings();
            }
            return _templateSettings;
        }

        /// <summary>
        /// Makes a template of the open view, with its unsaved changes, and shows it in the templates dialog to name and save it
        /// </summary>
        internal void SaveViewAsTemplate()
        {
            try
            {
                var editorViewModel = _viewDriverView.ViewEditorViewModel;
                if (!editorViewModel.HasView
                    || !_entityDetailCache.TryGetValue(editorViewModel.View.EntityLogicalName, out var entityDetail))
                {
                    return;
                }
                var viewDefinition = ViewDefinition.Parse(editorViewModel.GetFetchXml(), editorViewModel.GetLayoutXml(), entityDetail);
                var viewTemplate = viewDefinition.ToTemplate(editorViewModel.Name, editorViewModel.Description, entityDetail, out var leftOut);
                ShowTemplatesDialog(viewTemplate, leftOut);
            }
            catch (Exception exception)
            {
                ShowErrorDialog(exception, "Save as Template");
            }
        }

        /// <summary>
        /// Shows the templates to change, duplicate or delete them; <paramref name="newTemplate"/> is added as a new one
        /// </summary>
        internal void ShowTemplatesDialog(ViewTemplate newTemplate, IEnumerable<string> leftOut)
        {
            var templateSettings = LoadTemplateSettings();
            var window = new ViewTemplatesWindow(ViewTemplate.BuiltIn, templateSettings.Templates, newTemplate, leftOut)
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            var center = WindowUtility.CalculateCenter(window.Width, window.Height, GetWindowBounds());
            window.Left = center.X;
            window.Top = center.Y;
            _ = new WindowInteropHelper(window)
            {
                Owner = Handle
            };
            if (window.ShowDialog() ?? false)
            {
                templateSettings.Templates = window.UserTemplates.ToList();
                SettingsManager.Instance.Save(GetType(), templateSettings, TemplateSettingsName);
            }
        }

        #endregion

        internal void SaveViewAsync(ViewItem viewItem, string name, string description, string fetchXml, string layoutXml, int editVersion)
        {
            const string message = "Saving View";
            var viewOwner = _viewDriverView.ViewListViewModel.SelectedOwner;
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var userQuery = new UserQuery
                    {
                        Name = name,
                        Description = description,
                        FetchXml = fetchXml,
                        LayoutXml = layoutXml
                    };
                    using (var lease = AcquireService(viewOwner))
                    {
                        if (viewItem.IsNew)
                        {
                            userQuery.ReturnedTypeCode = viewItem.EntityLogicalName;
                            userQuery.QueryType = 0;
                            doWorkEventArgs.Result = lease.Service.Create(userQuery);
                        }
                        else
                        {
                            userQuery.Id = viewItem.Id.Value;
                            lease.Service.Update(userQuery);
                            doWorkEventArgs.Result = viewItem.Id.Value;
                        }
                    }
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        // A failed write is reported even after a connection change: the change did not happen
                        runWorkerCompletedEventArgs.ThrowIfError();
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        viewItem.Id = (Guid)runWorkerCompletedEventArgs.Result;
                        viewItem.Name = name;
                        viewItem.Description = description;
                        viewItem.FetchXml = fetchXml;
                        viewItem.LayoutXml = layoutXml;
                        _viewDriverView.ViewEditorViewModel.MarkSaved(viewItem, editVersion);
                        _viewDriverView.ViewListViewModel.Refresh();
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

        /// <summary>
        /// Saves a copy of a view as a personal view of the chosen owner. The copy of the open view includes its unsaved changes.
        /// </summary>
        internal void CloneViewAsync(ViewItem viewItem)
        {
            const string message = "Cloning View";
            var editorViewModel = _viewDriverView.ViewEditorViewModel;
            var isOpenView = editorViewModel.View == viewItem;
            var fetchXml = isOpenView ? editorViewModel.GetFetchXml() : viewItem.FetchXml;
            var layoutXml = isOpenView ? editorViewModel.GetLayoutXml() : viewItem.LayoutXml;
            var description = isOpenView ? editorViewModel.Description : viewItem.Description;
            var viewListViewModel = _viewDriverView.ViewListViewModel;
            if (!TryShowCloneViewDialog($"{(isOpenView ? editorViewModel.Name : viewItem.Name)} (copy)", viewListViewModel.SelectedOwner, out var name, out var viewOwner))
            {
                return;
            }
            // A copy for the shown user reloads the list and opens the copy, which replaces the open view; the open view's own
            // changes go into the copy, but those of another open view would be lost without asking
            if (!isOpenView && viewOwner.Id == viewListViewModel.SelectedOwner?.Id && !ConfirmDiscardChanges())
            {
                return;
            }
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    using (var lease = AcquireService(viewOwner))
                    {
                        doWorkEventArgs.Result = lease.Service.Create(new UserQuery
                        {
                            Name = name,
                            Description = description,
                            ReturnedTypeCode = viewItem.EntityLogicalName,
                            QueryType = 0,
                            FetchXml = fetchXml,
                            LayoutXml = layoutXml
                        });
                    }
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        // A failed write is reported even after a connection change: the change did not happen
                        runWorkerCompletedEventArgs.ThrowIfError();
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        if (viewOwner.Id == viewListViewModel.SelectedOwner?.Id)
                        {
                            if (isOpenView)
                            {
                                // The changes went into the copy
                                editorViewModel.Revert();
                            }
                            LoadViewsAsync((Guid)runWorkerCompletedEventArgs.Result);
                        }
                        else
                        {
                            MessageBox.Show($"'{name}' was added to the personal views of {viewOwner.Name}.", message, MessageBoxButton.OK, MessageBoxImage.Information);
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

        internal void DeleteViewAsync(ViewItem viewItem)
        {
            const string message = "Deleting View";
            if (MessageBox.Show($"Delete the personal view '{viewItem.Name}'?\n\nThis cannot be undone.", message, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }
            var viewListViewModel = _viewDriverView.ViewListViewModel;
            if (viewItem.IsNew)
            {
                DiscardView(viewItem);
                return;
            }
            var viewOwner = viewListViewModel.SelectedOwner;
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    using (var lease = AcquireService(viewOwner))
                    {
                        lease.Service.Delete(UserQuery.EntityLogicalName, viewItem.Id.Value);
                    }
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        // A failed write is reported even after a connection change: the change did not happen
                        runWorkerCompletedEventArgs.ThrowIfError();
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        DiscardView(viewItem);
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

        #region Sharing

        // Teams to share with, loaded on the first share and kept for the connection
        private List<SharePrincipal> _teams;

        /// <summary>
        /// Shows who a personal view is shared with, and grants, changes and revokes access as the owner of the view
        /// </summary>
        internal void ShareViewAsync(ViewItem viewItem)
        {
            const string message = "Loading Sharing";
            if (viewItem == null || viewItem.IsNew || !viewItem.IsPersonal)
            {
                return;
            }
            var viewListViewModel = _viewDriverView.ViewListViewModel;
            // Shared as the owner, who can always share it; a view shared with the selected user may belong to someone else
            var viewOwner = viewListViewModel.Owners.FirstOrDefault(vo => vo.Id == viewItem.OwnerId) ?? viewListViewModel.SelectedOwner;
            var users = viewListViewModel.Owners.Where(vo => vo.Id != viewOwner.Id).ToList();
            var teams = _teams;
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    var loadedTeams = teams ?? RetrieveTeams();
                    using (var lease = AcquireService(viewOwner))
                    {
                        var response = (RetrieveSharedPrincipalsAndAccessResponse)lease.Service.Execute(new RetrieveSharedPrincipalsAndAccessRequest
                        {
                            Target = new EntityReference(UserQuery.EntityLogicalName, viewItem.Id.Value)
                        });
                        doWorkEventArgs.Result = (loadedTeams, response.PrincipalAccesses);
                    }
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        // A load for an earlier connection or reload is dropped, also when it failed
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        runWorkerCompletedEventArgs.ThrowIfError();
                        var (loadedTeams, principalAccesses) = ((List<SharePrincipal>, PrincipalAccess[]))runWorkerCompletedEventArgs.Result;
                        _teams = loadedTeams;
                        var principals = users
                            .Select(vo => new SharePrincipal(vo.Id, SharePrincipal.UserLogicalName, vo.Name, vo.DomainName))
                            .Concat(loadedTeams)
                            .ToList();
                        var principalsById = principals.GroupBy(sp => sp.Id).ToDictionary(g => g.Key, g => g.First());
                        var entries = (principalAccesses ?? new PrincipalAccess[0])
                            .Where(pa => pa.Principal != null)
                            .Select(pa => new ShareEntry(principalsById.TryGetValue(pa.Principal.Id, out var sharePrincipal) ? sharePrincipal
                                    // A disabled user or a team that is not listed: shown by its own name or id
                                    : new SharePrincipal(pa.Principal.Id, pa.Principal.LogicalName, pa.Principal.Name, null),
                                pa.AccessMask))
                            .ToList();
                        if (TryShowShareViewDialog(viewItem.Name, viewOwner.Name, principals, entries, out var shareEntries, out var removedPrincipals))
                        {
                            ApplySharingAsync(viewItem, viewOwner, shareEntries, removedPrincipals);
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

        private void ApplySharingAsync(ViewItem viewItem, ViewOwner viewOwner, IEnumerable<ShareEntry> shareEntries, IEnumerable<SharePrincipal> removedPrincipals)
        {
            const string message = "Sharing View";
            var target = new EntityReference(UserQuery.EntityLogicalName, viewItem.Id.Value);
            var removedPrincipalList = removedPrincipals.ToList();
            var removedIds = new HashSet<Guid>(removedPrincipalList.Select(sp => sp.Id));
            var requests = removedPrincipalList
                .Select(sp => (OrganizationRequest)new RevokeAccessRequest { Target = target, Revokee = new EntityReference(sp.LogicalName, sp.Id) })
                .Concat(shareEntries
                    // New shares need Read; existing ones are only touched when their access changed
                    .Where(se => !removedIds.Contains(se.Principal.Id) && (se.IsNew ? se.CanRead : se.GetRights() != se.OriginalRights))
                    .Select(se =>
                    {
                        var principalAccess = new PrincipalAccess { Principal = new EntityReference(se.Principal.LogicalName, se.Principal.Id), AccessMask = se.GetRights() };
                        return se.IsNew
                            ? (OrganizationRequest)new GrantAccessRequest { Target = target, PrincipalAccess = principalAccess }
                            : new ModifyAccessRequest { Target = target, PrincipalAccess = principalAccess };
                    }))
                .ToList();
            if (!requests.Any())
            {
                return;
            }
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    // One by one, so an error names the share that failed and the ones before it stay done
                    using (var lease = AcquireService(viewOwner))
                    {
                        foreach (var request in requests)
                        {
                            lease.Service.Execute(request);
                        }
                    }
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        // A failed write is reported even after a connection change: the change did not happen
                        runWorkerCompletedEventArgs.ThrowIfError();
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
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

        /// <summary>
        /// Teams of any kind (owner, access and Microsoft Entra group teams)
        /// </summary>
        private List<SharePrincipal> RetrieveTeams()
        {
            var teamList = new List<SharePrincipal>();
            try
            {
                var query = new QueryExpression(Team.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(Team.ColumnNames.Name, Team.ColumnNames.TeamType, Team.ColumnNames.IsDefault),
                    PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
                };
                query.AddOrder(Team.ColumnNames.Name, OrderType.Ascending);
                EntityCollection entityCollection;
                do
                {
                    entityCollection = ServiceClient.RetrieveMultiple(query);
                    teamList.AddRange(entityCollection.Entities
                        .Select(e => e.ToEntity<Team>())
                        .Select(t => new SharePrincipal(t.Id, Team.EntityLogicalName, t.Name, GetTeamTypeText(t.TeamType, t.IsDefault ?? false))));
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = entityCollection.PagingCookie;
                }
                while (entityCollection.MoreRecords);
            }
            catch (FaultException<OrganizationServiceFault>)
            {
                // Without read access on teams the view can still be shared with users
            }
            return teamList;
        }

        private static string GetTeamTypeText(Team_TeamType? teamType, bool isDefault)
        {
            // The label of the team type in the metadata: Owner, Access, Security Group or Office Group
            var text = teamType.HasValue ? $"{EnumHelper.GetEnumAttributeProperty(teamType.Value, (OptionSetMetadataAttribute osma) => osma.Name)} team" : null;
            return isDefault ? $"{text}, business unit team" : text;
        }

        #endregion

        private void DiscardView(ViewItem viewItem)
        {
            var editorViewModel = _viewDriverView.ViewEditorViewModel;
            if (editorViewModel.View == viewItem)
            {
                editorViewModel.Clear();
                _viewDriverView.PreviewViewModel.Clear();
            }
            _viewDriverView.ViewListViewModel.RemoveView(viewItem);
        }

        /// <summary>
        /// Asks to discard the unsaved changes of the open view, if any, before something else is opened
        /// </summary>
        internal bool ConfirmDiscardChanges()
        {
            var editorViewModel = _viewDriverView.ViewEditorViewModel;
            var viewItem = editorViewModel.View;
            if (!editorViewModel.IsDirty || viewItem == null)
            {
                return true;
            }
            if (MessageBox.Show($"The view '{editorViewModel.Name}' has unsaved changes.\n\nDiscard them?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return false;
            }
            if (viewItem.IsNew)
            {
                DiscardView(viewItem);
            }
            else
            {
                viewItem.IsModified = false;
                editorViewModel.Clear();
            }
            return true;
        }

        internal void RevertNewView(ViewItem viewItem)
        {
            DiscardView(viewItem);
        }

        #endregion

        #region Metadata

        internal bool TryGetEntityDetail(string logicalName, out EntityDetail entityDetail)
        {
            entityDetail = null;
            return !string.IsNullOrEmpty(logicalName)
                && _entityDetailCache.TryGetValue(logicalName, out entityDetail);
        }

        /// <summary>
        /// Loads the metadata of tables that are not cached yet, for related columns, and calls back on the UI thread when done
        /// </summary>
        internal void LoadEntityDetailsAsync(IEnumerable<string> logicalNames, Action callback)
        {
            const string message = "Loading Related Tables";
            var missingLogicalNames = logicalNames.Where(ln => !string.IsNullOrEmpty(ln) && !_entityDetailCache.ContainsKey(ln)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!missingLogicalNames.Any())
            {
                callback?.Invoke();
                return;
            }
            var entityDisplayNames = GetEntityDisplayNames();
            var loadGeneration = _loadGeneration;
            BeginBusy();
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    doWorkEventArgs.Result = missingLogicalNames
                        .Select(ln => RetrieveEntityDetail(ln, entityDisplayNames))
                        .ToList();
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        // A load for an earlier connection or reload is dropped, also when it failed
                        if (loadGeneration != _loadGeneration)
                        {
                            return;
                        }
                        runWorkerCompletedEventArgs.ThrowIfError();
                        foreach (var entityDetail in (IEnumerable<EntityDetail>)runWorkerCompletedEventArgs.Result)
                        {
                            _entityDetailCache[entityDetail.LogicalName] = entityDetail;
                        }
                        callback?.Invoke();
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

        private EntityDetail RetrieveEntityDetail(string logicalName, IDictionary<string, string> entityDisplayNames)
        {
            var response = (RetrieveEntityResponse)ServiceClient.Execute(new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                RetrieveAsIfPublished = true
            });
            return MetadataHelper.CreateEntityDetail(response.EntityMetadata, ln => entityDisplayNames.TryGetValue(ln, out var displayName) ? displayName : null);
        }

        private IDictionary<string, string> GetEntityDisplayNames()
        {
            // Copied, as it is read on a background thread
            return _viewDriverView.EntityListViewModel.Entities
                .GroupBy(ve => ve.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().DisplayName, StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Preview

        /// <summary>
        /// Runs the query of the open view, as the owner of the shown views, and shows the records in the preview
        /// </summary>
        internal void LoadPreviewAsync(bool append)
        {
            var editorViewModel = _viewDriverView.ViewEditorViewModel;
            var previewViewModel = _viewDriverView.PreviewViewModel;
            if (!editorViewModel.HasView)
            {
                return;
            }
            string fetchXml;
            try
            {
                fetchXml = editorViewModel.GetFetchXml();
            }
            catch (Exception exception)
            {
                previewViewModel.SetError(exception.Message);
                return;
            }
            // The next page must line up with the rows already shown
            var cellNames = append ? previewViewModel.CellNames.ToList() : editorViewModel.Columns.Select(vc => vc.Name).ToList();
            var viewOwner = _viewDriverView.ViewListViewModel.SelectedOwner;
            var pageSize = Settings.PreviewPageSize;
            var pageNumber = append ? previewViewModel.PageNumber + 1 : 1;
            var pagingCookie = append ? previewViewModel.PagingCookie : null;
            var requestId = ++_previewRequestId;
            var loadGeneration = _loadGeneration;
            previewViewModel.BeginLoad();
            WorkAsync(new WorkAsyncInfo
            {
                // No message: the preview shows its own progress, so the rest of the tool stays usable
                Work = (backgroundWorker, doWorkEventArgs) =>
                {
                    using (var lease = AcquireService(viewOwner))
                    {
                        doWorkEventArgs.Result = PreviewHelper.Retrieve(lease.Service, fetchXml, cellNames, pageSize, pageNumber, pagingCookie);
                    }
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    // A newer preview was started, or the view was closed
                    if (requestId != _previewRequestId || loadGeneration != _loadGeneration)
                    {
                        return;
                    }
                    if (runWorkerCompletedEventArgs.Error != null)
                    {
                        previewViewModel.SetError(runWorkerCompletedEventArgs.Error.Message);
                    }
                    else
                    {
                        previewViewModel.SetResult(runWorkerCompletedEventArgs.Result as PreviewResult, append);
                    }
                }
            });
        }

        internal void CancelPreview()
        {
            _previewRequestId++;
        }

        internal bool CanOpenInBrowser()
        {
            return !string.IsNullOrEmpty(ConnectionDetail?.WebApplicationUrl);
        }

        internal void OpenRecord(PreviewRow previewRow)
        {
            if (previewRow != null && CanOpenInBrowser())
            {
                var url = $"{ConnectionDetail.WebApplicationUrl.TrimEnd('/')}/main.aspx?pagetype=entityrecord&etn={previewRow.EntityLogicalName}&id={previewRow.Id}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        internal void OpenViewInBrowser(ViewItem viewItem)
        {
            if (viewItem?.Id != null && CanOpenInBrowser())
            {
                var viewType = viewItem.Kind == ViewKind.Personal ? UserQueryTypeCode : SavedQueryTypeCode;
                var url = $"{ConnectionDetail.WebApplicationUrl.TrimEnd('/')}/main.aspx?pagetype=entitylist&etn={viewItem.EntityLogicalName}&viewid={viewItem.Id}&viewtype={viewType}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        #endregion

        #region FetchXML Builder

        internal void CallFetchXmlBuilder(string fetchXml)
        {
            _fetchXmlBuilderView = _viewDriverView.ViewEditorViewModel.View;
            OnOutgoingMessage?.Invoke(this, new MessageBusEventArgs(FetchXmlBuilderSourcePluginName)
            {
                TargetArgument = fetchXml
            });
        }

        public void OnIncomingMessage(MessageBusEventArgs message)
        {
            if (message.SourcePlugin.Equals(FetchXmlBuilderSourcePluginName)
                && message.TargetArgument is string fetchXml
                && !string.IsNullOrEmpty(fetchXml))
            {
                var editorViewModel = _viewDriverView.ViewEditorViewModel;
                if (editorViewModel.View == null || editorViewModel.View != _fetchXmlBuilderView)
                {
                    MessageBox.Show("The view that was sent to FetchXML Builder is no longer open, so the FetchXML was not applied.", "FetchXML Builder", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                try
                {
                    editorViewModel.ApplyFetchXml(fetchXml);
                }
                catch (Exception exception)
                {
                    ShowErrorDialog(exception, "FetchXML Builder");
                }
            }
        }

        #endregion

        #region Dialogs

        private bool TryShowCloneViewDialog(string defaultName, ViewOwner defaultOwner, out string name, out ViewOwner viewOwner)
        {
            var window = new CloneViewWindow(_viewDriverView.ViewListViewModel.Owners, defaultName, defaultOwner)
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            var center = WindowUtility.CalculateCenter(window.Width, window.Height, GetWindowBounds());
            window.Left = center.X;
            window.Top = center.Y;
            _ = new WindowInteropHelper(window)
            {
                Owner = Handle
            };
            var isConfirmed = window.ShowDialog() ?? false;
            name = window.ViewName;
            viewOwner = window.ViewOwner;
            return isConfirmed && !string.IsNullOrWhiteSpace(name) && viewOwner != null;
        }

        private bool TryShowShareViewDialog(string viewName, string ownerName, IEnumerable<SharePrincipal> principals, IEnumerable<ShareEntry> entries,
            out IReadOnlyList<ShareEntry> shareEntries, out IEnumerable<SharePrincipal> removedPrincipals)
        {
            var window = new ShareViewWindow(viewName, ownerName, principals, entries)
            {
                WindowStartupLocation = WindowStartupLocation.Manual
            };
            var center = WindowUtility.CalculateCenter(window.Width, window.Height, GetWindowBounds());
            window.Left = center.X;
            window.Top = center.Y;
            _ = new WindowInteropHelper(window)
            {
                Owner = Handle
            };
            var isConfirmed = window.ShowDialog() ?? false;
            shareEntries = window.Entries;
            removedPrincipals = window.RemovedPrincipals;
            return isConfirmed;
        }

        private F81Rect GetWindowBounds()
        {
            var rectangle = new F81Rect();
            return WindowUtility.GetWindowRect(Handle, ref rectangle) ? rectangle : default;
        }

        #endregion

        /// <summary>
        /// The organization service as the owner of the shown views. Another user is impersonated on a clone of the connection,
        /// so the connection that other tools share is not touched.
        /// </summary>
        /// <summary>
        /// The service to use for one piece of work, typically on a background thread; dispose the lease when done, so clients
        /// replaced in the meantime (connection change, reload) are only disposed once nothing uses them any more
        /// </summary>
        private ServiceLease AcquireService(ViewOwner viewOwner)
        {
            lock (_impersonatedServiceClientsLock)
            {
                var service = GetService(viewOwner);
                _servicesInUse++;
                return new ServiceLease(service, ReleaseService);
            }
        }

        private void ReleaseService()
        {
            lock (_impersonatedServiceClientsLock)
            {
                _servicesInUse--;
                if (_servicesInUse == 0)
                {
                    DisposeRetiredServiceClients();
                }
            }
        }

        private sealed class ServiceLease : IDisposable
        {
            private Action _release;

            public IOrganizationService Service { get; }

            public ServiceLease(IOrganizationService service, Action release)
            {
                Service = service;
                _release = release;
            }

            public void Dispose()
            {
                _release?.Invoke();
                _release = null;
            }
        }

        // Only called by AcquireService, under the lock
        private IOrganizationService GetService(ViewOwner viewOwner)
        {
            if (viewOwner == null || viewOwner.IsCurrentUser)
            {
                return ServiceClient;
            }
            lock (_impersonatedServiceClientsLock)
            {
                if (!_impersonatedServiceClients.TryGetValue(viewOwner.Id, out var serviceClient))
                {
                    serviceClient = ServiceClient.Clone();
                    if (!(serviceClient?.IsReady ?? false))
                    {
                        throw new InvalidOperationException($"The connection could not be cloned to act on behalf of {viewOwner.Name}: {serviceClient?.LastCrmError}");
                    }
                    serviceClient.CallerId = viewOwner.Id;
                    _impersonatedServiceClients[viewOwner.Id] = serviceClient;
                }
                return serviceClient;
            }
        }

        internal void Reload()
        {
            if (!ConfirmDiscardChanges())
            {
                return;
            }
            ResetCaches();
            _viewDriverView.ViewEditorViewModel.Clear();
            _viewDriverView.PreviewViewModel.Clear();
            _viewDriverView.ViewListViewModel.Clear();
            LoadEntitiesAsync();
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            var editorViewModel = _viewDriverView.ViewEditorViewModel;
            if (!info.Silent
                && editorViewModel.IsDirty
                && MessageBox.Show($"The view '{editorViewModel.Name}' has unsaved changes.\n\nClose anyway?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                info.Cancel = true;
                return;
            }
            base.ClosingPlugin(info);
            SaveSettings();
            DisposeImpersonatedServiceClients();
        }

        protected override void OnConnectionUpdated(ConnectionUpdatedEventArgs e)
        {
            base.OnConnectionUpdated(e);

            // The connection change cannot be cancelled and the view belongs to the old environment, so the unsaved changes
            // cannot be kept; offer to copy them instead of dropping them silently
            var editorViewModel = _viewDriverView.ViewEditorViewModel;
            if (editorViewModel.IsDirty && editorViewModel.HasView
                && MessageBox.Show($"The connection changed, so the unsaved changes of the view '{editorViewModel.Name}' cannot be kept.\n\nCopy its FetchXML and LayoutXML to the clipboard?",
                    "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) == MessageBoxResult.Yes)
            {
                try
                {
                    System.Windows.Clipboard.SetText($"<!-- {editorViewModel.Name} -->\n{editorViewModel.GetFetchXml()}\n\n{editorViewModel.GetLayoutXml()}");
                }
                catch (Exception exception)
                {
                    ShowErrorDialog(exception, "Copy View");
                }
            }

            ResetCaches();
            _viewDriverView.ViewEditorViewModel.Clear();
            _viewDriverView.PreviewViewModel.Clear();
            _viewDriverView.ViewListViewModel.Clear();
            _viewDriverView.ViewListViewModel.SetOwners(Enumerable.Empty<ViewOwner>());
            _viewDriverView.EntityListViewModel.Clear();

            EntityIconCache.Instance.Clear();
            EntityIconCache.Instance.ServiceClient = e.ConnectionDetail.ServiceClient;

            LoadSettings();
            _viewDriverView.EntityListViewModel.NotifySettingsChanged();
            _viewDriverView.ViewListViewModel.NotifySettingsChanged();
            _viewDriverView.PreviewViewModel.NotifySettingsChanged();
            LoadEntitiesAsync();
        }

        private void ResetCaches()
        {
            _loadGeneration++;
            _entityDetailCache.Clear();
            _teams = null;
            _fetchXmlBuilderView = null;
            DisposeImpersonatedServiceClients();
        }

        /// <summary>
        /// Stops using the impersonated clients; they are disposed now, or when the last work that still uses one is done
        /// </summary>
        private void DisposeImpersonatedServiceClients()
        {
            lock (_impersonatedServiceClientsLock)
            {
                _retiredServiceClients.AddRange(_impersonatedServiceClients.Values);
                _impersonatedServiceClients.Clear();
                if (_servicesInUse == 0)
                {
                    DisposeRetiredServiceClients();
                }
            }
        }

        // Under the lock
        private void DisposeRetiredServiceClients()
        {
            foreach (var serviceClient in _retiredServiceClients)
            {
                serviceClient.Dispose();
            }
            _retiredServiceClients.Clear();
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
                Settings = SettingsManager.Instance.TryLoad(GetType(), out ViewDriverSettings settings, ConnectionDetail.ConnectionId.ToString()) ? settings : new ViewDriverSettings();
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
