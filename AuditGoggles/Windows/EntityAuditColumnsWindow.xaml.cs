using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Shared.Xrm.Extensions.Metadata;
using Formula81.XrmToolBox.Tools.AuditGoggles.Helpers;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Windows
{
    public partial class EntityAuditColumnsWindow : Window
    {
        private ICollectionView _entityAuditColumnsViewSource;

        private IEnumerable<EntityAuditColumnsItem> _entityAuditColumnsItems;

        private string _entityAuditColumnsFilter;

        public EntityAuditColumnsWindow()
        {
            InitializeComponent();

            CheckAllColumnsListBoxContextMenuItem.Command = new RelayCommand(ExecuteSelectColumns, CanExecuteSelectColumns);
            CheckAllColumnsListBoxContextMenuItem.CommandParameter = true;
            UncheckAllColumnsListBoxContextMenuItem.Command = new RelayCommand(ExecuteSelectColumns, CanExecuteSelectColumns);
            UncheckAllColumnsListBoxContextMenuItem.CommandParameter = false;
            ResetButton.Command = new RelayCommand(ExecuteReset, CanExecuteReset);
            SubmitButton.Command = new RelayCommand(ExecuteSubmit, CanExecuteSubmit);
            CancelButton.Command = new RelayCommand(ExecuteCancel, CanExecuteCancel);
        }

        private bool CanExecuteSelectColumns(object parameter)
        {
            return AuditEntityListBox.SelectedItem is EntityAuditColumnsItem entityAuditColumnsItem
                && !entityAuditColumnsItem.AllColumns;
        }

        private bool CanExecuteReset(object parameter)
        {
            return true;
        }

        private bool CanExecuteSubmit(object parameter)
        {
            return true;
        }

        private bool CanExecuteCancel(object parameter)
        {
            return true;
        }

        private void ExecuteSelectColumns(object parameter)
        {
            if (parameter is bool isChecked)
            {
                foreach (var column in _entityAuditColumnsViewSource.OfType<EntityAuditColumnsItemColumn>())
                {
                    column.IsChecked = isChecked;
                }
            }
        }

        private void ExecuteReset(object parameter)
        {
            foreach (var item in _entityAuditColumnsItems)
            {
                item.AllColumns = true;
            }
            DialogResult = true;
            Close();
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
            return _entityAuditColumnsItems?.ToDictionary(eaci => eaci.Name, eaci => eaci.AllColumns 
                    || eaci.Columns.All(c => c.IsChecked) ? new ColumnSet(true)
                        : new ColumnSet(eaci.Columns.Where(c => c.IsChecked).Select(c => c.Value).ToArray()));
        }

        private void AuditEntityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AuditEntityListBox.SelectedItem is EntityAuditColumnsItem entityAuditColumnsItem)
            {
                var allColumns = entityAuditColumnsItem.AllColumns;
                AllColumnsCheckBox.IsChecked = allColumns;
                AuditEntityColumnsListBox.IsEnabled = !allColumns;

                _entityAuditColumnsViewSource = new CollectionViewSource
                {
                    Source = entityAuditColumnsItem.Columns,
                    SortDescriptions =
                    {
                        new SortDescription(nameof(EntityAuditColumnsItemColumn.DisplayName), ListSortDirection.Ascending)
                    }
                }.View;
                _entityAuditColumnsViewSource.Filter = FilterEntityAuditColumnsItemColumn;
            }
            else
            {
                _entityAuditColumnsViewSource = null;
            }
            AuditEntityColumnsListBox.ItemsSource = _entityAuditColumnsViewSource;
        }

        private void AllColumnsCheckBox_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (AuditEntityListBox.SelectedItem is EntityAuditColumnsItem entityAuditColumnsItem)
            {
                entityAuditColumnsItem.AllColumns = AllColumnsCheckBox.IsChecked ?? false;
                AuditEntityColumnsListBox.IsEnabled = !entityAuditColumnsItem.AllColumns;
            }
        }

        private void ColumnFilterTextBox_FilterChanged(string filter)
        {
            _entityAuditColumnsFilter = filter;
            _entityAuditColumnsViewSource?.Refresh();
        }

        private bool FilterEntityAuditColumnsItemColumn(object obj)
        {
            if (obj is EntityAuditColumnsItemColumn column)
            {
                return string.IsNullOrEmpty(_entityAuditColumnsFilter)
                    || column.DisplayName.IndexOf(_entityAuditColumnsFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }
    }
}
