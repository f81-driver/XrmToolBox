using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Formula81.XrmToolBox.Tools.ViewDriver.ViewModels
{
    public class EntityListViewModel : ObservableObject
    {
        /// <summary>Stored with the criteria in the settings; the criterion keys stored with it are the selection to return to</summary>
        private const string AllTablesKey = "all";
        private static readonly string[] DefaultCriterionKeys = { "advancedfind", "public" };

        public const string DisplayNameSort = "Display name";
        public const string LogicalNameSort = "Logical name";

        public static IEnumerable<string> SortOptions { get; } = new[] { DisplayNameSort, LogicalNameSort };

        public event EventHandler FilterReset;

        private readonly ViewDriverPluginControl _viewDriverPluginControl;

        private ICollectionView _entitiesViewSource;
        public ICollectionView EntitiesViewSource { get => _entitiesViewSource; private set => SetValue(nameof(EntitiesViewSource), value, ref _entitiesViewSource); }

        private ViewEntity _selectedEntity;
        public ViewEntity SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (value == _selectedEntity || value?.IsPlaceholder == true)
                {
                    return;
                }
                if (!_viewDriverPluginControl.ConfirmDiscardChanges())
                {
                    // Puts the list selection back once the list is done changing it
                    Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => NotifyPropertyChanged(nameof(SelectedEntity))), DispatcherPriority.Background);
                    return;
                }
                SetValue(nameof(SelectedEntity), value, ref _selectedEntity);
                _viewDriverPluginControl.SelectEntity(value);
            }
        }

        /// <summary>The kinds of tables to show, any of which a table must be</summary>
        public IReadOnlyList<TableCriterion> KindCriteria { get; }
        /// <summary>What every table shown must be</summary>
        public IReadOnlyList<TableCriterion> RequirementCriteria { get; }

        private readonly List<TableCriterion> _criteria;
        private bool _isApplyingSettings;

        /// <summary>The criteria checked before All tables was, restored when it is unchecked</summary>
        private HashSet<string> _keysBeforeAllTables = new HashSet<string>();

        private bool _isAllTables;
        /// <summary>Shows every table, with all criteria checked and locked</summary>
        public bool IsAllTables
        {
            get => _isAllTables;
            set
            {
                if (value == _isAllTables)
                {
                    return;
                }
                if (value)
                {
                    _keysBeforeAllTables = GetCheckedKeys();
                }
                SetValue(nameof(IsAllTables), value, ref _isAllTables);
                ApplyCriteria(value ? null : _keysBeforeAllTables);
                SaveCriteria();
            }
        }

        private string _criteriaText;
        public string CriteriaText { get => _criteriaText; private set => SetValue(nameof(CriteriaText), value, ref _criteriaText); }

        private string _criteriaToolTip;
        public string CriteriaToolTip { get => _criteriaToolTip; private set => SetValue(nameof(CriteriaToolTip), value, ref _criteriaToolTip); }

        /// <summary>A table navigated to while the criteria hide it, shown anyway until the criteria change</summary>
        private ViewEntity _revealedEntity;

        public string SortBy
        {
            get => _viewDriverPluginControl.Settings.EntitySortBy == LogicalNameSort ? LogicalNameSort : DisplayNameSort;
            set
            {
                _viewDriverPluginControl.Settings.EntitySortBy = value;
                _viewDriverPluginControl.SaveSettings();
                NotifyPropertyChanged(nameof(SortBy));
                ApplySort(EntitiesViewSource);
            }
        }

        private string _countText;
        public string CountText { get => _countText; private set => SetValue(nameof(CountText), value, ref _countText); }

        public IEnumerable<ViewEntity> Entities { get; private set; } = Enumerable.Empty<ViewEntity>();

        public ICommand FavorizeCommand { get; }
        public ICommand CopyCommand { get; }

        private string _filter;

        public EntityListViewModel(ViewDriverPluginControl viewDriverPluginControl)
        {
            _viewDriverPluginControl = viewDriverPluginControl;

            FavorizeCommand = new RelayCommand(ExecuteFavorize, CanExecuteFavorize);
            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);

            KindCriteria = new[]
            {
                new TableCriterion("custom", "Custom", "Tables created by a solution, including Microsoft solutions", false, ve => ve.IsCustom),
                new TableCriterion("system", "System", "Tables that are part of Dataverse itself", false, ve => !ve.IsCustom),
                new TableCriterion("activity", "Activity", "Activity tables, such as email, task and appointment", false, ve => ve.IsActivity)
            };
            RequirementCriteria = new[]
            {
                new TableCriterion("advancedfind", "Valid for Advanced Find", "Only these tables can have personal views", true, ve => ve.IsValidForAdvancedFind),
                new TableCriterion("public", "Public", "Hide private tables, which are for internal use by Dataverse", true, ve => !ve.IsPrivate),
                new TableCriterion("notintersect", "Not intersect", "Hide the intersect tables of many-to-many relationships", true, ve => !ve.IsIntersect),
                // Unchecked hides them: deprecated tables are rarely wanted, so letting them in is what takes a check
                new TableCriterion("deprecated", "Deprecated", "Show tables whose display name starts with [Deprecated] or (Deprecated)", true, ve => !ve.IsDeprecated, appliesWhenUnchecked: true)
            };
            _criteria = KindCriteria.Concat(RequirementCriteria).ToList();
            foreach (var criterion in _criteria)
            {
                criterion.PropertyChanged += Criterion_PropertyChanged;
            }
            ApplyCriteriaSettings();
        }

        private void Criterion_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isApplyingSettings || e.PropertyName != nameof(TableCriterion.IsChecked))
            {
                return;
            }
            SaveCriteria();
            _revealedEntity = null;
            UpdateCriteriaText();
            Refresh();
        }

        private HashSet<string> GetCheckedKeys()
        {
            return new HashSet<string>(_criteria.Where(tc => tc.IsChecked).Select(tc => tc.Key));
        }

        private void SaveCriteria()
        {
            var keys = _isAllTables ? new[] { AllTablesKey }.Concat(_keysBeforeAllTables) : GetCheckedKeys();
            _viewDriverPluginControl.Settings.EntityCriteria = string.Join(",", keys);
            _viewDriverPluginControl.SaveSettings();
        }

        private void ApplyCriteriaSettings()
        {
            var entityCriteria = _viewDriverPluginControl.Settings.EntityCriteria;
            var keys = new HashSet<string>(entityCriteria == null ? DefaultCriterionKeys : entityCriteria.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            _isAllTables = keys.Remove(AllTablesKey);
            NotifyPropertyChanged(nameof(IsAllTables));
            _keysBeforeAllTables = _isAllTables ? keys : new HashSet<string>();
            ApplyCriteria(_isAllTables ? null : keys);
        }

        /// <param name="checkedKeys">The criteria to check, null to check and lock them all</param>
        private void ApplyCriteria(HashSet<string> checkedKeys)
        {
            _isApplyingSettings = true;
            try
            {
                foreach (var criterion in _criteria)
                {
                    criterion.IsChecked = checkedKeys?.Contains(criterion.Key) ?? true;
                    criterion.IsEnabled = checkedKeys != null;
                }
            }
            finally
            {
                _isApplyingSettings = false;
            }
            _revealedEntity = null;
            UpdateCriteriaText();
            Refresh();
        }

        private void UpdateCriteriaText()
        {
            if (_isAllTables)
            {
                CriteriaText = "All tables";
                CriteriaToolTip = "Show every table";
                return;
            }
            var checkedKinds = KindCriteria.Where(tc => tc.IsChecked).Select(tc => tc.Label).ToList();
            var checkedRequirements = RequirementCriteria.Where(tc => tc.Applies).Select(tc => tc.SummaryLabel).ToList();
            var kindsText = checkedKinds.Any() ? string.Join(", ", checkedKinds) : "Any kind";
            CriteriaText = !checkedKinds.Any() && checkedRequirements.Any() ? string.Join(", ", checkedRequirements)
                : checkedRequirements.Any() ? $"{kindsText} (+{checkedRequirements.Count})"
                : kindsText;
            CriteriaToolTip = $"Show: {kindsText}"
                + (checkedRequirements.Any() ? $"\nOnly: {string.Join(", ", checkedRequirements)}" : string.Empty);
        }

        internal bool CanExecuteFavorize(object parameter)
        {
            return parameter is ViewEntity;
        }

        internal bool CanExecuteCopy(object parameter)
        {
            return !string.IsNullOrEmpty(parameter?.ToString());
        }

        internal void ExecuteFavorize(object parameter)
        {
            try
            {
                if (parameter is ViewEntity viewEntity)
                {
                    _viewDriverPluginControl.Settings.FavorizeEntity(viewEntity.LogicalName);
                    viewEntity.IsFavorite = _viewDriverPluginControl.Settings.IsFavoriteEntity(viewEntity.LogicalName);
                    _viewDriverPluginControl.SaveSettings();
                    Refresh();
                }
            }
            catch (Exception exception)
            {
                _viewDriverPluginControl.ShowErrorDialog(exception, "Favorize Table");
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

        internal ViewEntity Find(string logicalName)
        {
            return Entities.FirstOrDefault(ve => ve.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));
        }

        internal void Select(ViewEntity viewEntity)
        {
            if (viewEntity == null)
            {
                return;
            }
            // A table hidden by the filter or criteria is shown anyway when navigated to, without changing the saved criteria
            if (!IsVisible(viewEntity))
            {
                _filter = null;
                FilterReset?.Invoke(this, EventArgs.Empty);
                _revealedEntity = IsInCriteria(viewEntity) ? null : viewEntity;
                Refresh();
            }
            _selectedEntity = viewEntity;
            NotifyPropertyChanged(nameof(SelectedEntity));
            _viewDriverPluginControl.SelectEntity(viewEntity);
        }

        internal void SetEntities(IEnumerable<ViewEntity> entities)
        {
            Entities = entities.ToList();
            _revealedEntity = null;
            UpdateShowsFavoritesPlaceholder();
            var entitiesViewSource = new CollectionViewSource { Source = Entities.Concat(new[] { _favoritesPlaceholder }).ToList() }.View;
            entitiesViewSource.Filter = FilterEntity;
            // Favorites first, grouped under their own header
            entitiesViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ViewEntity.IsFavorite)));
            ApplySort(entitiesViewSource);
            EntitiesViewSource = entitiesViewSource;
            UpdateCountText();
        }

        private void ApplySort(ICollectionView entitiesViewSource)
        {
            if (entitiesViewSource == null)
            {
                return;
            }
            var sortPropertyName = SortBy == LogicalNameSort ? nameof(ViewEntity.LogicalName) : nameof(ViewEntity.DisplayName);
            using (entitiesViewSource.DeferRefresh())
            {
                entitiesViewSource.SortDescriptions.Clear();
                entitiesViewSource.SortDescriptions.Add(new SortDescription(nameof(ViewEntity.IsFavorite), ListSortDirection.Descending));
                entitiesViewSource.SortDescriptions.Add(new SortDescription(sortPropertyName, ListSortDirection.Ascending));
            }
        }

        internal void Clear()
        {
            _selectedEntity = null;
            NotifyPropertyChanged(nameof(SelectedEntity));
            SetEntities(Enumerable.Empty<ViewEntity>());
        }

        internal void SetFilter(string filter)
        {
            _filter = filter;
            Refresh();
        }

        internal void NotifySettingsChanged()
        {
            // The settings were (re)loaded for another connection
            NotifyPropertyChanged(nameof(SortBy));
            ApplySort(EntitiesViewSource);
            ApplyCriteriaSettings();
            Refresh();
        }

        internal void Refresh()
        {
            // Before refreshing, as the filter of the view asks for it
            UpdateShowsFavoritesPlaceholder();
            EntitiesViewSource?.Refresh();
            UpdateCountText();
        }

        private void UpdateShowsFavoritesPlaceholder()
        {
            // The Favorites section stays, empty, while there are favorites but all of them are hidden
            _showsFavoritesPlaceholder = Entities.Any(ve => ve.IsFavorite) && !Entities.Any(ve => ve.IsFavorite && IsVisible(ve));
        }

        private void UpdateCountText()
        {
            var visibleCount = Entities.Count(IsVisible);
            var totalCount = Entities.Count();
            CountText = visibleCount == totalCount ? $"{totalCount} tables" : $"{visibleCount} of {totalCount} tables";

            // Favorites follow the criteria like any table, so tell how many of them are hidden
            var hiddenEntities = Entities.Where(ve => !IsVisible(ve)).ToList();
            HiddenFavoriteCount = hiddenEntities.Count(ve => ve.IsFavorite);
            HiddenTableCount = hiddenEntities.Count(ve => !ve.IsFavorite);
            HiddenFavoritesToolTip = HiddenFavoriteCount == 1 ? "1 favorite table is hidden by the table criteria or the search"
                : $"{HiddenFavoriteCount} favorite tables are hidden by the table criteria or the search";
            HiddenTablesToolTip = HiddenTableCount == 1 ? "1 table is hidden by the table criteria or the search"
                : $"{HiddenTableCount} tables are hidden by the table criteria or the search";
        }

        private readonly ViewEntity _favoritesPlaceholder = ViewEntity.CreateFavoritesPlaceholder();
        private bool _showsFavoritesPlaceholder;

        private int _hiddenTableCount;
        /// <summary>Tables other than favorites hidden by the criteria or the search</summary>
        public int HiddenTableCount { get => _hiddenTableCount; private set => SetValue(nameof(HiddenTableCount), value, ref _hiddenTableCount); }

        private string _hiddenTablesToolTip;
        public string HiddenTablesToolTip { get => _hiddenTablesToolTip; private set => SetValue(nameof(HiddenTablesToolTip), value, ref _hiddenTablesToolTip); }

        private int _hiddenFavoriteCount;
        public int HiddenFavoriteCount { get => _hiddenFavoriteCount; private set => SetValue(nameof(HiddenFavoriteCount), value, ref _hiddenFavoriteCount); }

        private string _hiddenFavoritesToolTip;
        public string HiddenFavoritesToolTip { get => _hiddenFavoritesToolTip; private set => SetValue(nameof(HiddenFavoritesToolTip), value, ref _hiddenFavoritesToolTip); }

        private bool FilterEntity(object obj)
        {
            return obj is ViewEntity viewEntity && (viewEntity.IsPlaceholder ? _showsFavoritesPlaceholder : IsVisible(viewEntity));
        }

        private bool IsVisible(ViewEntity viewEntity)
        {
            return (viewEntity == _revealedEntity || IsInCriteria(viewEntity)) && IsMatch(viewEntity);
        }

        private bool IsInCriteria(ViewEntity viewEntity)
        {
            // All criteria checked would still apply the requirements, so All tables skips the criteria instead
            if (_isAllTables)
            {
                return true;
            }
            var checkedKinds = KindCriteria.Where(tc => tc.IsChecked).ToList();
            return (!checkedKinds.Any() || checkedKinds.Any(tc => tc.IsMatch(viewEntity)))
                && RequirementCriteria.Where(tc => tc.Applies).All(tc => tc.IsMatch(viewEntity));
        }

        private bool IsMatch(ViewEntity viewEntity)
        {
            return string.IsNullOrEmpty(_filter)
                || Contains(viewEntity.DisplayName, _filter)
                || Contains(viewEntity.LogicalName, _filter)
                || Contains(viewEntity.SchemaName, _filter)
                || viewEntity.ObjectTypeCode?.ToString() == _filter;
        }

        private static bool Contains(string value, string filter)
        {
            return value?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
