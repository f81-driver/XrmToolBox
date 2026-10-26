using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Formula81.XrmToolBox.Tools.AuditGoggles.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Views
{
    public partial class AuditGogglesView : UserControl
    {
        private const double ChangesColumnMinWidth = 200;
        private const double ChangesColumnPadding = 10;

        private readonly AuditGogglesPluginControl _auditGogglesPluginControl;

        public AuditEntityViewModel AuditEntityViewModel { get; }
        public AuditRecordViewModel AuditRecordViewModel { get; }
        public EntityAuditViewModel EntityAuditViewModel { get; }

        private ScrollViewer _scrollViewer;

        public AuditGogglesView(AuditGogglesPluginControl auditGogglesPluginControl)
        {
            _auditGogglesPluginControl = auditGogglesPluginControl;

            AuditEntityViewModel = new AuditEntityViewModel(_auditGogglesPluginControl);
            AuditRecordViewModel = new AuditRecordViewModel(_auditGogglesPluginControl);
            EntityAuditViewModel = new EntityAuditViewModel(_auditGogglesPluginControl);

            AuditRecordViewModel.ColorChanged += AuditRecordViewModel_ColorChanged;
            AuditRecordViewModel.AuditRecordsChanged += AuditRecordViewModel_AuditRecordsChanged;
            EntityAuditViewModel.EntityAuditsReset += EntityAuditViewModel_EntityAuditsReset;

            InitializeComponent();

            CloseToolButton.Command = new RelayCommand(ExecuteCloseTool, CanExecuteCloseTool);
            RefreshButton.Command = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);

            AddAuditRecordFXBButton.Command = AuditRecordViewModel.FxbCommand;
            ClearAuditRecordsButton.Command = AuditRecordViewModel.ClearCommand;

            LoadEntityAuditsButton.Command = EntityAuditViewModel.LoadCommand;

            EntityAuditSortComboBox.ItemsSource = Enum.GetValues(typeof(ListSortDirection));

            EditEntityAuditFiltersButton.Command = EntityAuditViewModel.EditFilters;
            EditEntityAuditColumnsButton.Command = EntityAuditViewModel.EditColumns;

            var columnWidthDescriptor = DependencyPropertyDescriptor.FromProperty(GridViewColumn.WidthProperty, typeof(GridViewColumn));
            foreach (var column in new[] { ChangedDateColumn, ChangedByColumn, RecordColumn, EventColumn })
            {
                columnWidthDescriptor.AddValueChanged(column, EntityAuditColumn_WidthChanged);
            }
        }

        internal bool CanExecuteCloseTool(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy;
        }

        internal bool CanExecuteRefresh(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy
                && _auditGogglesPluginControl.ConnectionDetail != null;
        }

        internal void ExecuteCloseTool(object parameter)
        {
            _auditGogglesPluginControl.CloseTool();
        }

        internal void ExecuteRefresh(object parameter)
        {
            try
            {
                _auditGogglesPluginControl.LoadAuditEntitiesAsync();
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Refresh");
            }
        }

        private void EntityAuditListView_ScrollChanged(object sender, ScrollChangedEventArgs scrollChangedEventArgs)
        {
            if (scrollChangedEventArgs.VerticalChange > 0)
            {
                if (_scrollViewer == null)
                {
                    _scrollViewer = scrollChangedEventArgs.OriginalSource as ScrollViewer;
                }
                EntityAuditViewModel?.ScrollChangedCommand.Execute(scrollChangedEventArgs);
            }
        }

        private void EntityAuditListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateChangesColumnWidth();
        }

        private void EntityAuditColumn_WidthChanged(object sender, EventArgs e)
        {
            UpdateChangesColumnWidth();
        }

        private void UpdateChangesColumnWidth()
        {
            // The Changes column fills the space the other (user resizable) columns leave
            var otherColumnsWidth = new[] { ChangedDateColumn, ChangedByColumn, RecordColumn, EventColumn }
                .Sum(c => double.IsNaN(c.Width) ? c.ActualWidth : c.Width);
            var availableWidth = EntityAuditListView.ActualWidth - otherColumnsWidth - SystemParameters.VerticalScrollBarWidth - ChangesColumnPadding;
            ChangesColumn.Width = Math.Max(ChangesColumnMinWidth, availableWidth);
        }

        private void AuditRecordViewModel_AuditRecordsChanged()
        {
            EntityAuditViewModel.Terminate(AuditRecordViewModel.HasAuditRecords);
        }

        private void EntityAuditViewModel_EntityAuditsReset()
        {
            _scrollViewer?.ScrollToTop();
        }

        private void AuditRecordViewModel_ColorChanged(AuditRecord auditRecord)
        {
            EntityAuditViewModel?.UpdateColorCombination(auditRecord);
        }

        private void AuditEntityFilterTextBox_FilterChanged(string filter)
        {
            AuditEntityViewModel.SetFilter(filter);
        }
    }
}
