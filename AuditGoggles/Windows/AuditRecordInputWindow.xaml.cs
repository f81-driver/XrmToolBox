using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Windows
{
    public partial class AuditRecordInputWindow : Window
    {
        private IEnumerable<AuditEntity> _auditEntities;
        public IEnumerable<AuditEntity> AuditEntities { get => _auditEntities; set { _auditEntities = value; OnAuditEntitiesChanged(); } }
        public AuditEntity AuditEntity { get => AuditEntityComboBox?.SelectedItem as AuditEntity; set => AuditEntityComboBox.SelectedItem = value; }
        public IEnumerable<Guid> Ids { get; private set; }

        public AuditRecordInputWindow()
        {
            InitializeComponent();

            Loaded += AuditRecordInputWindow_Loaded;

            SubmitButton.Command = new RelayCommand(ExecuteSubmit, CanExecuteSubmit);
            CancelButton.Command = new RelayCommand(ExecuteCancel, CanExecuteCancel);
        }

        private void AuditRecordInputWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IdTextBox.Focus();
        }

        private bool CanExecuteSubmit(object parameter)
        {
            return (Ids?.Any() ?? false)
                && AuditEntity != null;
        }

        private bool CanExecuteCancel(object parameter)
        {
            return true;
        }

        private void ExecuteSubmit(object parameter)
        {
            DialogResult = true;
            Close();
        }

        private void ExecuteCancel(object parameter)
        {
            DialogResult = false;
            Close();
        }

        private void OnAuditEntitiesChanged()
        {
            AuditEntityComboBox.ItemsSource = _auditEntities.OrderBy(ae => ae.DisplayName)
                .ToList();
        }

        private void IdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Ids = IdTextBox.Text.Split(' ', ',', ';', '\n')
                .Select(t => Guid.TryParse(t, out Guid id) ? (Guid?)id : null)
                .Where(g => g.HasValue)
                .Select(g => g.Value)
                .Distinct()
                .ToList();
        }
    }
}
