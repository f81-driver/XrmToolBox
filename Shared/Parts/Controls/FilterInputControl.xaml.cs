using Formula81.XrmToolBox.Shared.Parts.Events;
using Formula81.XrmToolBox.Shared.Parts.Input;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Shared.Parts.Controls
{
    public partial class FilterInputControl : UserControl
    {
        public event FilterChangedEvent FilterChanged;

        public FilterInputControl()
        {
            InitializeComponent();

            ClearFilterButton.Command = new RelayCommand(ExecuteClearFilter, CanExecuteClearFilter);
        }

        internal bool CanExecuteClearFilter(object parameter)
        {
            return (FilterTextBox?.Text?.Length ?? 0) > 0;
        }

        internal void ExecuteClearFilter(object parameter)
        {
            FilterTextBox.Clear();
        }

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChanged?.Invoke(FilterTextBox.Text);
        }
    }
}
