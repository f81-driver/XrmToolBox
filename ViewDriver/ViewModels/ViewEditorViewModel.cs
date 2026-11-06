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
using System.Xml;

namespace Formula81.XrmToolBox.Tools.ViewDriver.ViewModels
{
    public class ViewEditorViewModel : ObservableObject
    {
        public static IEnumerable<string> FilterTypes { get; } = new[] { "All conditions (AND)", "Any condition (OR)" };
        public static IEnumerable<string> SortDirections { get; } = new[] { "Ascending", "Descending" };

        // The data in the preview no longer matches the query
        public event EventHandler QueryChanged;
        // The columns of the preview grid must follow the order, widths or names of the view columns
        public event EventHandler LayoutChanged;

        private readonly ViewDriverPluginControl _viewDriverPluginControl;

        private ViewDefinition _viewDefinition;
        // Suppresses change tracking while a view is (re)loaded into the editor
        private bool _isLoading;
        // A change made in the preview grid itself does not have to be pushed back to it
        private bool _isApplyingGridLayout;
        private bool _isFetchXmlTextEdited;
        private HashSet<string> _columnNameSet = new HashSet<string>();

        private ViewItem _view;
        public ViewItem View
        {
            get => _view;
            private set
            {
                SetValue(nameof(View), value, ref _view);
                NotifyPropertyChanged(nameof(HasView));
                NotifyPropertyChanged(nameof(IsSystemView));
            }
        }

        public bool HasView => _view != null;
        public bool IsSystemView => _view?.Kind == ViewKind.System;

        private EntityDetail _entity;
        public EntityDetail Entity
        {
            get => _entity;
            private set
            {
                SetValue(nameof(Entity), value, ref _entity);
                NotifyPropertyChanged(nameof(FilterAttributes));
            }
        }

        public IReadOnlyList<ViewAttribute> FilterAttributes => _entity?.FilterAttributes ?? new ViewAttribute[0];

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (value == _name)
                {
                    return;
                }
                SetValue(nameof(Name), value, ref _name);
                MarkDirty(false);
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                if (value == _description)
                {
                    return;
                }
                SetValue(nameof(Description), value, ref _description);
                MarkDirty(false);
            }
        }

        private bool _isDirty;
        public bool IsDirty { get => _isDirty; private set => SetValue(nameof(IsDirty), value, ref _isDirty); }

        public ObservableCollection<ViewColumn> Columns { get; }
        public ObservableCollection<ViewCondition> Conditions { get; }
        public ObservableCollection<ViewOrder> Orders { get; }

        private ViewColumn _selectedColumn;
        public ViewColumn SelectedColumn { get => _selectedColumn; set => SetValue(nameof(SelectedColumn), value, ref _selectedColumn); }

        private int _filterTypeIndex;
        public int FilterTypeIndex
        {
            get => _filterTypeIndex;
            set
            {
                if (value < 0 || value == _filterTypeIndex)
                {
                    return;
                }
                SetValue(nameof(FilterTypeIndex), value, ref _filterTypeIndex);
                // Without conditions there is no filter yet; the type is applied with the first condition
                if (_viewDefinition != null && (Conditions.Any() || _viewDefinition.NestedFilterCount > 0))
                {
                    _viewDefinition.IsMatchAny = value == 1;
                    OnQueryEdited();
                }
            }
        }

        private string _advancedQueryText;
        public string AdvancedQueryText { get => _advancedQueryText; private set => SetValue(nameof(AdvancedQueryText), value, ref _advancedQueryText); }

        private IReadOnlyList<RelatedTable> _columnSources = new RelatedTable[0];
        public IReadOnlyList<RelatedTable> ColumnSources { get => _columnSources; private set => SetValue(nameof(ColumnSources), value, ref _columnSources); }

        private RelatedTable _selectedColumnSource;
        public RelatedTable SelectedColumnSource
        {
            get => _selectedColumnSource;
            set
            {
                if (value == _selectedColumnSource)
                {
                    return;
                }
                SetValue(nameof(SelectedColumnSource), value, ref _selectedColumnSource);
                LoadAvailableAttributes(value);
            }
        }

        private ICollectionView _availableAttributesViewSource;
        public ICollectionView AvailableAttributesViewSource { get => _availableAttributesViewSource; private set => SetValue(nameof(AvailableAttributesViewSource), value, ref _availableAttributesViewSource); }

        private bool _isLoadingAvailable;
        public bool IsLoadingAvailable { get => _isLoadingAvailable; private set => SetValue(nameof(IsLoadingAvailable), value, ref _isLoadingAvailable); }

        private string _fetchXmlText;
        public string FetchXmlText
        {
            get => _fetchXmlText;
            set
            {
                if (value == _fetchXmlText)
                {
                    return;
                }
                SetValue(nameof(FetchXmlText), value, ref _fetchXmlText);
                IsFetchXmlTextEdited = true;
            }
        }

        public bool IsFetchXmlTextEdited
        {
            get => _isFetchXmlTextEdited;
            private set => SetValue(nameof(IsFetchXmlTextEdited), value, ref _isFetchXmlTextEdited);
        }

        private string _layoutXmlText;
        public string LayoutXmlText { get => _layoutXmlText; private set => SetValue(nameof(LayoutXmlText), value, ref _layoutXmlText); }

        public ICommand SaveCommand { get; }
        public ICommand RevertCommand { get; }
        public ICommand AddColumnCommand { get; }
        public ICommand RemoveColumnCommand { get; }
        public ICommand RoundColumnWidthsCommand { get; }
        public ICommand AddConditionCommand { get; }
        public ICommand RemoveConditionCommand { get; }
        public ICommand AddOrderCommand { get; }
        public ICommand RemoveOrderCommand { get; }
        public ICommand ApplyFetchXmlCommand { get; }
        public ICommand ResetFetchXmlCommand { get; }
        public ICommand EditInFetchXmlBuilderCommand { get; }
        public ICommand CopyCommand { get; }

        private IReadOnlyList<ViewAttribute> _availableAttributes = new ViewAttribute[0];
        private string _availableFilter;

        public ViewEditorViewModel(ViewDriverPluginControl viewDriverPluginControl)
        {
            _viewDriverPluginControl = viewDriverPluginControl;

            Columns = new ObservableCollection<ViewColumn>();
            Conditions = new ObservableCollection<ViewCondition>();
            Orders = new ObservableCollection<ViewOrder>();

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            RevertCommand = new RelayCommand(ExecuteRevert, CanExecuteRevert);
            AddColumnCommand = new RelayCommand(ExecuteAddColumn, CanExecuteAddColumn);
            RemoveColumnCommand = new RelayCommand(ExecuteRemoveColumn, CanExecuteRemoveColumn);
            RoundColumnWidthsCommand = new RelayCommand(ExecuteRoundColumnWidths, CanExecuteRoundColumnWidths);
            AddConditionCommand = new RelayCommand(ExecuteAddCondition, CanExecuteEdit);
            RemoveConditionCommand = new RelayCommand(ExecuteRemoveCondition, p => p is ViewCondition);
            AddOrderCommand = new RelayCommand(ExecuteAddOrder, CanExecuteEdit);
            RemoveOrderCommand = new RelayCommand(ExecuteRemoveOrder, p => p is ViewOrder);
            ApplyFetchXmlCommand = new RelayCommand(ExecuteApplyFetchXml, CanExecuteApplyFetchXml);
            ResetFetchXmlCommand = new RelayCommand(ExecuteResetFetchXml, CanExecuteApplyFetchXml);
            EditInFetchXmlBuilderCommand = new RelayCommand(ExecuteEditInFetchXmlBuilder, CanExecuteEdit);
            CopyCommand = new RelayCommand(ExecuteCopy, p => !string.IsNullOrEmpty(p?.ToString()));
        }

        #region Commands

        internal bool CanExecuteEdit(object parameter)
        {
            return HasView;
        }

        internal bool CanExecuteSave(object parameter)
        {
            return !_viewDriverPluginControl.IsBusy
                && HasView
                && !IsSystemView
                && IsDirty
                && !string.IsNullOrWhiteSpace(Name);
        }

        internal bool CanExecuteRevert(object parameter)
        {
            return !_viewDriverPluginControl.IsBusy
                && HasView
                && IsDirty;
        }

        internal bool CanExecuteAddColumn(object parameter)
        {
            return HasView && parameter is ViewAttribute;
        }

        internal bool CanExecuteRemoveColumn(object parameter)
        {
            return HasView && (parameter ?? SelectedColumn) is ViewColumn;
        }

        internal bool CanExecuteApplyFetchXml(object parameter)
        {
            return HasView && IsFetchXmlTextEdited;
        }

        internal void ExecuteSave(object parameter)
        {
            try
            {
                // Typed FetchXML that was not applied yet is part of what is saved
                if (IsFetchXmlTextEdited)
                {
                    ApplyFetchXml(FetchXmlText);
                }
                _viewDriverPluginControl.SaveViewAsync(View, Name.Trim(), string.IsNullOrWhiteSpace(Description) ? null : Description, GetFetchXml(), GetLayoutXml(), _editVersion);
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Save View");
            }
        }

        internal void ExecuteRevert(object parameter)
        {
            try
            {
                Revert();
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Revert View");
            }
        }

        internal void ExecuteAddColumn(object parameter)
        {
            try
            {
                AddColumn(parameter as ViewAttribute, -1);
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Add Column");
            }
        }

        internal void ExecuteRemoveColumn(object parameter)
        {
            try
            {
                RemoveColumn((parameter ?? SelectedColumn) as ViewColumn);
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Remove Column");
            }
        }

        internal void ExecuteAddCondition(object parameter)
        {
            try
            {
                var viewAttribute = FilterAttributes.FirstOrDefault(va => va.IsPrimaryName) ?? FilterAttributes.FirstOrDefault();
                if (viewAttribute == null)
                {
                    return;
                }
                var conditionElement = _viewDefinition.AddCondition(viewAttribute.LogicalName, ConditionOperator.Default.Value);
                _viewDefinition.IsMatchAny = FilterTypeIndex == 1;
                Conditions.Add(CreateCondition(conditionElement));
                OnQueryEdited();
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Add Condition");
            }
        }

        internal void ExecuteRemoveCondition(object parameter)
        {
            if (parameter is ViewCondition viewCondition)
            {
                viewCondition.Changed -= ViewCondition_Changed;
                _viewDefinition.RemoveCondition(viewCondition.Element);
                Conditions.Remove(viewCondition);
                OnQueryEdited();
            }
        }

        internal void ExecuteAddOrder(object parameter)
        {
            try
            {
                var orderedNames = new HashSet<string>(Orders.Select(vo => vo.AttributeName));
                var viewAttribute = FilterAttributes.FirstOrDefault(va => va.IsPrimaryName && !orderedNames.Contains(va.LogicalName))
                    ?? FilterAttributes.FirstOrDefault(va => !orderedNames.Contains(va.LogicalName));
                if (viewAttribute == null)
                {
                    return;
                }
                Orders.Add(CreateOrder(_viewDefinition.AddOrder(viewAttribute.LogicalName)));
                OnQueryEdited();
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Add Sort");
            }
        }

        internal void ExecuteRemoveOrder(object parameter)
        {
            if (parameter is ViewOrder viewOrder)
            {
                viewOrder.Changed -= ViewOrder_Changed;
                _viewDefinition.RemoveOrder(viewOrder.Element);
                Orders.Remove(viewOrder);
                OnQueryEdited();
            }
        }

        internal void ExecuteApplyFetchXml(object parameter)
        {
            try
            {
                ApplyFetchXml(FetchXmlText);
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Apply FetchXML");
            }
        }

        internal void ExecuteResetFetchXml(object parameter)
        {
            UpdateXmlTexts(true);
        }

        internal void ExecuteEditInFetchXmlBuilder(object parameter)
        {
            try
            {
                _viewDriverPluginControl.CallFetchXmlBuilder(_viewDefinition.GetFetchXml(true));
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "FetchXML Builder");
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

        #endregion

        internal void Load(ViewItem viewItem, EntityDetail entityDetail)
        {
            // Parsed first: an invalid view leaves the editor as it was
            var viewDefinition = ViewDefinition.Parse(viewItem.FetchXml, viewItem.LayoutXml, entityDetail);
            _isLoading = true;
            try
            {
                _viewDefinition = viewDefinition;
                Entity = entityDetail;
                View = viewItem;
                _name = viewItem.Name;
                NotifyPropertyChanged(nameof(Name));
                _description = viewItem.Description;
                NotifyPropertyChanged(nameof(Description));
                ReadDefinition();
                ColumnSources = entityDetail.RelatedTables;
                if (_selectedColumnSource == null || _selectedColumnSource.EntityLogicalName != entityDetail.LogicalName || !_selectedColumnSource.IsRoot)
                {
                    _selectedColumnSource = ColumnSources.FirstOrDefault();
                    NotifyPropertyChanged(nameof(SelectedColumnSource));
                }
                LoadAvailableAttributes(_selectedColumnSource);
                IsDirty = viewItem.IsNew;
                viewItem.IsModified = viewItem.IsNew;
            }
            finally
            {
                _isLoading = false;
            }
            LayoutChanged?.Invoke(this, EventArgs.Empty);
            LoadRelatedEntities();
        }

        internal void Clear()
        {
            _viewDefinition = null;
            View = null;
            Entity = null;
            _name = null;
            NotifyPropertyChanged(nameof(Name));
            _description = null;
            NotifyPropertyChanged(nameof(Description));
            ClearCollections();
            ColumnSources = new RelatedTable[0];
            _selectedColumnSource = null;
            NotifyPropertyChanged(nameof(SelectedColumnSource));
            SetAvailableAttributes(new ViewAttribute[0]);
            AdvancedQueryText = null;
            _fetchXmlText = null;
            NotifyPropertyChanged(nameof(FetchXmlText));
            IsFetchXmlTextEdited = false;
            LayoutXmlText = null;
            IsDirty = false;
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }

        internal void Revert()
        {
            var viewItem = View;
            if (viewItem == null)
            {
                return;
            }
            if (viewItem.IsNew)
            {
                _viewDriverPluginControl.RevertNewView(viewItem);
            }
            else
            {
                _viewDriverPluginControl.OpenView(viewItem);
            }
        }

        /// <param name="editVersion">The edit version that was saved; edits made while saving keep the view dirty</param>
        internal void MarkSaved(ViewItem viewItem, int editVersion)
        {
            if (viewItem == View && editVersion == _editVersion)
            {
                IsDirty = false;
                viewItem.IsModified = false;
            }
        }

        internal string GetFetchXml()
        {
            return _viewDefinition?.GetFetchXml(false);
        }

        internal string GetLayoutXml()
        {
            return _viewDefinition?.GetLayoutXml(false);
        }

        /// <summary>
        /// Replaces the query of the view, from the FetchXML tab or from FetchXML Builder
        /// </summary>
        internal void ApplyFetchXml(string fetchXml)
        {
            if (_viewDefinition == null)
            {
                return;
            }
            try
            {
                _viewDefinition.ReplaceFetch(fetchXml, Entity);
            }
            catch (XmlException xmlException)
            {
                throw new InvalidOperationException($"The FetchXML is not valid XML: {xmlException.Message}", xmlException);
            }
            ReadDefinition();
            MarkDirty(true);
            LayoutChanged?.Invoke(this, EventArgs.Empty);
            LoadRelatedEntities();
        }

        #region Columns

        internal void AddColumn(ViewAttribute viewAttribute, int index)
        {
            var columnSource = SelectedColumnSource;
            if (_viewDefinition == null || viewAttribute == null || columnSource == null
                // Dragged from a list that has changed since
                || viewAttribute.EntityLogicalName != columnSource.EntityLogicalName)
            {
                return;
            }
            var cellName = columnSource.IsRoot ? viewAttribute.LogicalName : $"{_viewDefinition.EnsureLink(columnSource)}.{viewAttribute.LogicalName}";
            var viewColumn = Columns.FirstOrDefault(vc => vc.Name == cellName);
            if (viewColumn == null)
            {
                _viewDefinition.EnsureAttribute(cellName);
                viewColumn = CreateColumn(cellName, ViewColumn.DefaultWidth);
                Columns.Insert(index < 0 || index > Columns.Count ? Columns.Count : index, viewColumn);
                OnLayoutEdited(true);
                // The new column has no data until the query runs again
                MarkDirty(true);
            }
            SelectedColumn = viewColumn;
        }

        internal void RemoveColumn(ViewColumn viewColumn)
        {
            if (viewColumn == null || !Columns.Contains(viewColumn))
            {
                return;
            }
            viewColumn.PropertyChanged -= ViewColumn_PropertyChanged;
            Columns.Remove(viewColumn);
            _viewDefinition.RemoveAttribute(viewColumn.Name, Entity.PrimaryIdAttribute);
            OnLayoutEdited(true);
        }

        internal void MoveColumn(ViewColumn viewColumn, int index)
        {
            var oldIndex = Columns.IndexOf(viewColumn);
            if (oldIndex < 0)
            {
                return;
            }
            // The index is where it is dropped, before the column is taken out
            var newIndex = Math.Max(0, Math.Min(Columns.Count - 1, index > oldIndex ? index - 1 : index));
            if (newIndex != oldIndex)
            {
                Columns.Move(oldIndex, newIndex);
                OnLayoutEdited(true);
            }
        }

        /// <summary>
        /// Columns were reordered in the preview grid
        /// </summary>
        internal void ReorderColumns(IList<string> columnNames)
        {
            _isApplyingGridLayout = true;
            try
            {
                var moved = false;
                for (var newIndex = 0; newIndex < columnNames.Count; newIndex++)
                {
                    var oldIndex = Columns.IndexOf(Columns.FirstOrDefault(vc => vc.Name == columnNames[newIndex]));
                    if (oldIndex >= 0 && oldIndex != newIndex && newIndex < Columns.Count)
                    {
                        Columns.Move(oldIndex, newIndex);
                        moved = true;
                    }
                }
                if (moved)
                {
                    OnLayoutEdited(false);
                }
            }
            finally
            {
                _isApplyingGridLayout = false;
            }
        }

        /// <summary>
        /// A column was resized in the preview grid
        /// </summary>
        internal void SetColumnWidth(string columnName, int width)
        {
            var viewColumn = Columns.FirstOrDefault(vc => vc.Name == columnName);
            if (viewColumn == null || viewColumn.Width == width)
            {
                return;
            }
            _isApplyingGridLayout = true;
            try
            {
                viewColumn.Width = width;
            }
            finally
            {
                _isApplyingGridLayout = false;
            }
        }

        internal bool CanExecuteRoundColumnWidths(object parameter)
        {
            return Columns.Any(vc => vc.Width != ViewColumn.RoundWidth(vc.Width));
        }

        /// <summary>
        /// Rounds every column width to a tidy step, as one layout edit
        /// </summary>
        internal void ExecuteRoundColumnWidths(object parameter)
        {
            try
            {
                _isApplyingGridLayout = true;
                try
                {
                    foreach (var viewColumn in Columns)
                    {
                        viewColumn.Width = ViewColumn.RoundWidth(viewColumn.Width);
                    }
                }
                finally
                {
                    _isApplyingGridLayout = false;
                }
                OnLayoutEdited(true);
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Round Column Widths");
            }
        }

        internal void SetAvailableFilter(string filter)
        {
            _availableFilter = filter;
            AvailableAttributesViewSource?.Refresh();
        }

        private ViewColumn CreateColumn(string cellName, int width)
        {
            var viewColumn = new ViewColumn(cellName, width);
            UpdateColumnCaption(viewColumn);
            viewColumn.PropertyChanged += ViewColumn_PropertyChanged;
            return viewColumn;
        }

        private void UpdateColumnCaption(ViewColumn viewColumn)
        {
            var name = viewColumn.Name;
            var separatorIndex = name.IndexOf('.');
            if (separatorIndex < 0)
            {
                var isKnown = Entity.TryGetAttribute(name, out var viewAttribute);
                viewColumn.DisplayName = isKnown ? viewAttribute.DisplayName : name;
                viewColumn.Caption = isKnown ? $"{name} · {viewAttribute.TypeName}" : $"{name} · not found in this table";
                viewColumn.IsUnknown = !isKnown;
                return;
            }
            var alias = name.Substring(0, separatorIndex);
            var attributeName = name.Substring(separatorIndex + 1);
            if (!_viewDefinition.TryGetLink(alias, out var entityName, out var lookupAttribute))
            {
                viewColumn.DisplayName = name;
                viewColumn.Caption = $"{name} · related table not in the query";
                viewColumn.IsUnknown = true;
                return;
            }
            var lookupDisplayName = lookupAttribute != null && Entity.TryGetAttribute(lookupAttribute, out var lookupViewAttribute) ? lookupViewAttribute.DisplayName : alias;
            if (_viewDriverPluginControl.TryGetEntityDetail(entityName, out var entityDetail))
            {
                var isKnown = entityDetail.TryGetAttribute(attributeName, out var viewAttribute);
                viewColumn.DisplayName = $"{(isKnown ? viewAttribute.DisplayName : attributeName)} ({lookupDisplayName})";
                viewColumn.Caption = isKnown ? $"{entityName}.{attributeName} · {viewAttribute.TypeName}" : $"{entityName}.{attributeName} · not found";
                viewColumn.IsUnknown = !isKnown;
            }
            else
            {
                // The related table is still loading
                viewColumn.DisplayName = $"{attributeName} ({lookupDisplayName})";
                viewColumn.Caption = $"{entityName}.{attributeName}";
                viewColumn.IsUnknown = false;
            }
        }

        private void LoadRelatedEntities()
        {
            var viewDefinition = _viewDefinition;
            var linkEntityNames = viewDefinition?.GetLinkEntityNames().ToList();
            if (linkEntityNames == null || !linkEntityNames.Any())
            {
                return;
            }
            _viewDriverPluginControl.LoadEntityDetailsAsync(linkEntityNames, () =>
            {
                if (viewDefinition == _viewDefinition)
                {
                    foreach (var viewColumn in Columns.Where(vc => vc.IsRelated))
                    {
                        UpdateColumnCaption(viewColumn);
                    }
                    LayoutChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private void LoadAvailableAttributes(RelatedTable relatedTable)
        {
            IsLoadingAvailable = false;
            if (relatedTable == null || Entity == null)
            {
                SetAvailableAttributes(new ViewAttribute[0]);
            }
            else if (relatedTable.IsRoot)
            {
                SetAvailableAttributes(Entity.GridAttributes);
            }
            else if (_viewDriverPluginControl.TryGetEntityDetail(relatedTable.EntityLogicalName, out var entityDetail))
            {
                SetAvailableAttributes(entityDetail.GridAttributes);
            }
            else
            {
                SetAvailableAttributes(new ViewAttribute[0]);
                IsLoadingAvailable = true;
                _viewDriverPluginControl.LoadEntityDetailsAsync(new[] { relatedTable.EntityLogicalName }, () =>
                {
                    if (SelectedColumnSource == relatedTable)
                    {
                        IsLoadingAvailable = false;
                        if (_viewDriverPluginControl.TryGetEntityDetail(relatedTable.EntityLogicalName, out var loadedEntityDetail))
                        {
                            SetAvailableAttributes(loadedEntityDetail.GridAttributes);
                        }
                    }
                });
            }
        }

        private void SetAvailableAttributes(IReadOnlyList<ViewAttribute> viewAttributes)
        {
            _availableAttributes = viewAttributes;
            var availableAttributesViewSource = new CollectionViewSource { Source = _availableAttributes }.View;
            availableAttributesViewSource.Filter = FilterAvailableAttribute;
            AvailableAttributesViewSource = availableAttributesViewSource;
        }

        private bool FilterAvailableAttribute(object obj)
        {
            return obj is ViewAttribute viewAttribute
                // Columns of the view table that are already in the view are left out
                && !((SelectedColumnSource?.IsRoot ?? false) && _columnNameSet.Contains(viewAttribute.LogicalName))
                && (string.IsNullOrEmpty(_availableFilter)
                    || viewAttribute.DisplayName.IndexOf(_availableFilter, StringComparison.OrdinalIgnoreCase) >= 0
                    || viewAttribute.LogicalName.IndexOf(_availableFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void ViewColumn_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewColumn.Width))
            {
                OnLayoutEdited(!_isApplyingGridLayout);
            }
        }

        private void OnLayoutEdited(bool updateGrid)
        {
            _viewDefinition.SetCells(Columns);
            var columnNameSet = new HashSet<string>(Columns.Select(vc => vc.Name));
            if (!columnNameSet.SetEquals(_columnNameSet))
            {
                _columnNameSet = columnNameSet;
                AvailableAttributesViewSource?.Refresh();
            }
            UpdateXmlTexts(false);
            MarkDirty(false);
            if (updateGrid)
            {
                LayoutChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Filter and sort

        private ViewCondition CreateCondition(System.Xml.Linq.XElement conditionElement)
        {
            var viewCondition = new ViewCondition(conditionElement, ResolveFilterAttribute);
            viewCondition.Changed += ViewCondition_Changed;
            return viewCondition;
        }

        private ViewOrder CreateOrder(System.Xml.Linq.XElement orderElement)
        {
            var viewOrder = new ViewOrder(orderElement);
            viewOrder.Changed += ViewOrder_Changed;
            return viewOrder;
        }

        private ViewAttribute ResolveFilterAttribute(string logicalName)
        {
            return Entity != null && Entity.TryGetAttribute(logicalName, out var viewAttribute) ? viewAttribute : null;
        }

        private void ViewCondition_Changed(object sender, EventArgs e)
        {
            OnQueryEdited();
        }

        private void ViewOrder_Changed(object sender, EventArgs e)
        {
            OnQueryEdited();
        }

        private void OnQueryEdited()
        {
            UpdateAdvancedQueryText();
            UpdateXmlTexts(false);
            MarkDirty(true);
        }

        private void UpdateAdvancedQueryText()
        {
            var parts = new List<string>();
            var nestedFilterCount = _viewDefinition?.NestedFilterCount ?? 0;
            var linkFilterCount = _viewDefinition?.LinkFilterCount ?? 0;
            var linkOrderCount = _viewDefinition?.LinkOrderCount ?? 0;
            if (nestedFilterCount > 0)
            {
                parts.Add(nestedFilterCount == 1 ? "1 nested filter group" : $"{nestedFilterCount} nested filter groups");
            }
            if (linkFilterCount > 0)
            {
                parts.Add(linkFilterCount == 1 ? "1 filter on a related table" : $"{linkFilterCount} filters on related tables");
            }
            if (linkOrderCount > 0)
            {
                parts.Add(linkOrderCount == 1 ? "1 sort on a related table" : $"{linkOrderCount} sorts on related tables");
            }
            AdvancedQueryText = parts.Any() ? $"This view also has {string.Join(", ", parts)}. They are kept as they are; edit them on the FetchXML tab or in FetchXML Builder." : null;
        }

        #endregion

        private void ReadDefinition()
        {
            ClearCollections();
            foreach (var (name, width) in _viewDefinition.GetCells())
            {
                Columns.Add(CreateColumn(name, width));
            }
            foreach (var conditionElement in _viewDefinition.GetConditionElements())
            {
                Conditions.Add(CreateCondition(conditionElement));
            }
            foreach (var orderElement in _viewDefinition.GetOrderElements())
            {
                Orders.Add(CreateOrder(orderElement));
            }
            _filterTypeIndex = _viewDefinition.IsMatchAny ? 1 : 0;
            NotifyPropertyChanged(nameof(FilterTypeIndex));
            _columnNameSet = new HashSet<string>(Columns.Select(vc => vc.Name));
            AvailableAttributesViewSource?.Refresh();
            UpdateAdvancedQueryText();
            UpdateXmlTexts(true);
        }

        private void ClearCollections()
        {
            foreach (var viewColumn in Columns)
            {
                viewColumn.PropertyChanged -= ViewColumn_PropertyChanged;
            }
            Columns.Clear();
            foreach (var viewCondition in Conditions)
            {
                viewCondition.Changed -= ViewCondition_Changed;
            }
            Conditions.Clear();
            foreach (var viewOrder in Orders)
            {
                viewOrder.Changed -= ViewOrder_Changed;
            }
            Orders.Clear();
            SelectedColumn = null;
            _columnNameSet = new HashSet<string>();
        }

        /// <param name="overwriteEditedFetchXml">FetchXML that is being typed is kept, unless it was applied or reset</param>
        private void UpdateXmlTexts(bool overwriteEditedFetchXml)
        {
            if (overwriteEditedFetchXml || !IsFetchXmlTextEdited)
            {
                _fetchXmlText = _viewDefinition?.GetFetchXml(true);
                NotifyPropertyChanged(nameof(FetchXmlText));
                IsFetchXmlTextEdited = false;
            }
            LayoutXmlText = _viewDefinition?.GetLayoutXml(true);
        }

        /// <summary>Counts the edits, so a save that finishes knows whether the view was changed while it ran</summary>
        private int _editVersion;

        private void MarkDirty(bool queryChanged)
        {
            if (_isLoading || View == null)
            {
                return;
            }
            _editVersion++;
            IsDirty = true;
            View.IsModified = true;
            if (queryChanged)
            {
                QueryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
