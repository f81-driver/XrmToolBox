using Formula81.XrmToolBox.Libraries.Core.Components;
using Formula81.XrmToolBox.Libraries.Parts.Input;
using Formula81.XrmToolBox.Tools.AuditGoggles.Forms;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.ViewModels
{
    public class AuditEntityViewModel : ObservableObject
    {
        private ICollectionView _auditEntitiesViewSource;
        public ICollectionView AuditEntitiesViewSource { get => _auditEntitiesViewSource; private set => SetValue(nameof(AuditEntitiesViewSource), value, ref _auditEntitiesViewSource); }

        private ICollectionView _favoriteAuditEntitiesViewSource;
        public ICollectionView FavoriteAuditEntitiesViewSource { get => _favoriteAuditEntitiesViewSource; private set => SetValue(nameof(FavoriteAuditEntitiesViewSource), value, ref _favoriteAuditEntitiesViewSource); }

        private bool _hasFavorites;
        public bool HasFavorites { get => _hasFavorites; set => SetValue(nameof(HasFavorites), value, ref _hasFavorites); }

        public IEnumerable<AuditEntity> AuditEntities { get; private set; }

        public ICommand FavorizeCommand { get; }

        private readonly AuditGogglesPluginControl _auditGogglesPluginControl;
        private string _auditEntityFilter;

        public AuditEntityViewModel(AuditGogglesPluginControl auditGogglesPluginControl)
        {
            _auditGogglesPluginControl = auditGogglesPluginControl;

            FavorizeCommand = new RelayCommand(ExecuteFavorizeAuditEntity, CanExecuteFavorizeAuditEntity);
        }

        internal bool CanExecuteFavorizeAuditEntity(object parameter)
        {
            return !_auditGogglesPluginControl.IsBusy;
        }

        internal void ExecuteFavorizeAuditEntity(object parameter)
        {
            try
            {
                if (parameter is AuditEntity auditEntity)
                {
                    _auditGogglesPluginControl.Settings.FavorizeAuditEntities(auditEntity.Name);
                    auditEntity.IsFavorite = _auditGogglesPluginControl.Settings.IsFavoriteAuditEntity(auditEntity.Name);
                    HasFavorites = AuditEntities.Any(ae => ae.IsFavorite);
                    _auditGogglesPluginControl.SaveSettings();
                    Refresh();
                }
            }
            catch (Exception exception)
            {
                _auditGogglesPluginControl.ShowErrorDialog(exception, "Favorize Audit Entity");
            }
        }

        internal bool Contains(string logicalName)
        {
            return AuditEntities.Any(ae => ae.Name.Equals(logicalName));
        }

        internal void Refresh()
        {
            AuditEntitiesViewSource.Refresh();
            FavoriteAuditEntitiesViewSource.Refresh();
        }

        internal void SetAuditEntities(IEnumerable<AuditEntity> auditEntities)
        {
            AuditEntities = auditEntities;
            HasFavorites = AuditEntities.Any(ae => ae.IsFavorite);
            var auditEntitiesViewSource = new CollectionViewSource { Source = auditEntities }.View;
            auditEntitiesViewSource.Filter = FilterAuditEntities;
            auditEntitiesViewSource.SortDescriptions.Add(new SortDescription(nameof(AuditEntity.DisplayName), ListSortDirection.Ascending));

            var favoriteAuditEntitiesViewSource = CollectionViewSource.GetDefaultView(auditEntities);
            favoriteAuditEntitiesViewSource.Filter = FilterFavoriteAuditEntities;
            favoriteAuditEntitiesViewSource.SortDescriptions.Add(new SortDescription(nameof(AuditEntity.DisplayName), ListSortDirection.Ascending));

            AuditEntitiesViewSource = auditEntitiesViewSource;
            FavoriteAuditEntitiesViewSource = favoriteAuditEntitiesViewSource;
        }

        private bool _auditEntityFiltering = false;
        private bool _auditEntityFilterDefer = false;
        internal void SetFilter(string filter)
        {
            if (_auditEntityFiltering)
            {
                _auditEntityFilterDefer = true;
            }
            else
            {
                try
                {
                    _auditEntityFiltering = true;
                    _auditEntityFilter = filter;
                    Refresh();
                    if (_auditEntityFilterDefer)
                    {
                        _auditEntityFilter = filter;
                        Refresh();
                        _auditEntityFilterDefer = false;
                    }
                }
                finally
                {
                    _auditEntityFiltering = false;
                }
            }
        }

        private bool FilterAuditEntities(object obj)
        {
            if (obj is AuditEntity auditEntity)
            {
                return !auditEntity.IsFavorite
                    && auditEntity.IsAuditEnabled
                    && Filter(auditEntity);
            }
            return false;
        }

        private bool FilterFavoriteAuditEntities(object obj)
        {
            if (obj is AuditEntity auditEntity)
            {
                return auditEntity.IsFavorite
                    && Filter(auditEntity);
            }
            return false;
        }

        private bool Filter(AuditEntity auditEntity)
        {
            var noFilter = string.IsNullOrEmpty(_auditEntityFilter);
            return noFilter
                || (!noFilter
                    && auditEntity.DisplayName.IndexOf(_auditEntityFilter, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
