using Formula81.XrmToolBox.Shared.Parts.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Shared.XrmParts.Components;
using Formula81.XrmToolBox.Shared.XrmParts.Events;
using Formula81.XrmToolBox.Shared.XrmParts.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Shared.XrmParts.Controls
{
    public partial class EntityListControl : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<EntityItem>), typeof(EntityListControl), new PropertyMetadata(Enumerable.Empty<EntityItem>(), OnItemsSourcePropertyChanged));
        public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(nameof(SelectionMode), typeof(EntityListSelectionMode), typeof(EntityListControl), new PropertyMetadata(EntityListSelectionMode.None));
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(EntityItem), typeof(EntityListControl), new PropertyMetadata(null));

        public IEnumerable<EntityItem> ItemsSource { get => (IEnumerable<EntityItem>)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
        public EntityListSelectionMode SelectionMode { get => (EntityListSelectionMode)GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }
        public EntityItem SelectedItem { get => (EntityItem)GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

        public event EntityInvokedEvent EntityInvoked;
        public event FavoriteEntitiesChangedEvent FavoriteEntitiesChanged;

        public IList<EntityItem> Entities { get; private set; }
        public ObservableCollection<EntityItem> SelectedItems { get; }

        private ICollectionView _entitiesViewSource;
        private ICollectionView _favoriteEntitiesViewSource;

        public ICommand ClickCommand { get; }
        public ICommand FavorizeCommand { get; }

        private IDialogService DialogService { get; set; }
        private string _entityFilter;

        public EntityListControl()
        {
            SelectedItems = new ObservableCollection<EntityItem>();

            FavorizeCommand = new RelayCommand(ExecuteFavorize, CanExecuteFavorize);
            ClickCommand = new RelayCommand(ExecuteClickEntity, CanExecuteClickEntity);

            InitializeComponent();

            EntityFilterInputControl.FilterChanged += EntityFilterTextBox_FilterChanged;
            FavoriteEntitiesChanged += EntityListControl_FavoriteEntitiesChanged;
        }

        internal bool CanExecuteClickEntity(object parameter)
        {
            return true;
        }

        internal bool CanExecuteFavorize(object parameter)
        {
            return true;
        }

        internal void ExecuteClickEntity(object parameter)
        {
            if (parameter is EntityItem entity)
            {
                switch (SelectionMode)
                {
                    case EntityListSelectionMode.None:
                        EntityInvoked?.Invoke(this, entity);
                        break;

                    case EntityListSelectionMode.Single:
                        SelectedItems.Clear();
                        SelectedItems.Add(entity);
                        SelectedItem = entity;
                        break;

                    case EntityListSelectionMode.Multiple:
                        if (SelectedItems.Contains(entity))
                            SelectedItems.Remove(entity);
                        else
                            SelectedItems.Add(entity);
                        break;
                }
            }
        }

        internal void ExecuteFavorize(object parameter)
        {
            try
            {
                if (parameter is EntityItem entity)
                {
                    entity.IsFavorite = !entity.IsFavorite;
                }
            }
            catch (Exception exception)
            {
                DialogService?.ShowError(exception, "Favorize");
            }
        }

        internal void SetEntities(IEnumerable<EntityItem> entities)
        {
            try
            {
                Entities = entities?.ToList() ?? new List<EntityItem>();
                foreach (var entity in Entities)
                {
                    entity.PropertyChanged += Entity_PropertyChanged;
                }

                var entitiesViewSource = new CollectionViewSource { Source = Entities }.View;
                entitiesViewSource.Filter = FilterEntities;
                entitiesViewSource.SortDescriptions.Add(new SortDescription(nameof(EntityItem.DisplayName), ListSortDirection.Ascending));

                var favoriteEntitiesViewSource = new CollectionViewSource { Source = Entities }.View;
                favoriteEntitiesViewSource.Filter = FilterFavoriteEntities;
                favoriteEntitiesViewSource.SortDescriptions.Add(new SortDescription(nameof(EntityItem.DisplayName), ListSortDirection.Ascending));

                _entitiesViewSource = entitiesViewSource;
                _favoriteEntitiesViewSource = favoriteEntitiesViewSource;

                EntityItemsControl.ItemsSource = _entitiesViewSource;
                FavoriteEntityItemsControl.ItemsSource = _favoriteEntitiesViewSource;
            }
            catch (Exception exception)
            {
                DialogService?.ShowError(exception, "Set Entities");
            }
        }

        private bool _entityFiltering = false;
        private bool _entityFilterDefer = false;
        internal void SetFilter(string filter)
        {
            if (_entityFiltering)
            {
                _entityFilterDefer = true;
            }
            else
            {
                try
                {
                    _entityFiltering = true;
                    _entityFilter = filter;
                    Refresh();
                    if (_entityFilterDefer)
                    {
                        _entityFilter = filter;
                        Refresh();
                        _entityFilterDefer = false;
                    }
                }
                finally
                {
                    _entityFiltering = false;
                }
            }
        }

        private void Refresh()
        {
            _entitiesViewSource.Refresh();
            _favoriteEntitiesViewSource.Refresh();
        }

        private bool FilterEntities(object obj)
        {
            if (obj is EntityItem entity)
            {
                return !entity.IsFavorite
                    && Filter(entity);
            }
            return false;
        }

        private bool FilterFavoriteEntities(object obj)
        {
            if (obj is EntityItem entity)
            {
                return entity.IsFavorite
                    && Filter(entity);
            }
            return false;
        }

        private bool Filter(EntityItem entity)
        {
            var noFilter = string.IsNullOrEmpty(_entityFilter);
            return noFilter
                || (!noFilter
                    && entity.DisplayName.IndexOf(_entityFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void EntityFilterTextBox_FilterChanged(string filter)
        {
            _entityFilter = filter;
            Refresh();
        }

        private void EntityListControl_FavoriteEntitiesChanged(object sender, FavoriteEntitiesChangedArgs args)
        {
            var showFavorites = args.Entities.Any();
            FavoriteEntityItemsControl.Visibility = showFavorites ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Entity_PropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(EntityItem.IsFavorite):
                    Refresh();
                    FavoriteEntitiesChanged?.Invoke(this, new FavoriteEntitiesChangedArgs(Entities.Where(e => e.IsFavorite).ToList()));
                    break;

            }
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EntityListControl entityListControl)
            {
                entityListControl.SetEntities(e.NewValue as IEnumerable<EntityItem>);
            }
        }
    }
}
