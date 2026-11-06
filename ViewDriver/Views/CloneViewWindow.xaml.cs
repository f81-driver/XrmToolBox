using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Views
{
    public partial class CloneViewWindow : Window
    {
        public string ViewName => NameTextBox.Text?.Trim();
        public ViewOwner ViewOwner => OwnerComboBox.SelectedItem as ViewOwner;

        public CloneViewWindow(IEnumerable<ViewOwner> viewOwners, string viewName, ViewOwner viewOwner)
        {
            InitializeComponent();

            OwnerComboBox.ItemsSource = viewOwners;
            OwnerComboBox.SelectedItem = viewOwner;
            NameTextBox.Text = viewName;
            NameTextBox.SelectAll();
            UpdateSubmitButton();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSubmitButton();
        }

        private void OwnerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSubmitButton();
        }

        private void UpdateSubmitButton()
        {
            if (SubmitButton != null)
            {
                SubmitButton.IsEnabled = !string.IsNullOrEmpty(ViewName) && ViewOwner != null;
            }
        }
    }
}
