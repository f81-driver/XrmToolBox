using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Components;
using Formula81.XrmToolBox.Tools.AuditGoggles.Events;
using Formula81.XrmToolBox.Tools.AuditGoggles.Helpers;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Win32;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.ViewModels
{
    public class EntityAuditViewModel : ObservableObject
    {
        private const int PageSize = 100;
        private const int TotalRecordCountLimit = 5000;

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

        private int _count;
        public int Count { get => _count; private set => SetValue(nameof(Count), value, ref _count); }

        private int _total;
        public int Total { get => _total; private set => SetValue(nameof(Total), value, ref _total); }

        private string _statusText;
        public string StatusText { get => _statusText; private set => SetValue(nameof(StatusText), value, ref _statusText); }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; private set => SetValue(nameof(IsLoading), value, ref _isLoading); }

        private ListSortDirection _sortDirection;
        public ListSortDirection SortDirection { get => _sortDirection; set => SetValue(nameof(SortDirection), value, ref _sortDirection); }

        public ICommand LoadCommand { get; }
        public ICommand EditFilters { get; }
        public ICommand EditColumns { get; }
        public ICommand ScrollChangedCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ShowDetailsCommand { get; }
        public ICommand OpenInBrowserCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand AddAuditRecordCommand { get; }

        public ObservableCollection<EntityAudit> _entityAuditCollection;
        private IEnumerable<ConditionExpression> _criteriaConditions;
        private IDictionary<string, ColumnSet> _columns;
        private readonly DispatcherTimer _scrollDebounce;
        private int _pageNumber = 1;
        private string _pagingCookie;
        private bool _hasMoreRecords = true;

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
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
            ShowDetailsCommand = new RelayCommand(ExecuteShowDetails, CanExecuteShowDetails);
            OpenInBrowserCommand = new RelayCommand(ExecuteOpenInBrowser, CanExecuteOpenInBrowser);
            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
            AddAuditRecordCommand = new RelayCommand(ExecuteAddAuditRecord, CanExecuteAddAuditRecord);
        }

        internal bool CanExecuteExport(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy
                && _entityAuditCollection.Count > 0;
        }

        internal bool CanExecuteShowDetails(object parameter)
        {
            return parameter is EntityAudit;
        }

        internal bool CanExecuteOpenInBrowser(object parameter)
        {
            return ToEntityReference(parameter) != null;
        }

        internal bool CanExecuteCopy(object parameter)
        {
            return !string.IsNullOrEmpty(parameter?.ToString());
        }

        internal bool CanExecuteAddAuditRecord(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy
                && ToEntityReference(parameter) != null;
        }

        internal void ExecuteExport(object parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Audits",
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"AuditGoggles_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (saveFileDialog.ShowDialog() ?? false)
                {
                    CsvExportHelper.Export(saveFileDialog.FileName, _entityAuditCollection.ToList());
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Export Audits");
            }
        }

        internal void ExecuteShowDetails(object parameter)
        {
            try
            {
                if (parameter is EntityAudit entityAudit)
                {
                    _auditGogglesPluginControl.ShowEntityAuditDetailDialog(entityAudit);
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Show Audit Details");
            }
        }

        internal void ExecuteOpenInBrowser(object parameter)
        {
            try
            {
                _auditGogglesPluginControl.OpenInBrowser(ToEntityReference(parameter));
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Open in Browser");
            }
        }

        internal void ExecuteCopy(object parameter)
        {
            try
            {
                Clipboard.SetText(parameter.ToString());
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Copy");
            }
        }

        internal void ExecuteAddAuditRecord(object parameter)
        {
            try
            {
                _auditGogglesPluginControl.AddAuditRecords(new[] { ToEntityReference(parameter) });
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Add Audit Record");
            }
        }

        private static EntityReference ToEntityReference(object parameter)
        {
            switch (parameter)
            {
                case EntityLookupValue entityLookupValue:
                    return new EntityReference(entityLookupValue.EntityLogicalName, entityLookupValue.Id);
                case EntityAuditValue entityAuditValue:
                    return entityAuditValue.Value as EntityReference;
                default:
                    return null;
            }
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(SortDirection))
            {
                ReloadIfInitiated();
            }
        }

        private void ReloadIfInitiated()
        {
            if (IsInitiated
                && !IsLoading
                && !_auditGogglesPluginControl.IsBusy)
            {
                ExecuteLoad(null);
            }
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
                var criteriaConditions = _auditGogglesPluginControl.ShowEntityAuditFilterDialog(_criteriaConditions);
                // The dialog returns the same instance when cancelled
                var isChanged = !ReferenceEquals(criteriaConditions, _criteriaConditions);
                _criteriaConditions = criteriaConditions;
                FilterCount = _criteriaConditions?.Count() ?? 0;
                HasFilters = FilterCount > 0;
                if (isChanged)
                {
                    ReloadIfInitiated();
                }
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
                var columns = _auditGogglesPluginControl.ShowEntityAuditColumnsDialog(_columns);
                // The dialog returns the same instance when cancelled
                var isChanged = !ReferenceEquals(columns, _columns);
                _columns = columns?
                    .Where(c => !c.Value.AllColumns)
                    .ToDictionary(c => c.Key, c => c.Value);
                ColumnCount = _columns?.Count() ?? 0;
                HasColumns = ColumnCount > 0;
                if (isChanged)
                {
                    ReloadIfInitiated();
                }
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
            IsLoading = false;
            _entityAuditCollection.Clear();
            Count = 0;
            StatusText = null;
        }

        internal void EndLoad()
        {
            IsLoading = false;
        }

        internal void ApplyEntityAuditResult(EntityAuditResult entityAuditResult)
        {
            _pagingCookie = entityAuditResult.PagingCookie;
            _hasMoreRecords = entityAuditResult.MoreRecords;
            if (!string.IsNullOrEmpty(_pagingCookie))
            {
                _pageNumber++;
            }

            foreach (var entityAudit in entityAuditResult.EntityAudits)
            {
                _entityAuditCollection.Add(entityAudit);
            }

            IsLoading = false;

            Count = _entityAuditCollection.Count;
            if (!entityAuditResult.TotalRecordCountLimitExceeded)
            {
                Total = entityAuditResult.TotalRecordCount;
            }
            // Dataverse stops counting at 5000, so the real total is unknown beyond that
            var totalText = entityAuditResult.TotalRecordCountLimitExceeded ? $"{TotalRecordCountLimit:N0}+" : $"{Total:N0}";
            StatusText = $"Showing {Count:N0} of {totalText} audits";
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

            if (scrollChangedEventArgs.VerticalOffset + scrollChangedEventArgs.ViewportHeight >= scrollChangedEventArgs.ExtentHeight - 40)
            {
                HandleLoad();
            }
        }

        private void HandleLoad()
        {
            if (_isLoading || !_hasMoreRecords)
                return;

            IsLoading = true;

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
