using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Formula81.XrmToolBox.Tools.ViewDriver.ViewModels
{
    public class ViewListViewModel : ObservableObject
    {
        private readonly ViewDriverPluginControl _viewDriverPluginControl;

        private readonly ObservableCollection<ViewItem> _viewCollection;
        public ICollectionView ViewsViewSource { get; }

        private IReadOnlyList<ViewOwner> _owners = new ViewOwner[0];
        public IReadOnlyList<ViewOwner> Owners { get => _owners; private set => SetValue(nameof(Owners), value, ref _owners); }

        private ViewOwner _selectedOwner;
        public ViewOwner SelectedOwner
        {
            get => _selectedOwner;
            set
            {
                // An editable combo box sets null while text is typed that matches no user
                if (value == null || value == _selectedOwner)
                {
                    return;
                }
                if (!_viewDriverPluginControl.ConfirmDiscardChanges())
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => NotifyPropertyChanged(nameof(SelectedOwner))), DispatcherPriority.Background);
                    return;
                }
                SetValue(nameof(SelectedOwner), value, ref _selectedOwner);
                NotifyPropertyChanged(nameof(IsOtherOwner));
                Clear();
                _viewDriverPluginControl.OpenView(null);
                _viewDriverPluginControl.LoadViewsAsync(null);
            }
        }

        public bool IsOtherOwner => !(_selectedOwner?.IsCurrentUser ?? true);

        private ViewItem _selectedView;
        public ViewItem SelectedView
        {
            get => _selectedView;
            set
            {
                if (value == _selectedView)
                {
                    return;
                }
                if (!_viewDriverPluginControl.ConfirmDiscardChanges())
                {
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => NotifyPropertyChanged(nameof(SelectedView))), DispatcherPriority.Background);
                    return;
                }
                // Discarding a new view removes it, which may have selected another view already
                if (value != null && !_viewCollection.Contains(value))
                {
                    value = null;
                }
                SetValue(nameof(SelectedView), value, ref _selectedView);
                _viewDriverPluginControl.OpenView(value);
            }
        }

        public bool ShowSystemViews
        {
            get => _viewDriverPluginControl.Settings.ShowSystemViews;
            set
            {
                _viewDriverPluginControl.Settings.ShowSystemViews = value;
                _viewDriverPluginControl.SaveSettings();
                NotifyPropertyChanged(nameof(ShowSystemViews));
                Refresh();
            }
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetValue(nameof(IsLoading), value, ref _isLoading); }

        private bool _hasEntity;
        public bool HasEntity { get => _hasEntity; private set => SetValue(nameof(HasEntity), value, ref _hasEntity); }

        private string _countText;
        public string CountText { get => _countText; private set => SetValue(nameof(CountText), value, ref _countText); }

        public ICommand NewViewCommand { get; }
        public ICommand CloneViewCommand { get; }
        public ICommand ShareViewCommand { get; }
        public ICommand DeleteViewCommand { get; }
        public ICommand OpenInBrowserCommand { get; }
        public ICommand CopyCommand { get; }

        private string _filter;

        public ViewListViewModel(ViewDriverPluginControl viewDriverPluginControl)
        {
            _viewDriverPluginControl = viewDriverPluginControl;

            _viewCollection = new ObservableCollection<ViewItem>();
            ViewsViewSource = new CollectionViewSource { Source = _viewCollection }.View;
            ViewsViewSource.Filter = FilterView;
            ViewsViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ViewItem.GroupName)));
            ViewsViewSource.SortDescriptions.Add(new SortDescription(nameof(ViewItem.Kind), ListSortDirection.Ascending));
            ViewsViewSource.SortDescriptions.Add(new SortDescription(nameof(ViewItem.Name), ListSortDirection.Ascending));

            NewViewCommand = new RelayCommand(ExecuteNewView, CanExecuteNewView);
            CloneViewCommand = new RelayCommand(ExecuteCloneView, CanExecuteCloneView);
            ShareViewCommand = new RelayCommand(ExecuteShareView, CanExecuteShareView);
            DeleteViewCommand = new RelayCommand(ExecuteDeleteView, CanExecuteDeleteView);
            OpenInBrowserCommand = new RelayCommand(ExecuteOpenInBrowser, CanExecuteOpenInBrowser);
            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
        }

        // Context menus pass their view, toolbar buttons pass nothing and act on the selected view
        private ViewItem GetViewItem(object parameter)
        {
            return parameter as ViewItem ?? _selectedView;
        }

        internal bool CanExecuteNewView(object parameter)
        {
            return !_viewDriverPluginControl.IsBusy
                && HasEntity
                && _selectedOwner != null;
        }

        internal bool CanExecuteCloneView(object parameter)
        {
            return !_viewDriverPluginControl.IsBusy
                && GetViewItem(parameter) != null
                && _selectedOwner != null;
        }

        internal bool CanExecuteShareView(object parameter)
        {
            // Only saved personal views can be shared; system views are visible to everyone with access to the table
            var viewItem = GetViewItem(parameter);
            return !_viewDriverPluginControl.IsBusy
                && viewItem != null
                && viewItem.IsPersonal
                && !viewItem.IsNew
                && _selectedOwner != null;
        }

        internal void ExecuteShareView(object parameter)
        {
            try
            {
                _viewDriverPluginControl.ShareViewAsync(GetViewItem(parameter));
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Share View");
            }
        }

        internal bool CanExecuteDeleteView(object parameter)
        {
            var viewItem = GetViewItem(parameter);
            return !_viewDriverPluginControl.IsBusy
                && viewItem != null
                && viewItem.IsPersonal;
        }

        internal bool CanExecuteOpenInBrowser(object parameter)
        {
            var viewItem = GetViewItem(parameter);
            return viewItem != null
                && !viewItem.IsNew
                && _viewDriverPluginControl.CanOpenInBrowser();
        }

        internal bool CanExecuteCopy(object parameter)
        {
            return !string.IsNullOrEmpty(parameter?.ToString());
        }

        internal void ExecuteNewView(object parameter)
        {
            try
            {
                if (!_viewDriverPluginControl.ConfirmDiscardChanges())
                {
                    return;
                }
                // The New menu passes the template to start from
                var viewItem = _viewDriverPluginControl.CreateNewView(parameter as ViewTemplate);
                if (viewItem != null)
                {
                    _viewCollection.Add(viewItem);
                    UpdateCountText();
                    SelectedView = viewItem;
                }
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "New View");
            }
        }

        internal void ExecuteCloneView(object parameter)
        {
            try
            {
                _viewDriverPluginControl.CloneViewAsync(GetViewItem(parameter));
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Clone View");
            }
        }

        internal void ExecuteDeleteView(object parameter)
        {
            try
            {
                _viewDriverPluginControl.DeleteViewAsync(GetViewItem(parameter));
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Delete View");
            }
        }

        internal void ExecuteOpenInBrowser(object parameter)
        {
            try
            {
                _viewDriverPluginControl.OpenViewInBrowser(GetViewItem(parameter));
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Open in Browser");
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
                _viewDriverPluginControl.ShowErrorDialog(exception, "Copy");
            }
        }

        internal void SetOwners(IEnumerable<ViewOwner> owners)
        {
            Owners = owners.ToList();
            // Starts with the views of the current user, without asking to discard: this happens on (re)connect
            _selectedOwner = Owners.FirstOrDefault(vo => vo.IsCurrentUser) ?? Owners.FirstOrDefault();
            NotifyPropertyChanged(nameof(SelectedOwner));
            NotifyPropertyChanged(nameof(IsOtherOwner));
        }

        internal void SetViews(IEnumerable<ViewItem> viewItems, Guid? selectViewId)
        {
            _viewCollection.Clear();
            foreach (var viewItem in viewItems)
            {
                _viewCollection.Add(viewItem);
            }
            HasEntity = true;
            UpdateCountText();
            var viewItemToSelect = selectViewId.HasValue ? _viewCollection.FirstOrDefault(vi => vi.Id == selectViewId) : null;
            if (viewItemToSelect != null)
            {
                if (viewItemToSelect.Kind == ViewKind.System && !ShowSystemViews)
                {
                    ShowSystemViews = true;
                }
                _selectedView = viewItemToSelect;
                NotifyPropertyChanged(nameof(SelectedView));
                _viewDriverPluginControl.OpenView(viewItemToSelect);
            }
        }

        internal void RemoveView(ViewItem viewItem)
        {
            if (_selectedView == viewItem)
            {
                _selectedView = null;
                NotifyPropertyChanged(nameof(SelectedView));
            }
            _viewCollection.Remove(viewItem);
            UpdateCountText();
        }

        internal void Clear()
        {
            _selectedView = null;
            NotifyPropertyChanged(nameof(SelectedView));
            _viewCollection.Clear();
            HasEntity = false;
            IsLoading = false;
            UpdateCountText();
        }

        internal void SetFilter(string filter)
        {
            _filter = filter;
            Refresh();
        }

        internal void NotifySettingsChanged()
        {
            NotifyPropertyChanged(nameof(ShowSystemViews));
            Refresh();
        }

        internal void Refresh()
        {
            ViewsViewSource.Refresh();
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            var personalCount = _viewCollection.Count(vi => vi.IsPersonal);
            var systemCount = _viewCollection.Count - personalCount;
            CountText = HasEntity ? $"{personalCount} personal, {systemCount} system views" : null;
        }

        private bool FilterView(object obj)
        {
            return obj is ViewItem viewItem
                && (viewItem.IsPersonal || ShowSystemViews)
                && (string.IsNullOrEmpty(_filter)
                    // The open view stays visible while its name is being edited
                    || viewItem == _selectedView
                    || (viewItem.Name?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                    || (viewItem.Description?.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }
    }
}
