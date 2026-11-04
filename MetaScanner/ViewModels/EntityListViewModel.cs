using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.MetaScanner.ViewModels
{
    public class EntityListViewModel : ObservableObject
    {
        public const string AllCategory = "All tables";
        public const string CustomCategory = "Custom tables";
        public const string SystemCategory = "System tables";
        public const string ActivityCategory = "Activity tables";
        public const string AuditCategory = "Audit enabled";
        public const string IntersectCategory = "Intersect tables";

        public static IEnumerable<string> Categories { get; } = new[] { AllCategory, CustomCategory, SystemCategory, ActivityCategory, AuditCategory, IntersectCategory };

        public const string DisplayNameSort = "Display name";
        public const string LogicalNameSort = "Logical name";

        public static IEnumerable<string> SortOptions { get; } = new[] { DisplayNameSort, LogicalNameSort };

        public event EventHandler FilterReset;

        private readonly MetaScannerPluginControl _metaScannerPluginControl;

        private ICollectionView _entitiesViewSource;
        public ICollectionView EntitiesViewSource { get => _entitiesViewSource; private set => SetValue(nameof(EntitiesViewSource), value, ref _entitiesViewSource); }

        private MetaEntity _selectedEntity;
        public MetaEntity SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                if (value != _selectedEntity)
                {
                    SetValue(nameof(SelectedEntity), value, ref _selectedEntity);
                    if (value != null)
                    {
                        _metaScannerPluginControl.ShowEntityDetail(value, null);
                    }
                }
            }
        }

        private string _category = AllCategory;
        public string Category { get => _category; set { SetValue(nameof(Category), value, ref _category); Refresh(); } }

        public string SortBy
        {
            get => _metaScannerPluginControl.Settings.EntitySortBy == LogicalNameSort ? LogicalNameSort : DisplayNameSort;
            set
            {
                _metaScannerPluginControl.Settings.EntitySortBy = value;
                _metaScannerPluginControl.SaveSettings();
                NotifyPropertyChanged(nameof(SortBy));
                ApplySort(EntitiesViewSource);
            }
        }

        private string _countText;
        public string CountText { get => _countText; private set => SetValue(nameof(CountText), value, ref _countText); }

        public IEnumerable<MetaEntity> Entities { get; private set; } = Enumerable.Empty<MetaEntity>();

        public ICommand FavorizeCommand { get; }
        public ICommand CopyCommand { get; }

        private string _filter;

        public EntityListViewModel(MetaScannerPluginControl metaScannerPluginControl)
        {
            _metaScannerPluginControl = metaScannerPluginControl;

            FavorizeCommand = new RelayCommand(ExecuteFavorize, CanExecuteFavorize);
            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
        }

        internal bool CanExecuteFavorize(object parameter)
        {
            return parameter is MetaEntity;
        }

        internal bool CanExecuteCopy(object parameter)
        {
            return !string.IsNullOrEmpty(parameter?.ToString());
        }

        internal void ExecuteFavorize(object parameter)
        {
            try
            {
                if (parameter is MetaEntity metaEntity)
                {
                    _metaScannerPluginControl.Settings.FavorizeEntity(metaEntity.LogicalName);
                    metaEntity.IsFavorite = _metaScannerPluginControl.Settings.IsFavoriteEntity(metaEntity.LogicalName);
                    _metaScannerPluginControl.SaveSettings();
                    Refresh();
                }
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Favorize Table");
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
                _metaScannerPluginControl.ShowErrorDialog(exception, "Copy");
            }
        }

        internal MetaEntity Find(string logicalName)
        {
            return Entities.FirstOrDefault(me => me.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));
        }

        internal void Select(MetaEntity metaEntity, string attributeLogicalName)
        {
            if (metaEntity != null)
            {
                // A table hidden by the filter or category is shown anyway when navigated to
                if (!IsVisible(metaEntity))
                {
                    _category = AllCategory;
                    NotifyPropertyChanged(nameof(Category));
                    _filter = null;
                    FilterReset?.Invoke(this, EventArgs.Empty);
                    Refresh();
                }
                _selectedEntity = metaEntity;
                NotifyPropertyChanged(nameof(SelectedEntity));
                _metaScannerPluginControl.ShowEntityDetail(metaEntity, attributeLogicalName);
            }
        }

        internal void SetEntities(IEnumerable<MetaEntity> entities)
        {
            Entities = entities.ToList();
            var entitiesViewSource = new CollectionViewSource { Source = Entities }.View;
            entitiesViewSource.Filter = FilterEntity;
            // Favorites first, grouped under their own header
            entitiesViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MetaEntity.IsFavorite)));
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
            var sortPropertyName = SortBy == LogicalNameSort ? nameof(MetaEntity.LogicalName) : nameof(MetaEntity.DisplayName);
            using (entitiesViewSource.DeferRefresh())
            {
                entitiesViewSource.SortDescriptions.Clear();
                entitiesViewSource.SortDescriptions.Add(new SortDescription(nameof(MetaEntity.IsFavorite), ListSortDirection.Descending));
                entitiesViewSource.SortDescriptions.Add(new SortDescription(sortPropertyName, ListSortDirection.Ascending));
            }
        }

        internal void Clear()
        {
            _selectedEntity = null;
            NotifyPropertyChanged(nameof(SelectedEntity));
            SetEntities(Enumerable.Empty<MetaEntity>());
        }

        internal void SetFilter(string filter)
        {
            _filter = filter;
            Refresh();
        }

        internal void NotifySortByChanged()
        {
            // The settings were (re)loaded for another connection
            NotifyPropertyChanged(nameof(SortBy));
            ApplySort(EntitiesViewSource);
        }

        internal void Refresh()
        {
            EntitiesViewSource?.Refresh();
            UpdateCountText();
        }

        private void UpdateCountText()
        {
            var visibleCount = Entities.Count(IsVisible);
            var totalCount = Entities.Count();
            CountText = visibleCount == totalCount ? $"{totalCount} tables" : $"{visibleCount} of {totalCount} tables";
        }

        private bool FilterEntity(object obj)
        {
            return obj is MetaEntity metaEntity && IsVisible(metaEntity);
        }

        private bool IsVisible(MetaEntity metaEntity)
        {
            return IsInCategory(metaEntity) && IsMatch(metaEntity);
        }

        private bool IsInCategory(MetaEntity metaEntity)
        {
            switch (_category)
            {
                case CustomCategory: return metaEntity.IsCustom;
                case SystemCategory: return !metaEntity.IsCustom;
                case ActivityCategory: return metaEntity.IsActivity;
                case AuditCategory: return metaEntity.IsAuditEnabled;
                case IntersectCategory: return metaEntity.IsIntersect;
                default: return true;
            }
        }

        private bool IsMatch(MetaEntity metaEntity)
        {
            return string.IsNullOrEmpty(_filter)
                || Contains(metaEntity.DisplayName, _filter)
                || Contains(metaEntity.LogicalName, _filter)
                || Contains(metaEntity.SchemaName, _filter)
                || metaEntity.ObjectTypeCode?.ToString() == _filter;
        }

        private static bool Contains(string value, string filter)
        {
            return value?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
