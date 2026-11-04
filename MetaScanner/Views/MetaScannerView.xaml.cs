using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.MetaScanner.ViewModels;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Views
{
    public partial class MetaScannerView : UserControl
    {
        private readonly MetaScannerPluginControl _metaScannerPluginControl;

        public EntityListViewModel EntityListViewModel { get; }
        public EntityDetailViewModel EntityDetailViewModel { get; }
        public ScanViewModel ScanViewModel { get; }

        public MetaScannerView(MetaScannerPluginControl metaScannerPluginControl)
        {
            _metaScannerPluginControl = metaScannerPluginControl;

            EntityListViewModel = new EntityListViewModel(_metaScannerPluginControl);
            EntityDetailViewModel = new EntityDetailViewModel(_metaScannerPluginControl);
            ScanViewModel = new ScanViewModel(_metaScannerPluginControl);

            EntityListViewModel.FilterReset += EntityListViewModel_FilterReset;
            EntityListViewModel.PropertyChanged += EntityListViewModel_PropertyChanged;
            EntityDetailViewModel.AttributeFiltersReset += EntityDetailViewModel_AttributeFiltersReset;
            EntityDetailViewModel.PropertyChanged += EntityDetailViewModel_PropertyChanged;

            InitializeComponent();

            CloseToolButton.Command = new RelayCommand(ExecuteCloseTool, CanExecuteCloseTool);
            RefreshButton.Command = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
            ScanButton.Command = ScanViewModel.ScanCommand;
        }

        internal bool CanExecuteCloseTool(object parameter)
        {
            return !_metaScannerPluginControl.IsBusy;
        }

        internal bool CanExecuteRefresh(object parameter)
        {
            return !_metaScannerPluginControl.IsBusy
                && _metaScannerPluginControl.ConnectionDetail != null;
        }

        internal void ExecuteCloseTool(object parameter)
        {
            _metaScannerPluginControl.CloseTool();
        }

        internal void ExecuteRefresh(object parameter)
        {
            try
            {
                _metaScannerPluginControl.Reload();
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Refresh");
            }
        }

        private void EntityFilterInput_FilterChanged(string filter)
        {
            EntityListViewModel.SetFilter(filter);
        }

        private void AttributeFilterInput_FilterChanged(string filter)
        {
            EntityDetailViewModel.SetAttributeFilter(filter);
        }

        private void RelationshipFilterInput_FilterChanged(string filter)
        {
            EntityDetailViewModel.SetRelationshipFilter(filter);
        }

        private void PropertyFilterInput_FilterChanged(string filter)
        {
            EntityDetailViewModel.SetPropertyFilter(filter);
        }

        private void EntityListViewModel_FilterReset(object sender, EventArgs e)
        {
            EntityFilterInput.Clear();
        }

        private void EntityDetailViewModel_AttributeFiltersReset(object sender, EventArgs e)
        {
            AttributeFilterInput.Clear();
        }

        private void EntityListViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EntityListViewModel.SelectedEntity)
                && EntityListViewModel.SelectedEntity != null)
            {
                ScrollIntoView(EntityListBox, EntityListViewModel.SelectedEntity);
            }
        }

        private void EntityDetailViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EntityDetailViewModel.SelectedAttribute)
                && EntityDetailViewModel.SelectedAttribute != null)
            {
                ScrollIntoView(AttributesDataGrid, EntityDetailViewModel.SelectedAttribute);
            }
        }

        private void RelationshipRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow dataGridRow
                && EntityDetailViewModel.GoToEntityCommand.CanExecute(dataGridRow.Item))
            {
                EntityDetailViewModel.GoToEntityCommand.Execute(dataGridRow.Item);
                e.Handled = true;
            }
        }

        private void ScanResultRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow dataGridRow
                && ScanViewModel.OpenResultCommand.CanExecute(dataGridRow.Item))
            {
                ScanViewModel.OpenResultCommand.Execute(dataGridRow.Item);
                e.Handled = true;
            }
        }

        private void ScanResultsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // The DataGrid moves to the next row on Enter, open the selected result instead
            if (e.Key == Key.Return
                && ScanViewModel.OpenResultCommand.CanExecute(ScanResultsDataGrid.SelectedItem))
            {
                ScanViewModel.OpenResultCommand.Execute(ScanResultsDataGrid.SelectedItem);
                e.Handled = true;
            }
        }

        private void ScrollIntoView(ListBox listBox, object item)
        {
            // Wait for the items to be (re)generated after a filter or source change
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => listBox.ScrollIntoView(item)));
        }

        private void ScrollIntoView(DataGrid dataGrid, object item)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => dataGrid.ScrollIntoView(item)));
        }
    }
}
