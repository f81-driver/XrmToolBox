using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.ViewDriver.Helpers;
using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace Formula81.XrmToolBox.Tools.ViewDriver.ViewModels
{
    public class PreviewViewModel : ObservableObject
    {
        private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(700);

        // A new result arrived for other columns, the grid columns must be rebuilt
        public event EventHandler ColumnsReset;

        private readonly ViewDriverPluginControl _viewDriverPluginControl;
        private readonly ViewEditorViewModel _viewEditorViewModel;
        private readonly DispatcherTimer _refreshTimer;

        public ObservableCollection<PreviewRow> Rows { get; }

        public IReadOnlyList<string> CellNames { get; private set; } = new string[0];
        public IReadOnlyDictionary<string, int> CellIndexes { get; private set; } = new Dictionary<string, int>();

        public int PageNumber { get; private set; }
        public string PagingCookie { get; private set; }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; private set => SetValue(nameof(IsLoading), value, ref _isLoading); }

        private bool _hasMore;
        public bool HasMore { get => _hasMore; private set => SetValue(nameof(HasMore), value, ref _hasMore); }

        // The query changed since the data was loaded
        private bool _isOutdated;
        public bool IsOutdated { get => _isOutdated; private set => SetValue(nameof(IsOutdated), value, ref _isOutdated); }

        private string _statusText;
        public string StatusText { get => _statusText; private set => SetValue(nameof(StatusText), value, ref _statusText); }

        private string _errorText;
        public string ErrorText { get => _errorText; private set { SetValue(nameof(ErrorText), value, ref _errorText); NotifyPropertyChanged(nameof(HasError)); } }

        public bool HasError => !string.IsNullOrEmpty(_errorText);

        public bool AutoRefresh
        {
            get => !_viewDriverPluginControl.Settings.ManualPreviewRefresh;
            set
            {
                _viewDriverPluginControl.Settings.ManualPreviewRefresh = !value;
                _viewDriverPluginControl.SaveSettings();
                NotifyPropertyChanged(nameof(AutoRefresh));
                if (value && IsOutdated)
                {
                    Run();
                }
            }
        }

        public ICommand RunCommand { get; }
        public ICommand LoadMoreCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand OpenRecordCommand { get; }
        public ICommand CopyCommand { get; }

        private int _totalRecordCount = -1;
        private bool _totalRecordCountLimitExceeded;

        public PreviewViewModel(ViewDriverPluginControl viewDriverPluginControl, ViewEditorViewModel viewEditorViewModel)
        {
            _viewDriverPluginControl = viewDriverPluginControl;
            _viewEditorViewModel = viewEditorViewModel;

            Rows = new ObservableCollection<PreviewRow>();

            // Waits until editing pauses, so typing a condition value does not run a query per key
            _refreshTimer = new DispatcherTimer { Interval = RefreshDelay };
            _refreshTimer.Tick += RefreshTimer_Tick;

            RunCommand = new RelayCommand(ExecuteRun, CanExecuteRun);
            LoadMoreCommand = new RelayCommand(ExecuteLoadMore, CanExecuteLoadMore);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
            OpenRecordCommand = new RelayCommand(ExecuteOpenRecord, CanExecuteOpenRecord);
            CopyCommand = new RelayCommand(ExecuteCopy, p => !string.IsNullOrEmpty(p?.ToString()));

            _viewEditorViewModel.QueryChanged += ViewEditorViewModel_QueryChanged;
        }

        internal bool CanExecuteRun(object parameter)
        {
            return _viewEditorViewModel.HasView;
        }

        internal bool CanExecuteLoadMore(object parameter)
        {
            // The next page of another query would not match the rows shown
            return HasMore && !IsLoading && !IsOutdated;
        }

        internal bool CanExecuteExport(object parameter)
        {
            return Rows.Any();
        }

        internal bool CanExecuteOpenRecord(object parameter)
        {
            return parameter is PreviewRow previewRow
                && previewRow.Id != Guid.Empty
                && _viewDriverPluginControl.CanOpenInBrowser();
        }

        internal void ExecuteRun(object parameter)
        {
            Run();
        }

        internal void ExecuteLoadMore(object parameter)
        {
            _viewDriverPluginControl.LoadPreviewAsync(true);
        }

        internal void ExecuteExport(object parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Records",
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"{_viewEditorViewModel.Name ?? "View"}.csv"
                };
                if (saveFileDialog.ShowDialog() ?? false)
                {
                    // The columns as they are shown, that have data
                    var viewColumns = _viewEditorViewModel.Columns.Where(vc => CellIndexes.ContainsKey(vc.Name)).ToList();
                    CsvExportHelper.Export(saveFileDialog.FileName,
                        viewColumns.Select(vc => vc.DisplayName),
                        Rows.Select(pr => viewColumns.Select(vc => pr.Cells[CellIndexes[vc.Name]])));
                }
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Export Records");
            }
        }

        internal void ExecuteOpenRecord(object parameter)
        {
            try
            {
                _viewDriverPluginControl.OpenRecord(parameter as PreviewRow);
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Open Record");
            }
        }

        internal void ExecuteCopy(object parameter)
        {
            try
            {
                System.Windows.Clipboard.SetText(parameter.ToString());
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Copy");
            }
        }

        internal void Run()
        {
            _refreshTimer.Stop();
            _viewDriverPluginControl.LoadPreviewAsync(false);
        }

        internal void BeginLoad()
        {
            IsLoading = true;
            ErrorText = null;
        }

        internal void SetResult(PreviewResult previewResult, bool append)
        {
            IsLoading = false;
            if (!append)
            {
                Rows.Clear();
                CellNames = previewResult.CellNames;
                CellIndexes = previewResult.CellNames
                    .Select((cn, i) => (Name: cn, Index: i))
                    .GroupBy(c => c.Name)
                    .ToDictionary(g => g.Key, g => g.First().Index);
                IsOutdated = false;
                ColumnsReset?.Invoke(this, EventArgs.Empty);
            }
            foreach (var previewRow in previewResult.Rows)
            {
                Rows.Add(previewRow);
            }
            PageNumber = previewResult.PageNumber;
            PagingCookie = previewResult.PagingCookie;
            HasMore = previewResult.MoreRecords;
            _totalRecordCount = previewResult.TotalRecordCount;
            _totalRecordCountLimitExceeded = previewResult.TotalRecordCountLimitExceeded;
            UpdateStatusText();
        }

        internal void SetError(string message)
        {
            IsLoading = false;
            ErrorText = message;
        }

        internal void Clear()
        {
            _refreshTimer.Stop();
            _viewDriverPluginControl.CancelPreview();
            Rows.Clear();
            CellNames = new string[0];
            CellIndexes = new Dictionary<string, int>();
            PageNumber = 0;
            PagingCookie = null;
            HasMore = false;
            IsOutdated = false;
            IsLoading = false;
            ErrorText = null;
            StatusText = null;
            _totalRecordCount = -1;
            _totalRecordCountLimitExceeded = false;
            ColumnsReset?.Invoke(this, EventArgs.Empty);
        }

        internal void NotifySettingsChanged()
        {
            NotifyPropertyChanged(nameof(AutoRefresh));
        }

        private void UpdateStatusText()
        {
            if (!Rows.Any())
            {
                StatusText = "No records";
                return;
            }
            var count = Rows.Count.ToString("N0", CultureInfo.CurrentCulture);
            var total = _totalRecordCount >= 0 && _totalRecordCount != Rows.Count
                ? $" of {_totalRecordCount.ToString("N0", CultureInfo.CurrentCulture)}{(_totalRecordCountLimitExceeded ? "+" : string.Empty)}"
                : string.Empty;
            StatusText = $"{count}{total} records{(HasMore ? ", more available" : string.Empty)}";
        }

        private void ViewEditorViewModel_QueryChanged(object sender, EventArgs e)
        {
            IsOutdated = true;
            if (AutoRefresh)
            {
                _refreshTimer.Stop();
                _refreshTimer.Start();
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            Run();
        }
    }
}
