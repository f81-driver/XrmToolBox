using Formula81.XrmToolBox.Shared.Parts.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Components;
using Formula81.XrmToolBox.Tools.AuditGoggles.Events;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.ViewModels
{
    public class AuditRecordViewModel
    {
        private readonly AuditGogglesPluginControl _auditGogglesPluginControl;
        private readonly List<ColorCombination> _usedColorCombinationList = new List<ColorCombination>();
        private readonly ObservableCollection<AuditRecord> _auditRecordCollection;
        private readonly HashSet<Guid> _auditRecordIdSet;

        private int AuditRecordCount { get => _auditRecordCollection.Count; }

        public IEnumerable<AuditRecord> AuditRecords { get => _auditRecordCollection; }
        public bool IsEmpty { get => (_auditRecordCollection?.Count ?? 0) > 0; }

        public ICommand AddCommand { get; }
        public ICommand FxbCommand { get; }
        public ICommand ChangeColorCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand RemoveCommand { get; }

        public event AuditRecordColorChangedEvent ColorChanged;

        public AuditRecordViewModel(AuditGogglesPluginControl auditGogglesPluginControl)
        {
            _auditGogglesPluginControl = auditGogglesPluginControl;

            _auditRecordIdSet = new HashSet<Guid>();
            _auditRecordCollection = new ObservableCollection<AuditRecord>();
            _auditRecordCollection.CollectionChanged += AuditRecordCollection_CollectionChanged;

            AddCommand = new RelayCommand(ExecuteAdd, CanExecuteAdd);
            FxbCommand = new RelayCommand(ExecuteFxb, CanExecuteFxb);
            ClearCommand = new RelayCommand(ExecuteClear, CanExecuteClear);
            ChangeColorCommand = new RelayCommand(ExecuteChangeColor, CanExecuteChangeColor);
            RemoveCommand = new RelayCommand(ExecuteRemove, CanExecuteRemove);
        }

        internal bool CanExecuteAdd(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy
                && AuditRecordCount < AuditGogglesPluginControl.AuditRecordsMax;
        }

        internal bool CanExecuteFxb(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy
                && AuditRecordCount < AuditGogglesPluginControl.AuditRecordsMax;
        }

        internal bool CanExecuteClear(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy
                && AuditRecordCount > 0;
        }

        internal bool CanExecuteChangeColor(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy;
        }

        internal bool CanExecuteRemove(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy;
        }

        internal void ExecuteAdd(object parameter)
        {
            try
            {
                var logicalName = parameter as string;
                var inputRefs = _auditGogglesPluginControl.ShowAuditRecordInputDialog(logicalName);
                if (inputRefs?.Any() ?? false)
                {
                    var entityRefs = inputRefs.Where(i => !_auditRecordIdSet.Contains(i.Id));
                    if (entityRefs.Any())
                    {
                        _auditGogglesPluginControl.LoadAuditRecordsAsync((service) => entityRefs);
                    }
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Add Audit Record");
            }
        }

        internal void ExecuteFxb(object parameter)
        {
            try
            {
                _auditGogglesPluginControl.CallFxbPlugin();
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Add Audit Record");
            }
        }

        internal void ExecuteClear(object parameter)
        {
            try
            {
                if (MessageBox.Show("Clear all Audit Records?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Clear();
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Clear Audit Records");
            }
        }

        internal void ExecuteChangeColor(object parameter)
        {
            try
            {
                if (parameter is AuditRecord auditRecord)
                {
                    var colorCombination = GetRandomColorCombination();
                    auditRecord.ColorCombination = colorCombination;
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Change Audit Record Color");
            }
        }

        internal void ExecuteRemove(object parameter)
        {
            try
            {
                if (parameter is AuditRecord auditRecord)
                {
                    _auditRecordCollection.Remove(auditRecord);
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Remove Audit Record");
            }
        }

        internal void ExecuteClearAuditRecords(object parameter)
        {
            try
            {
                _auditRecordCollection.Clear();
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Clear Audit Entities");
            }
        }

        public void Add(AuditRecord auditRecord)
        {
            auditRecord.ColorCombination = GetRandomColorCombination();
            _auditRecordCollection.Add(auditRecord);
        }

        public void Clear()
        {
            _auditRecordCollection.Clear();
        }

        public bool ContainsId(Guid id)
        {
            return _auditRecordIdSet?.Contains(id) ?? false;
        }

        private ColorCombination GetRandomColorCombination()
        {
            var colorCombination = ColorCombinations.GetRandom(_usedColorCombinationList, out var allUsed);
            if (allUsed)
            {
                _usedColorCombinationList.Clear();
            }
            _usedColorCombinationList.Add(colorCombination);
            return colorCombination;
        }

        private void AuditRecordCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _auditRecordIdSet.Clear();
            }
            else
            {
                if ((e.OldItems?.Count ?? 0) > 0)
                {
                    foreach (var oldItem in e.OldItems.OfType<AuditRecord>())
                    {
                        oldItem.PropertyChanged -= AuditRecord_PropertyChanged;
                        _auditRecordIdSet.Remove(oldItem.Id);
                    }
                }
                if ((e.NewItems?.Count ?? 0) > 0)
                {
                    foreach (var newItem in e.NewItems?.OfType<AuditRecord>())
                    {
                        newItem.PropertyChanged += AuditRecord_PropertyChanged;
                        _auditRecordIdSet.Add(newItem.Id);
                    }
                }
            }
        }

        private void AuditRecord_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is AuditRecord auditRecord)
            {
                switch (e.PropertyName)
                {
                    case nameof(AuditRecord.ColorCombination):
                        ColorChanged?.Invoke(auditRecord);
                        break;
                }
            }
        }
    }
}
