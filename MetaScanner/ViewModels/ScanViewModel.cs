using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.MetaScanner.Helpers;
using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.MetaScanner.ViewModels
{
    public class ScanViewModel : ObservableObject
    {
        public const int ScanTextMinLength = 2;
        public const string AllKinds = "All results";

        public static IEnumerable<string> Kinds { get; } = new[] { AllKinds, ScanResult.TableKind, ScanResult.ColumnKind, ScanResult.ChoiceKind };

        private readonly MetaScannerPluginControl _metaScannerPluginControl;

        private string _scanText;
        public string ScanText { get => _scanText; set => SetValue(nameof(ScanText), value, ref _scanText); }

        private string _scannedText;
        public string ScannedText { get => _scannedText; private set => SetValue(nameof(ScannedText), value, ref _scannedText); }

        private bool _isActive;
        public bool IsActive { get => _isActive; set => SetValue(nameof(IsActive), value, ref _isActive); }

        private bool _hasResults;
        public bool HasResults { get => _hasResults; private set => SetValue(nameof(HasResults), value, ref _hasResults); }

        private ICollectionView _resultsViewSource;
        public ICollectionView ResultsViewSource { get => _resultsViewSource; private set => SetValue(nameof(ResultsViewSource), value, ref _resultsViewSource); }

        private string _selectedKind = AllKinds;
        public string SelectedKind { get => _selectedKind; set { SetValue(nameof(SelectedKind), value, ref _selectedKind); Refresh(); } }

        private string _statusText;
        public string StatusText { get => _statusText; private set => SetValue(nameof(StatusText), value, ref _statusText); }

        public ICommand ScanCommand { get; }
        public ICommand OpenResultCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CloseCommand { get; }

        private IList<ScanResult> _results = new List<ScanResult>();
        private bool _limitExceeded;

        public ScanViewModel(MetaScannerPluginControl metaScannerPluginControl)
        {
            _metaScannerPluginControl = metaScannerPluginControl;

            ScanCommand = new RelayCommand(ExecuteScan, CanExecuteScan);
            OpenResultCommand = new RelayCommand(ExecuteOpenResult, CanExecuteOpenResult);
            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        internal bool CanExecuteScan(object parameter)
        {
            return !_metaScannerPluginControl.IsBusy
                && _metaScannerPluginControl.ConnectionDetail != null
                && (_scanText?.Trim().Length ?? 0) >= ScanTextMinLength;
        }

        internal bool CanExecuteOpenResult(object parameter)
        {
            return !_metaScannerPluginControl.IsBusy
                && parameter is ScanResult;
        }

        internal bool CanExecuteCopy(object parameter)
        {
            return !string.IsNullOrEmpty(parameter?.ToString());
        }

        internal bool CanExecuteExport(object parameter)
        {
            return !_metaScannerPluginControl.IsBusy
                && (_resultsViewSource?.Cast<object>().Any() ?? false);
        }

        internal void ExecuteScan(object parameter)
        {
            try
            {
                _metaScannerPluginControl.ScanAsync(_scanText.Trim());
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Scan");
            }
        }

        internal void ExecuteOpenResult(object parameter)
        {
            try
            {
                if (parameter is ScanResult scanResult)
                {
                    IsActive = false;
                    _metaScannerPluginControl.GoToScanResult(scanResult);
                }
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Open Scan Result");
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
                _metaScannerPluginControl.ShowErrorDialog(exception, "Copy");
            }
        }

        internal void ExecuteExport(object parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Scan Results",
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"MetaScanner_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (saveFileDialog.ShowDialog() ?? false)
                {
                    CsvExportHelper.ExportScanResults(saveFileDialog.FileName, _resultsViewSource.Cast<ScanResult>().ToList());
                }
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Export Scan Results");
            }
        }

        internal void ExecuteClose(object parameter)
        {
            IsActive = false;
        }

        internal void SetResults(string scannedText, IList<ScanResult> results, bool limitExceeded)
        {
            ScannedText = scannedText;
            _results = results ?? new List<ScanResult>();
            _limitExceeded = limitExceeded;
            var resultsViewSource = new CollectionViewSource { Source = _results }.View;
            resultsViewSource.Filter = FilterResult;
            ResultsViewSource = resultsViewSource;
            HasResults = _results.Count > 0;
            IsActive = true;
            UpdateStatusText();
        }

        internal void Clear()
        {
            IsActive = false;
            ScannedText = null;
            _results = new List<ScanResult>();
            _limitExceeded = false;
            ResultsViewSource = null;
            HasResults = false;
            StatusText = null;
        }

        private void Refresh()
        {
            ResultsViewSource?.Refresh();
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            var visibleCount = _results.Count(IsVisible);
            var status = visibleCount == _results.Count ? $"{_results.Count} results" : $"{visibleCount} of {_results.Count} results";
            StatusText = _limitExceeded ? $"{status}, only the first {ScanHelper.ScanResultsMax} are shown, refine the search to see more" : status;
        }

        private bool FilterResult(object obj)
        {
            return obj is ScanResult scanResult && IsVisible(scanResult);
        }

        private bool IsVisible(ScanResult scanResult)
        {
            return _selectedKind == AllKinds || scanResult.Kind == _selectedKind;
        }
    }
}
