using Formula81.XrmToolBox.Libraries.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Forms;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Formula81.XrmToolBox.Tools.AuditGoggles.ViewModels;
using System;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Views
{
    public partial class AuditGogglesView : UserControl
    {
        private readonly AuditGogglesPluginControl _auditGogglesPluginControl;

        public AuditEntityViewModel AuditEntityViewModel { get; }
        public AuditRecordViewModel AuditRecordViewModel { get; }
        public EntityAuditViewModel EntityAuditViewModel { get; }

        public AuditGogglesView(AuditGogglesPluginControl auditGogglesPluginControl)
        {
            _auditGogglesPluginControl = auditGogglesPluginControl;

            AuditEntityViewModel = new AuditEntityViewModel(_auditGogglesPluginControl);
            AuditRecordViewModel = new AuditRecordViewModel(_auditGogglesPluginControl);
            EntityAuditViewModel = new EntityAuditViewModel(_auditGogglesPluginControl);

            AuditRecordViewModel.ColorChanged += AuditRecordViewModel_ColorChanged;

            InitializeComponent();

            CloseToolButton.Command = new RelayCommand(ExecuteCloseTool, CanExecuteCloseTool);
            RefreshButton.Command = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);

            AddAuditRecordButton.Command = AuditRecordViewModel.AddCommand;
            AddAuditRecordFXBButton.Command = AuditRecordViewModel.FxbCommand;
            ClearAuditRecordsButton.Command = AuditRecordViewModel.ClearCommand;

            LoadEntityAuditsButton.Command = EntityAuditViewModel.LoadCommand;
            EditEntityAuditFiltersButton.Command = EntityAuditViewModel.EditFilters;
            EditEntityAuditColumnsButton.Command = EntityAuditViewModel.EditColumns;
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

        private void AuditRecordViewModel_ColorChanged(AuditRecord auditRecord)
        {
            EntityAuditViewModel.UpdateColorCombination(auditRecord);
        }

        private void AuditEntityFilterTextBox_FilterChanged(string filter)
        {
            AuditEntityViewModel.SetFilter(filter);
        }
    }
}
