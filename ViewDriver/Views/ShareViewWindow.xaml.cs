using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Views
{
    /// <summary>
    /// Who a view is shared with. Works on a copy: nothing is shared or unshared until the dialog is confirmed.
    /// </summary>
    public partial class ShareViewWindow : Window
    {
        private readonly ObservableCollection<ShareEntry> _entries;
        private readonly ICollectionView _principalsView;
        private string _search;

        /// <summary>The shares as they are when the dialog is confirmed</summary>
        public IReadOnlyList<ShareEntry> Entries => _entries;

        /// <summary>The users and teams the view was shared with and no longer is</summary>
        public IEnumerable<SharePrincipal> RemovedPrincipals { get; private set; } = Enumerable.Empty<SharePrincipal>();

        private readonly List<ShareEntry> _originalEntries;

        public ShareViewWindow(string viewName, string ownerName, IEnumerable<SharePrincipal> principals, IEnumerable<ShareEntry> entries)
        {
            InitializeComponent();

            Title = $"Share View: {viewName}";
            HintTextBlock.Text = $"Shared on behalf of the owner, {ownerName}. Read is needed for any other access; unchecking it removes the share.";
            _originalEntries = entries.ToList();
            _entries = new ObservableCollection<ShareEntry>(_originalEntries);
            _entries.CollectionChanged += (sender, e) => UpdateState();
            SharesDataGrid.ItemsSource = _entries;

            _principalsView = new CollectionViewSource { Source = principals.ToList() }.View;
            _principalsView.Filter = FilterPrincipal;
            _principalsView.SortDescriptions.Add(new SortDescription(nameof(SharePrincipal.Name), ListSortDirection.Ascending));
            PrincipalsListBox.ItemsSource = _principalsView;
            UpdateState();
        }

        private bool FilterPrincipal(object obj)
        {
            return obj is SharePrincipal sharePrincipal
                && !_entries.Any(se => se.Principal.Id == sharePrincipal.Id)
                && (string.IsNullOrEmpty(_search)
                    || sharePrincipal.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                    || (sharePrincipal.Detail?.IndexOf(_search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _search = SearchTextBox.Text?.Trim();
            _principalsView.Refresh();
        }

        private void PrincipalsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateState();
        }

        private void PrincipalsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement)?.DataContext is SharePrincipal sharePrincipal)
            {
                Add(new[] { sharePrincipal });
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Add(PrincipalsListBox.SelectedItems.OfType<SharePrincipal>().ToList());
        }

        private void Add(IEnumerable<SharePrincipal> sharePrincipals)
        {
            foreach (var sharePrincipal in sharePrincipals)
            {
                // A share removed and added again in this dialog keeps its original access
                var entry = _originalEntries.FirstOrDefault(se => se.Principal.Id == sharePrincipal.Id) ?? new ShareEntry(sharePrincipal, null);
                if (!entry.CanRead)
                {
                    entry.CanRead = true;
                }
                _entries.Add(entry);
            }
            _principalsView.Refresh();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is ShareEntry shareEntry)
            {
                _entries.Remove(shareEntry);
                _principalsView.Refresh();
            }
        }

        private void UpdateState()
        {
            AddButton.IsEnabled = PrincipalsListBox.SelectedItems.Count > 0;
            EmptyTextBlock.Visibility = _entries.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Commit a checkbox that still has focus
            SharesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
            // Removed: taken off the list, or Read unchecked on a share that had it. A share without Read to begin with (such as
            // append only, granted elsewhere) is kept as long as it is left alone.
            var keptIds = new HashSet<Guid>(_entries
                .Where(se => se.CanRead || (se.OriginalRights.HasValue && !se.OriginalRights.Value.HasFlag(AccessRights.ReadAccess)))
                .Select(se => se.Principal.Id));
            RemovedPrincipals = _originalEntries.Where(se => !keptIds.Contains(se.Principal.Id)).Select(se => se.Principal).ToList();
            DialogResult = true;
        }
    }
}
