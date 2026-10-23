using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using System.Collections.ObjectModel;

namespace Formula81.XrmToolBox.Tools.ViewDriver.ViewModels
{
    internal class ViewViewModel
    {
        public ObservableCollection<Column> ColumnCollection { get; set; }

        public ViewViewModel()
        {
            ColumnCollection = new ObservableCollection<Column>();
        }
    }
}
