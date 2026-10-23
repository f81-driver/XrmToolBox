using Formula81.XrmToolBox.Shared.Core.Extensions.ComponentModel;
using Formula81.XrmToolBox.Shared.Properietary;
using Formula81.XrmToolBox.Shared.Xrm.Caches;
using Formula81.XrmToolBox.Shared.Xrm.Exceptions;
using Formula81.XrmToolBox.Shared.Xrm.Extensions.Metadata;
using Formula81.XrmToolBox.Shared.XrmParts.Models;
using Formula81.XrmToolBox.Tools.ViewDriver.Views;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Markup;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace Formula81.XrmToolBox.Tools.ViewDriver
{
    public partial class ViewDriverPluginControl : PluginControlBase, IGitHubPlugin, IPayPalPlugin
    {
        public string RepositoryName => Informations.GitHubRepositoryName;
        public string UserName => Informations.GitHubUserName;
        public string DonationDescription => Informations.PayPalDonationDescription;
        public string EmailAccount => Informations.PayPalEmailAccount;

        private readonly ViewDriverView _viewDriverView;
        public ViewDriverSettings Settings { get; private set; }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { _isBusy = value; UpdateUI(); } }

        private CrmServiceClient ServiceClient { get => (ConnectionDetail?.ServiceClient?.IsReady ?? false ? ConnectionDetail.ServiceClient : null) ?? throw new ServiceClientNotAvailableException(); }

        public ViewDriverPluginControl()
        {
            try
            {
                FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            }
            catch (ArgumentException) { }

            Settings = new ViewDriverSettings();

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

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            base.ClosingPlugin(info);
            SaveSettings();
        }

        internal void LoadEntitiesAsync()
        {
            const string message = "Loading Entities";
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
                        .Select(em => new EntityItem(em.ObjectTypeCode,
                            em.LogicalName,
                            em.GetDisplayLabel(),
                            entityIconDatas[em.LogicalName],
                            Settings.IsFavoriteEntity(em.LogicalName)))
                        .OrderBy(ae => ae.DisplayName)
                        .ToList();
                },
                PostWorkCallBack = runWorkerCompletedEventArgs =>
                {
                    try
                    {
                        runWorkerCompletedEventArgs.ThrowIfError();
                        _viewDriverView.ViewDriverEntityListControl.SetEntities(runWorkerCompletedEventArgs.Result as IEnumerable<EntityItem>);
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

        protected override void OnConnectionUpdated(ConnectionUpdatedEventArgs e)
        {
            base.OnConnectionUpdated(e);

            _viewDriverView.ViewDriverEntityListControl.SetEntities(null);
            //_auditGogglesView.AuditRecordViewModel.Clear();
            //_auditGogglesView.EntityAuditViewModel.Clear();

            EntityIconCache.Instance.ServiceClient = e.ConnectionDetail.ServiceClient;
            EntityNameCache.Instance.ServiceClient = e.ConnectionDetail.ServiceClient;

            LoadSettings();
            LoadEntitiesAsync();
        }

        private void UpdateUI()
        {
            _viewDriverView.IsEnabled = !IsBusy;
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