using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.ViewDriver.Controls;
using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using Formula81.XrmToolBox.Tools.ViewDriver.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Views
{
    public partial class ViewDriverView : UserControl
    {
        private static readonly DependencyPropertyDescriptor ColumnWidthDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));

        private readonly ViewDriverPluginControl _viewDriverPluginControl;

        // Cell name of each preview grid column
        private readonly Dictionary<DataGridColumn, string> _previewColumnNames = new Dictionary<DataGridColumn, string>();
        private bool _isBuildingPreviewColumns;

        private Point _dragStartPoint;
        private object _dragItem;
        private InsertionAdorner _insertionAdorner;

        public EntityListViewModel EntityListViewModel { get; }
        public ViewListViewModel ViewListViewModel { get; }
        public ViewEditorViewModel ViewEditorViewModel { get; }
        public PreviewViewModel PreviewViewModel { get; }

        public ViewDriverView(ViewDriverPluginControl viewDriverPluginControl)
        {
            _viewDriverPluginControl = viewDriverPluginControl;

            EntityListViewModel = new EntityListViewModel(_viewDriverPluginControl);
            ViewListViewModel = new ViewListViewModel(_viewDriverPluginControl);
            ViewEditorViewModel = new ViewEditorViewModel(_viewDriverPluginControl);
            PreviewViewModel = new PreviewViewModel(_viewDriverPluginControl, ViewEditorViewModel);

            EntityListViewModel.FilterReset += EntityListViewModel_FilterReset;
            EntityListViewModel.PropertyChanged += EntityListViewModel_PropertyChanged;
            ViewEditorViewModel.LayoutChanged += ViewEditorViewModel_LayoutChanged;
            PreviewViewModel.ColumnsReset += PreviewViewModel_ColumnsReset;

            InitializeComponent();

            CloseToolButton.Command = new RelayCommand(ExecuteCloseTool, CanExecuteCloseTool);
            RefreshButton.Command = new RelayCommand(ExecuteRefresh, CanExecuteRefresh);
            NewViewButton.Command = new RelayCommand(ExecuteOpenNewMenu, CanExecuteOpenNewMenu);

            InputBindings.Add(new KeyBinding(ViewEditorViewModel.SaveCommand, Key.S, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(PreviewViewModel.RunCommand, Key.F5, ModifierKeys.None));
            FetchXmlTextBox.InputBindings.Add(new KeyBinding(ViewEditorViewModel.ApplyFetchXmlCommand, Key.Enter, ModifierKeys.Control));
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

        internal bool CanExecuteOpenNewMenu(object parameter)
        {
            // Managing templates needs no table, so the menu opens whenever the tool is not busy
            return !_viewDriverPluginControl.IsBusy;
        }

        /// <summary>
        /// The New menu: a new view from each template, then saving the open view as a template and managing the templates
        /// </summary>
        internal void ExecuteOpenNewMenu(object parameter)
        {
            var contextMenu = new ContextMenu
            {
                PlacementTarget = NewViewButton,
                Placement = PlacementMode.Bottom
            };
            contextMenu.Items.Add(new MenuItem { Header = "New view from template", IsEnabled = false, FontWeight = FontWeights.Bold });
            foreach (var viewTemplate in _viewDriverPluginControl.GetTemplates())
            {
                contextMenu.Items.Add(new MenuItem
                {
                    Header = viewTemplate.Name,
                    ToolTip = viewTemplate.ToolTip,
                    Command = ViewListViewModel.NewViewCommand,
                    CommandParameter = viewTemplate
                });
            }
            contextMenu.Items.Add(new Separator());
            var saveAsTemplateMenuItem = new MenuItem
            {
                Header = "Save open view as template...",
                ToolTip = "Make a template of the open view, with its unsaved changes",
                IsEnabled = ViewEditorViewModel.HasView
            };
            saveAsTemplateMenuItem.Click += (sender, e) => _viewDriverPluginControl.SaveViewAsTemplate();
            contextMenu.Items.Add(saveAsTemplateMenuItem);
            var manageTemplatesMenuItem = new MenuItem { Header = "Manage templates..." };
            manageTemplatesMenuItem.Click += (sender, e) => _viewDriverPluginControl.ShowTemplatesDialog(null, null);
            contextMenu.Items.Add(manageTemplatesMenuItem);
            contextMenu.IsOpen = true;
        }

        internal void ExecuteCloseTool(object parameter)
        {
            _viewDriverPluginControl.CloseTool();
        }

        internal void ExecuteRefresh(object parameter)
        {
            try
            {
                _viewDriverPluginControl.Reload();
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Refresh");
            }
        }

        #region Filters

        private void EntityFilterInput_FilterChanged(string filter)
        {
            EntityListViewModel.SetFilter(filter);
        }

        private void ViewFilterInput_FilterChanged(string filter)
        {
            ViewListViewModel.SetFilter(filter);
        }

        private void AvailableFilterInput_FilterChanged(string filter)
        {
            ViewEditorViewModel.SetAvailableFilter(filter);
        }

        private void EntityListViewModel_FilterReset(object sender, EventArgs e)
        {
            EntityFilterInput.Clear();
        }

        private void EntityListViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EntityListViewModel.SelectedEntity)
                && EntityListViewModel.SelectedEntity != null)
            {
                // Wait for the items to be (re)generated after a filter or source change
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() => EntityListBox.ScrollIntoView(EntityListViewModel.SelectedEntity)));
            }
        }

        #endregion

        #region Owner

        // Typing in the owner box searches; the owner only switches on Enter, on picking from the list or on leaving the box,
        // so the views are not reloaded for every user passed while typing
        private void OwnerComboBox_DropDownClosed(object sender, EventArgs e)
        {
            CommitOwner();
        }

        private void OwnerComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                CommitOwner();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                OwnerComboBox.SetCurrentValue(Selector.SelectedItemProperty, ViewListViewModel.SelectedOwner);
            }
        }

        private void OwnerComboBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!OwnerComboBox.IsDropDownOpen && !OwnerComboBox.IsKeyboardFocusWithin)
            {
                CommitOwner();
            }
        }

        private void CommitOwner()
        {
            if (OwnerComboBox.SelectedItem is ViewOwner viewOwner && viewOwner != ViewListViewModel.SelectedOwner)
            {
                ViewListViewModel.SelectedOwner = viewOwner;
            }
            // Shows the owner that is in effect: the typed one may match nobody, or switching was cancelled
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => OwnerComboBox.SetCurrentValue(Selector.SelectedItemProperty, ViewListViewModel.SelectedOwner)));
        }

        #endregion

        #region Drag and drop

        private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragItem = null;
            // The width box and the remove button of a column must keep working
            if (FindAncestor<TextBoxBase>(e.OriginalSource) != null
                || FindAncestor<ButtonBase>(e.OriginalSource) != null
                || FindAncestor<ScrollBar>(e.OriginalSource) != null)
            {
                return;
            }
            var listBoxItem = FindAncestor<ListBoxItem>(e.OriginalSource);
            if (listBoxItem != null)
            {
                _dragStartPoint = e.GetPosition(null);
                _dragItem = listBoxItem.DataContext;
            }
        }

        private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragItem == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            var offset = _dragStartPoint - e.GetPosition(null);
            if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }
            var dragItem = _dragItem;
            _dragItem = null;
            try
            {
                DragDrop.DoDragDrop((DependencyObject)sender, new DataObject(dragItem.GetType(), dragItem),
                    dragItem is ViewColumn ? DragDropEffects.Move : DragDropEffects.Copy);
            }
            finally
            {
                RemoveInsertionAdorner();
            }
        }

        private void ViewColumnsListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetDataPresent(typeof(ViewColumn)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else if (e.Data.GetDataPresent(typeof(ViewAttribute)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                RemoveInsertionAdorner();
                return;
            }
            GetColumnDropIndex(e, out var targetItem, out var isAfter);
            ShowInsertionAdorner(targetItem, isAfter);
        }

        private void ViewColumnsListBox_DragLeave(object sender, DragEventArgs e)
        {
            RemoveInsertionAdorner();
        }

        private void ViewColumnsListBox_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            RemoveInsertionAdorner();
            try
            {
                var index = GetColumnDropIndex(e, out _, out _);
                if (e.Data.GetData(typeof(ViewColumn)) is ViewColumn viewColumn)
                {
                    ViewEditorViewModel.MoveColumn(viewColumn, index);
                }
                else if (e.Data.GetData(typeof(ViewAttribute)) is ViewAttribute viewAttribute)
                {
                    ViewEditorViewModel.AddColumn(viewAttribute, index);
                }
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Drop Column");
            }
        }

        private void ViewColumnsListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete
                && FindAncestor<TextBoxBase>(e.OriginalSource) == null
                && ViewEditorViewModel.RemoveColumnCommand.CanExecute(ViewEditorViewModel.SelectedColumn))
            {
                ViewEditorViewModel.RemoveColumnCommand.Execute(ViewEditorViewModel.SelectedColumn);
                e.Handled = true;
            }
        }

        private void AvailableAttributesListBox_DragOver(object sender, DragEventArgs e)
        {
            // A view column dropped back here is removed
            e.Effects = e.Data.GetDataPresent(typeof(ViewColumn)) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void AvailableAttributesListBox_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (e.Data.GetData(typeof(ViewColumn)) is ViewColumn viewColumn)
            {
                ViewEditorViewModel.RemoveColumnCommand.Execute(viewColumn);
            }
        }

        private void AvailableAttributesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && AvailableAttributesListBox.SelectedItem is ViewAttribute viewAttribute)
            {
                ViewEditorViewModel.AddColumnCommand.Execute(viewAttribute);
                e.Handled = true;
            }
        }

        private void AvailableAttributeItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem listBoxItem && listBoxItem.DataContext is ViewAttribute viewAttribute)
            {
                ViewEditorViewModel.AddColumnCommand.Execute(viewAttribute);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Index in the view columns where a drop lands: before or after the item under the mouse, or at the end
        /// </summary>
        private int GetColumnDropIndex(DragEventArgs e, out ListBoxItem targetItem, out bool isAfter)
        {
            var generator = ViewColumnsListBox.ItemContainerGenerator;
            targetItem = FindAncestor<ListBoxItem>(e.OriginalSource);
            if (targetItem == null)
            {
                isAfter = true;
                var count = ViewColumnsListBox.Items.Count;
                targetItem = count > 0 ? generator.ContainerFromIndex(count - 1) as ListBoxItem : null;
                return count;
            }
            var index = generator.IndexFromContainer(targetItem);
            isAfter = e.GetPosition(targetItem).Y > targetItem.ActualHeight / 2;
            return isAfter ? index + 1 : index;
        }

        private void ShowInsertionAdorner(ListBoxItem targetItem, bool isAfter)
        {
            RemoveInsertionAdorner();
            var adornerLayer = targetItem != null ? AdornerLayer.GetAdornerLayer(targetItem) : null;
            if (adornerLayer != null)
            {
                _insertionAdorner = new InsertionAdorner(targetItem, isAfter, (Brush)FindResource("AccentBrush"));
                adornerLayer.Add(_insertionAdorner);
            }
        }

        private void RemoveInsertionAdorner()
        {
            if (_insertionAdorner != null)
            {
                AdornerLayer.GetAdornerLayer(_insertionAdorner.AdornedElement)?.Remove(_insertionAdorner);
                _insertionAdorner = null;
            }
        }

        private static T FindAncestor<T>(object source) where T : DependencyObject
        {
            var current = source as DependencyObject;
            while (current != null && !(current is T))
            {
                // Text runs are content elements, not part of the visual tree
                current = current is Visual || current is Visual3D ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }
            return current as T;
        }

        #endregion

        #region Preview grid

        private void ViewEditorViewModel_LayoutChanged(object sender, EventArgs e)
        {
            BuildPreviewColumns();
        }

        private void PreviewViewModel_ColumnsReset(object sender, EventArgs e)
        {
            BuildPreviewColumns();
        }

        /// <summary>
        /// The grid shows the view columns in their order and width; a column that is not queried yet stays empty until the next refresh
        /// </summary>
        private void BuildPreviewColumns()
        {
            _isBuildingPreviewColumns = true;
            try
            {
                foreach (var dataGridColumn in _previewColumnNames.Keys)
                {
                    ColumnWidthDescriptor.RemoveValueChanged(dataGridColumn, PreviewColumn_WidthChanged);
                }
                _previewColumnNames.Clear();
                PreviewDataGrid.Columns.Clear();
                var cellIndexes = PreviewViewModel.CellIndexes;
                foreach (var viewColumn in ViewEditorViewModel.Columns)
                {
                    var dataGridColumn = new DataGridTextColumn
                    {
                        Header = new TextBlock { Text = viewColumn.DisplayName, ToolTip = viewColumn.Caption },
                        Width = viewColumn.Width,
                        MinWidth = ViewColumn.MinWidth,
                        MaxWidth = ViewColumn.MaxWidth,
                        Binding = cellIndexes.TryGetValue(viewColumn.Name, out var cellIndex) ? new Binding($"{nameof(PreviewRow.Cells)}[{cellIndex}]") : null
                    };
                    _previewColumnNames[dataGridColumn] = viewColumn.Name;
                    PreviewDataGrid.Columns.Add(dataGridColumn);
                    ColumnWidthDescriptor.AddValueChanged(dataGridColumn, PreviewColumn_WidthChanged);
                }
            }
            finally
            {
                _isBuildingPreviewColumns = false;
            }
        }

        private void PreviewColumn_WidthChanged(object sender, EventArgs e)
        {
            if (!_isBuildingPreviewColumns
                && sender is DataGridColumn dataGridColumn
                && dataGridColumn.Width.IsAbsolute
                && _previewColumnNames.TryGetValue(dataGridColumn, out var columnName))
            {
                ViewEditorViewModel.SetColumnWidth(columnName, (int)Math.Round(dataGridColumn.Width.Value));
            }
        }

        private void PreviewDataGrid_ColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            var columnNames = PreviewDataGrid.Columns
                .OrderBy(c => c.DisplayIndex)
                .Select(c => _previewColumnNames.TryGetValue(c, out var columnName) ? columnName : null)
                .Where(cn => cn != null)
                .ToList();
            ViewEditorViewModel.ReorderColumns(columnNames);
        }

        private void PreviewRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow dataGridRow
                && PreviewViewModel.OpenRecordCommand.CanExecute(dataGridRow.Item))
            {
                PreviewViewModel.OpenRecordCommand.Execute(dataGridRow.Item);
                e.Handled = true;
            }
        }

        #endregion
    }
}
