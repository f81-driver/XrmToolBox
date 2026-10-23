using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Shared.XrmParts.Events;
using Formula81.XrmToolBox.Shared.XrmParts.Models;
using System;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Views
{
    public partial class ViewDriverView : UserControl
    {
        private readonly ViewDriverPluginControl _viewDriverPluginControl;

        public ViewDriverView(ViewDriverPluginControl viewDriverPluginControl)
        {
            _viewDriverPluginControl = viewDriverPluginControl;        

            InitializeComponent();

            CloseToolButton.Command = new RelayCommand(ExecuteCloseTool, CanExecuteCloseTool);
            RefreshButton.Command = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);

            AddButton.Command = new RelayCommand(ExecuteAdd, CanExecuteAdd);

            ViewDriverEntityListControl.EntityInvoked += ViewDriverEntityListControl_EntityClicked;
            ViewDriverEntityListControl.FavoriteEntitiesChanged += ViewDriverEntityListControl_FavoriteEntitiesChanged;
        }

        internal bool CanExecuteCloseTool(object parameter)
        {
            return !_viewDriverPluginControl.IsBusy;
        }

        internal bool CanExecuteRefresh(object parameter)
        {
            return !_viewDriverPluginControl.IsBusy
                && _viewDriverPluginControl.ConnectionDetail != null;
        }

        internal bool CanExecuteAdd(object parameter)
        {
            return !_viewDriverPluginControl.IsBusy;
        }

        internal void ExecuteCloseTool(object parameter)
        {
            _viewDriverPluginControl.CloseTool();
        }

        internal void ExecuteRefresh(object parameter)
        {
            try
            {
                //_viewDriverPluginControl.LoadAuditEntitiesAsync();
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Refresh");
            }
        }

        internal void ExecuteAdd(object parameter)
        {
        }

        private void ViewDriverEntityListControl_EntityClicked(object sender, EntityItem entityItem)
        {
        }

        private void ViewDriverEntityListControl_FavoriteEntitiesChanged(object sender, FavoriteEntitiesChangedArgs args)
        {
            _viewDriverPluginControl.Settings.FavoriteEntities = string.Join(",", args.Entities);
            _viewDriverPluginControl.SaveSettings();
        }
    }
}
