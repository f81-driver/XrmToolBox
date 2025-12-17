using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Events;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.ViewModels
{
    public class EntityAuditViewModel : ObservableObject
    {
        private const int PageSize = 100;

        private readonly AuditGogglesPluginControl _auditGogglesPluginControl;

        public IEnumerable<EntityAudit> EntityAudits { get => _entityAuditCollection; }

        private bool _isInitiated;
        public bool IsInitiated { get => _isInitiated; private set => SetValue(nameof(IsInitiated), value, ref _isInitiated); }

        private bool _isLoadRequired;
        public bool IsLoadRequired { get => _isLoadRequired; private set => SetValue(nameof(IsLoadRequired), value, ref _isLoadRequired); }

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

        public ICommand LoadCommand { get; }
        public ICommand EditFilters { get; }
        public ICommand EditColumns { get; }
        public ICommand ScrollChangedCommand { get; }

        public ObservableCollection<EntityAudit> _entityAuditCollection;
        private IEnumerable<ConditionExpression> _criteriaConditions;
        private IDictionary<string, ColumnSet> _columns;
        private readonly DispatcherTimer _scrollDebounce;
        private int _pageNumber = 1;
        private string _pagingCookie;
        private bool _hasMoreRecords = true;
        private bool _isLoading;

        private ScrollChangedEventArgs _lastScrollArgs;

        public event EntityAuditsResetEvent EntityAuditsReset;

        public EntityAuditViewModel(AuditGogglesPluginControl auditGogglesPluginControl)
        {
            _auditGogglesPluginControl = auditGogglesPluginControl;
            _entityAuditCollection = new ObservableCollection<EntityAudit>();
            _entityAuditCollection.CollectionChanged += EntityAuditCollection_CollectionChanged;
            _isInitiated = false;
            _isLoadRequired = false;

            _scrollDebounce = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _scrollDebounce.Tick += ScrollDebounce_Tick;

            _sortDirection = ListSortDirection.Descending;
            LoadCommand = new RelayCommand(ExecuteLoad, CanExecuteLoad);
            EditFilters = new RelayCommand(ExecuteEditFilters, CanExecuteEditFilters);
            EditColumns = new RelayCommand(ExecuteEditColumns, CanExecuteEditColumns);
            ScrollChangedCommand = new RelayCommand(ExecuteScrollChanged, CanExecuteScrollChanged);
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

        internal bool CanExecuteScrollChanged(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy;
        }

        internal void ExecuteLoad(object parameter)
        {
            try
            {
                IsInitiated = true;
                IsLoadRequired = false;
                Clear();
                HandleLoad();
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
            _pageNumber = 1;
            _pagingCookie = null;
            _hasMoreRecords = true;
            _entityAuditCollection.Clear();
        }

        internal void ApplyEntityAuditResult(IEnumerable<EntityAudit> entityAudits, string pagingCookie, bool moreRecords)
        {
            _pagingCookie = pagingCookie;
            _hasMoreRecords = moreRecords;
            if (!string.IsNullOrEmpty(pagingCookie))
            {
                _pageNumber++;
            }

            foreach (var entityAudit in entityAudits)
            {
                _entityAuditCollection.Add(entityAudit);
            }

            _isLoading = false;
        }

        internal void Terminate(bool anyAuditRecords)
        {
            IsInitiated = false;
            IsLoadRequired = anyAuditRecords;
        }

        internal void UpdateColorCombination(AuditRecord auditRecord)
        {
            if (_entityAuditCollection?.Any() ?? false)
            {
                foreach (var entityAudit in _entityAuditCollection.Where(ea => ea.Record.Id.Equals(auditRecord.Id)))
                {
                    entityAudit.ColorCombination = auditRecord.ColorCombination;
                }
            }
        }

        private void HandleScroll(ScrollChangedEventArgs scrollChangedEventArgs)
        {
            if (_isLoading || !_hasMoreRecords)
                return;

            // bottom reached
            if (scrollChangedEventArgs.VerticalOffset + scrollChangedEventArgs.ViewportHeight >= scrollChangedEventArgs.ExtentHeight - 40)
            {
                HandleLoad();
            }
        }

        private void HandleLoad()
        {
            if (_isLoading || !_hasMoreRecords)
                return;

            _isLoading = true;

            var orderType = SortDirection == ListSortDirection.Ascending ? OrderType.Ascending : OrderType.Descending;
            var pageInfo = new PagingInfo { Count = PageSize, PageNumber = _pageNumber, PagingCookie = _pagingCookie };
            _auditGogglesPluginControl.LoadEntityAuditsAsync(_criteriaConditions, _columns, pageInfo, orderType);
        }

        private void ExecuteScrollChanged(object parameter)
        {
            if (parameter is ScrollChangedEventArgs scrollChangedEventArgs
                && IsInitiated)
            {
                _lastScrollArgs = scrollChangedEventArgs;
                _scrollDebounce.Stop();
                _scrollDebounce.Start();
            }
        }

        private void EntityAuditCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                EntityAuditsReset?.Invoke();
            }
        }

        private void ScrollDebounce_Tick(object sender, EventArgs e)
        {
            _scrollDebounce.Stop();
            HandleScroll(_lastScrollArgs);
        }
    }
}
