using Formula81.XrmToolBox.Libraries.Parts.Input;
using Formula81.XrmToolBox.Libraries.Xrm.Extensions.Metadata;
using Formula81.XrmToolBox.Tools.AuditGoggles.Helpers;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Windows
{
    public partial class EntityAuditColumnsWindow : Window
    {
        private IEnumerable<EntityAuditColumnsItem> _entityAuditColumnsItems;

        public EntityAuditColumnsWindow()
        {
            InitializeComponent();

            SubmitButton.Command = new RelayCommand(ExecuteSubmit, CanExecuteSubmit);
            CancelButton.Command = new RelayCommand(ExecuteCancel, CanExecuteCancel);
        }

        private bool CanExecuteSubmit(object parameter)
        {
            return true;
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

        public void SetSource(IEnumerable<EntityMetadata> entityMetadatas, IDictionary<string, ColumnSet> columns)
        {
            var entityAuditColumnsItemList = new List<EntityAuditColumnsItem>();
            foreach (var entityMetadata in entityMetadatas)
            {
                var attributeSet = EntityAuditHelper.GetEntityAuditEntityColumns(entityMetadata)
                    .ToHashSet();
                var columnSet = columns?.TryGetValue(entityMetadata.LogicalName, out var cs) ?? false ? cs : null;

                var cols = entityMetadata.Attributes.Where(am => attributeSet.Contains(am.LogicalName))
                    .Select(am => new EntityAuditColumnsItemColumn(am.LogicalName, am.GetDisplayLabel())
                    {
                        IsChecked = !(columnSet?.AllColumns ?? false)
                            && (columnSet?.Columns?.Contains(am.LogicalName) ?? false)
                    })
                    .OrderBy(ci => ci.DisplayName);
                entityAuditColumnsItemList.Add(new EntityAuditColumnsItem(entityMetadata.LogicalName, entityMetadata.GetDisplayLabel(), cols)
                {
                    AllColumns = columnSet?.AllColumns ?? true
                });
            }

            _entityAuditColumnsItems = entityAuditColumnsItemList.OrderBy(eaci => eaci.DisplayName)
                .ToList();
            AuditEntityListBox.ItemsSource = _entityAuditColumnsItems;
        }

        public IDictionary<string, ColumnSet> Get()
        {
            return _entityAuditColumnsItems?.ToDictionary(eaci => eaci.Name, eaci => eaci.AllColumns ? new ColumnSet(true)
                    : new ColumnSet(eaci.Columns.Where(c => c.IsChecked).Select(c => c.Value).ToArray()));
        }

        private void AuditEntityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AuditEntityListBox.SelectedItem is EntityAuditColumnsItem entityAuditColumnsItem)
            {
                var allColumns = entityAuditColumnsItem?.AllColumns ?? false;
                AllColumnsCheckBox.IsChecked = allColumns;
                AuditEntityColumnsListBox.IsEnabled = !allColumns;
                AuditEntityColumnsListBox.ItemsSource = entityAuditColumnsItem?.Columns;
            }
        }

        private void AllColumnsCheckBox_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (AuditEntityListBox.SelectedItem is EntityAuditColumnsItem entityAuditColumnsItem)
            {
                entityAuditColumnsItem.AllColumns = AllColumnsCheckBox.IsChecked ?? false;
                AuditEntityColumnsListBox.IsEnabled = !entityAuditColumnsItem.AllColumns;
            }
        }
    }
}
