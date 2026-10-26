using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Windows;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Windows
{
    public partial class EntityAuditDetailWindow : Window
    {
        private readonly AuditGogglesPluginControl _auditGogglesPluginControl;

        public EntityAuditDetailWindow(AuditGogglesPluginControl auditGogglesPluginControl)
        {
            _auditGogglesPluginControl = auditGogglesPluginControl;

            InitializeComponent();

            OpenRecordButton.Command = new RelayCommand(ExecuteOpenRecord, CanExecuteOpenRecord);
        }

        internal bool CanExecuteOpenRecord(object parameter)
        {
            return DataContext is EntityAudit entityAudit
                && entityAudit.Record != null;
        }

        internal void ExecuteOpenRecord(object parameter)
        {
            try
            {
                if (DataContext is EntityAudit entityAudit)
                {
                    _auditGogglesPluginControl.OpenInBrowser(new EntityReference(entityAudit.Record.EntityLogicalName, entityAudit.Record.Id));
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Open Record");
            }
        }
    }
}
