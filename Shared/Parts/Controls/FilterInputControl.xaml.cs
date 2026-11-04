using Formula81.XrmToolBox.Shared.Parts.Events;
using Formula81.XrmToolBox.Shared.Parts.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Shared.Parts.Controls
{
    public partial class FilterInputControl : UserControl
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(FilterInputControl), new PropertyMetadata("Filter..."));

        public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

        public event FilterChangedEvent FilterChanged;

        public FilterInputControl()
        {
            InitializeComponent();

            ClearFilterButton.Command = new RelayCommand(ExecuteClearFilter, CanExecuteClearFilter);
        }

        public void Clear()
        {
            FilterTextBox.Clear();
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

        private void FilterTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && FilterTextBox.Text.Length > 0)
            {
                FilterTextBox.Clear();
                e.Handled = true;
            }
        }
    }
}
