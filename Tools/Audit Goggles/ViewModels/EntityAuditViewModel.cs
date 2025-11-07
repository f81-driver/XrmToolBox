using Formula81.XrmToolBox.Libraries.Core.Components;
using Formula81.XrmToolBox.Libraries.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.ViewModels
{
    public class EntityAuditViewModel : ObservableObject
    {
        private readonly AuditGogglesPluginControl _auditGogglesPluginControl;

        private ICollectionView _entityAuditsViewSource;
        public IEnumerable EntityAuditsViewSource { get => _entityAuditsViewSource; private set => SetValue(nameof(EntityAuditsViewSource), (ICollectionView)value, ref _entityAuditsViewSource); }

        private bool _hasFilters;
        public bool HasFilters { get => _hasFilters; private set => SetValue(nameof(HasFilters), value, ref _hasFilters); }

        private int _filterCount;
        public int FilterCount { get => _filterCount; private set => SetValue(nameof(FilterCount), value, ref _filterCount); }

        private bool _hasColumns;
        public bool HasColumns { get => _hasColumns; private set => SetValue(nameof(HasColumns), value, ref _hasColumns); }

        private int _columnCount;
        public int ColumnCount { get => _columnCount; private set => SetValue(nameof(ColumnCount), value, ref _columnCount); }

        private ListSortDirection _sortDirection;
        public ListSortDirection SortDirection { get => _sortDirection; set => SetValue(nameof(SortDirection), value, ref _sortDirection); }

        private List<EntityAudit> _entityAudits;
        private IEnumerable<ConditionExpression> _criteriaConditions;
        private IDictionary<string, ColumnSet> _columns;

        public ICommand LoadCommand { get; }
        public ICommand EditFilters { get; }
        public ICommand EditColumns { get; }

        public EntityAuditViewModel(AuditGogglesPluginControl auditGogglesPluginControl)
        {
            _auditGogglesPluginControl = auditGogglesPluginControl;
            _sortDirection = ListSortDirection.Descending;
            LoadCommand = new RelayCommand(ExecuteLoad, CanExecuteLoad);
            EditFilters = new RelayCommand(ExecuteEditFilters, CanExecuteEditFilters);
            EditColumns = new RelayCommand(ExecuteEditColumns, CanExecuteEditColumns);
        }

        internal bool CanExecuteLoad(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy;
        }

        internal bool CanExecuteEditFilters(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy;
        }

        internal bool CanExecuteEditColumns(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy
                && _auditGogglesPluginControl.HasAuditRecords();
        }

        internal void ExecuteLoad(object parameter)
        {
            try
            {
                _auditGogglesPluginControl.LoadEntityAuditsAsync(_criteriaConditions, _columns, SortDirection == ListSortDirection.Ascending ? OrderType.Ascending : OrderType.Descending);
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Load Entity Audits");
            }
        }

        internal void ExecuteEditFilters(object parameter)
        {
            try
            {
                _criteriaConditions = _auditGogglesPluginControl.ShowEntityAuditFilterDialog(_criteriaConditions);
                FilterCount = _criteriaConditions?.Count() ?? 0;
                HasFilters = FilterCount > 0;
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Edit Entity Audit Filters");
            }
        }

        internal void ExecuteEditColumns(object parameter)
        {
            try
            {
                _columns = _auditGogglesPluginControl.ShowEntityAuditColumnsDialog(_columns)?
                    .Where(c => !c.Value.AllColumns)
                    .ToDictionary(c => c.Key, c => c.Value);
                ColumnCount = _columns?.Count() ?? 0;
                HasColumns = ColumnCount > 0;
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Edit Entity Audit Columns");
            }
        }

        internal void Clear()
        {
            _entityAuditsViewSource = null;
        }

        internal void SetSource(IEnumerable<EntityAudit> entityAudits)
        {
            _entityAudits = entityAudits?.ToList()
                ?? new List<EntityAudit>();
            var entityAuditsViewSource = new CollectionViewSource { Source = _entityAudits }.View;

            EntityAuditsViewSource = entityAuditsViewSource;
        }

        internal void UpdateColorCombination(AuditRecord auditRecord)
        {
            if (_entityAudits?.Any() ?? false)
            {
                foreach (var entityAudit in _entityAudits.Where(ea => ea.Record.Id.Equals(auditRecord.Id)))
                {
                    entityAudit.ColorCombination = auditRecord.ColorCombination;
                }
            }
        }
    }
}
