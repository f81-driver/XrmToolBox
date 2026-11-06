using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Views
{
    /// <summary>
    /// Shows the templates, to change, duplicate and delete the ones people made. Works on copies: nothing changes until OK.
    /// </summary>
    public partial class ViewTemplatesWindow : Window
    {
        private readonly ObservableCollection<ViewTemplate> _templates;
        /// <summary>The template made of the open view, the only one the left out note is about</summary>
        private readonly ViewTemplate _newTemplate;
        private bool _hasLeftOut;

        /// <summary>The templates people made, as they are when the dialog is confirmed</summary>
        public IEnumerable<ViewTemplate> UserTemplates => _templates.Where(vt => !vt.IsBuiltIn);

        private ViewTemplate SelectedTemplate => TemplatesListBox.SelectedItem as ViewTemplate;

        public ViewTemplatesWindow(IEnumerable<ViewTemplate> builtInTemplates, IEnumerable<ViewTemplate> userTemplates,
            ViewTemplate newTemplate, IEnumerable<string> leftOut)
        {
            InitializeComponent();

            _templates = new ObservableCollection<ViewTemplate>(builtInTemplates.Concat(userTemplates.Select(vt => vt.Clone())));
            if (newTemplate != null)
            {
                newTemplate.Name = GetUniqueName(string.IsNullOrWhiteSpace(newTemplate.Name) ? "My template" : newTemplate.Name);
                _templates.Add(newTemplate);
            }
            _newTemplate = newTemplate;
            var leftOutList = leftOut?.ToList() ?? new List<string>();
            _hasLeftOut = leftOutList.Any();
            LeftOutTextBlock.Text = "Left out of this template, as it would not work on every table:\n- " + string.Join("\n- ", leftOutList);

            TemplatesListBox.ItemsSource = _templates;
            TemplatesListBox.SelectedItem = newTemplate ?? _templates.FirstOrDefault();
            if (newTemplate != null)
            {
                Loaded += (sender, e) =>
                {
                    NameTextBox.Focus();
                    NameTextBox.SelectAll();
                };
            }
        }

        private void TemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowSelectedTemplate();
        }

        private void ShowSelectedTemplate()
        {
            var viewTemplate = SelectedTemplate;
            EditorGrid.DataContext = viewTemplate;
            EditorGrid.IsEnabled = viewTemplate != null && !viewTemplate.IsBuiltIn;
            BuiltInTextBlock.Visibility = viewTemplate?.IsBuiltIn ?? false ? Visibility.Visible : Visibility.Collapsed;
            LeftOutBorder.Visibility = _hasLeftOut && viewTemplate != null && viewTemplate == _newTemplate ? Visibility.Visible : Visibility.Collapsed;
            DuplicateButton.IsEnabled = viewTemplate != null;
            DeleteButton.IsEnabled = viewTemplate != null && !viewTemplate.IsBuiltIn;
            ShowColumns();
        }

        private void ShowColumns()
        {
            var viewTemplate = SelectedTemplate;
            ColumnsListBox.ItemsSource = null;
            ColumnsListBox.ItemsSource = viewTemplate?.Columns;
            var available = TemplateColumns.All
                .Where(c => viewTemplate == null || !viewTemplate.Columns.Any(vtc => string.Equals(vtc.Attribute, c.Attribute, StringComparison.OrdinalIgnoreCase)))
                .Select(c => new ViewTemplateColumn { Attribute = c.Attribute, Width = c.Width })
                .ToList();
            AvailableColumnsComboBox.ItemsSource = available;
            AvailableColumnsComboBox.SelectedIndex = available.Any() ? 0 : -1;
            SortTextBlock.Text = viewTemplate?.SortText;
            FilterTextBlock.Text = viewTemplate?.FilterText;
            ClearSortButton.IsEnabled = viewTemplate?.Orders.Any() ?? false;
            ClearFilterButton.IsEnabled = viewTemplate?.Conditions.Any() ?? false;
        }

        private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // The list shows the name; refreshed when done typing, so the list does not rebuild on every key
            TemplatesListBox.Items.Refresh();
        }

        private void DuplicateButton_Click(object sender, RoutedEventArgs e)
        {
            var viewTemplate = SelectedTemplate;
            if (viewTemplate == null)
            {
                return;
            }
            var copy = viewTemplate.Clone();
            copy.Name = GetUniqueName($"{viewTemplate.Name} (copy)");
            _templates.Add(copy);
            TemplatesListBox.SelectedItem = copy;
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var viewTemplate = SelectedTemplate;
            if (viewTemplate == null || viewTemplate.IsBuiltIn)
            {
                return;
            }
            var index = _templates.IndexOf(viewTemplate);
            _templates.Remove(viewTemplate);
            TemplatesListBox.SelectedIndex = Math.Min(index, _templates.Count - 1);
        }

        private void AddColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTemplate != null && AvailableColumnsComboBox.SelectedItem is ViewTemplateColumn viewTemplateColumn)
            {
                SelectedTemplate.Columns.Add(viewTemplateColumn);
                ShowColumns();
            }
        }

        private void RemoveColumnButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTemplate != null && (sender as FrameworkElement)?.DataContext is ViewTemplateColumn viewTemplateColumn)
            {
                SelectedTemplate.Columns.Remove(viewTemplateColumn);
                ShowColumns();
            }
        }

        private void MoveColumnUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveColumn(sender, -1);
        }

        private void MoveColumnDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveColumn(sender, 1);
        }

        private void MoveColumn(object sender, int offset)
        {
            var columns = SelectedTemplate?.Columns;
            if (columns == null || !((sender as FrameworkElement)?.DataContext is ViewTemplateColumn viewTemplateColumn))
            {
                return;
            }
            var index = columns.IndexOf(viewTemplateColumn);
            var newIndex = index + offset;
            if (index < 0 || newIndex < 0 || newIndex >= columns.Count)
            {
                return;
            }
            columns.RemoveAt(index);
            columns.Insert(newIndex, viewTemplateColumn);
            ShowColumns();
        }

        private void ClearSortButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTemplate?.Orders.Clear();
            ShowColumns();
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedTemplate != null)
            {
                SelectedTemplate.Conditions.Clear();
                SelectedTemplate.IsMatchAny = false;
            }
            ShowColumns();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var unnamed = UserTemplates.FirstOrDefault(vt => string.IsNullOrWhiteSpace(vt.Name));
            if (unnamed != null)
            {
                TemplatesListBox.SelectedItem = unnamed;
                MessageBox.Show(this, "Every template needs a name.", "View Templates", MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }
            foreach (var viewTemplate in UserTemplates)
            {
                viewTemplate.Name = viewTemplate.Name.Trim();
                foreach (var viewTemplateColumn in viewTemplate.Columns)
                {
                    viewTemplateColumn.Width = Math.Max(ViewColumn.MinWidth, Math.Min(ViewColumn.MaxWidth, viewTemplateColumn.Width));
                }
            }
            DialogResult = true;
        }

        private string GetUniqueName(string name)
        {
            var names = new HashSet<string>(_templates.Select(vt => vt.Name), StringComparer.OrdinalIgnoreCase);
            var uniqueName = name;
            for (var index = 2; names.Contains(uniqueName); index++)
            {
                uniqueName = $"{name} {index}";
            }
            return uniqueName;
        }
    }
}
