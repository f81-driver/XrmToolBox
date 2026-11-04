using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Input;
using Formula81.XrmToolBox.Tools.MetaScanner.Helpers;
using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Tools.MetaScanner.ViewModels
{
    public class EntityDetailViewModel : ObservableObject
    {
        public const string AllTypes = "All types";
        public const string AllKinds = "All relationships";
        public const int ColumnsTabIndex = 0;

        public static IEnumerable<string> RelationshipKinds { get; } = new[] { AllKinds, MetaRelationship.ManyToOneKind, MetaRelationship.OneToManyKind, MetaRelationship.ManyToManyKind };

        public event EventHandler AttributeFiltersReset;

        private readonly MetaScannerPluginControl _metaScannerPluginControl;

        private EntityDetail _detail;
        public EntityDetail Detail { get => _detail; private set { SetValue(nameof(Detail), value, ref _detail); NotifyPropertyChanged(nameof(HasDetail)); } }

        public bool HasDetail => _detail != null;

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; private set => SetValue(nameof(IsLoading), value, ref _isLoading); }

        private IEnumerable<MetaBadge> _badges;
        public IEnumerable<MetaBadge> Badges { get => _badges; private set => SetValue(nameof(Badges), value, ref _badges); }

        private IEnumerable<MetaProperty> _facts;
        public IEnumerable<MetaProperty> Facts { get => _facts; private set => SetValue(nameof(Facts), value, ref _facts); }

        private int _selectedTabIndex;
        public int SelectedTabIndex { get => _selectedTabIndex; set => SetValue(nameof(SelectedTabIndex), value, ref _selectedTabIndex); }

        private ICollectionView _attributesViewSource;
        public ICollectionView AttributesViewSource { get => _attributesViewSource; private set => SetValue(nameof(AttributesViewSource), value, ref _attributesViewSource); }

        private IEnumerable<string> _attributeTypeNames;
        public IEnumerable<string> AttributeTypeNames { get => _attributeTypeNames; private set => SetValue(nameof(AttributeTypeNames), value, ref _attributeTypeNames); }

        private string _selectedAttributeTypeName = AllTypes;
        public string SelectedAttributeTypeName { get => _selectedAttributeTypeName; set { SetValue(nameof(SelectedAttributeTypeName), value, ref _selectedAttributeTypeName); RefreshAttributes(); } }

        private bool _customAttributesOnly;
        public bool CustomAttributesOnly { get => _customAttributesOnly; set { SetValue(nameof(CustomAttributesOnly), value, ref _customAttributesOnly); RefreshAttributes(); } }

        public bool ShowHelperColumns
        {
            get => _metaScannerPluginControl.Settings.ShowHelperColumns;
            set
            {
                _metaScannerPluginControl.Settings.ShowHelperColumns = value;
                NotifyPropertyChanged(nameof(ShowHelperColumns));
                RefreshAttributes();
            }
        }

        private MetaAttribute _selectedAttribute;
        public MetaAttribute SelectedAttribute { get => _selectedAttribute; set => SetValue(nameof(SelectedAttribute), value, ref _selectedAttribute); }

        private string _attributeCountText;
        public string AttributeCountText { get => _attributeCountText; private set => SetValue(nameof(AttributeCountText), value, ref _attributeCountText); }

        private ICollectionView _relationshipsViewSource;
        public ICollectionView RelationshipsViewSource { get => _relationshipsViewSource; private set => SetValue(nameof(RelationshipsViewSource), value, ref _relationshipsViewSource); }

        private string _selectedRelationshipKind = AllKinds;
        public string SelectedRelationshipKind { get => _selectedRelationshipKind; set { SetValue(nameof(SelectedRelationshipKind), value, ref _selectedRelationshipKind); RefreshRelationships(); } }

        private MetaRelationship _selectedRelationship;
        public MetaRelationship SelectedRelationship { get => _selectedRelationship; set => SetValue(nameof(SelectedRelationship), value, ref _selectedRelationship); }

        private string _relationshipCountText;
        public string RelationshipCountText { get => _relationshipCountText; private set => SetValue(nameof(RelationshipCountText), value, ref _relationshipCountText); }

        private ICollectionView _propertiesViewSource;
        public ICollectionView PropertiesViewSource { get => _propertiesViewSource; private set => SetValue(nameof(PropertiesViewSource), value, ref _propertiesViewSource); }

        public ICommand CopyCommand { get; }
        public ICommand OpenInMakerPortalCommand { get; }
        public ICommand OpenRecordsCommand { get; }
        public ICommand ExportAttributesCommand { get; }
        public ICommand GoToEntityCommand { get; }
        public ICommand BackToScanCommand { get; }

        private string _attributeFilter;
        private string _relationshipFilter;
        private string _propertyFilter;

        public EntityDetailViewModel(MetaScannerPluginControl metaScannerPluginControl)
        {
            _metaScannerPluginControl = metaScannerPluginControl;

            CopyCommand = new RelayCommand(ExecuteCopy, CanExecuteCopy);
            OpenInMakerPortalCommand = new RelayCommand(ExecuteOpenInMakerPortal, CanExecuteOpenInMakerPortal);
            OpenRecordsCommand = new RelayCommand(ExecuteOpenRecords, CanExecuteOpenRecords);
            ExportAttributesCommand = new RelayCommand(ExecuteExportAttributes, CanExecuteExportAttributes);
            GoToEntityCommand = new RelayCommand(ExecuteGoToEntity, CanExecuteGoToEntity);
            BackToScanCommand = new RelayCommand(ExecuteBackToScan, CanExecuteBackToScan);
        }

        internal bool CanExecuteCopy(object parameter)
        {
            return !string.IsNullOrEmpty(parameter?.ToString());
        }

        internal bool CanExecuteOpenInMakerPortal(object parameter)
        {
            return _detail != null
                && _metaScannerPluginControl.CanOpenInMakerPortal(_detail.Entity);
        }

        internal bool CanExecuteOpenRecords(object parameter)
        {
            return _detail != null
                && _metaScannerPluginControl.CanOpenRecords();
        }

        internal bool CanExecuteExportAttributes(object parameter)
        {
            return !_metaScannerPluginControl.IsBusy
                && (_attributesViewSource?.Cast<object>().Any() ?? false);
        }

        internal bool CanExecuteGoToEntity(object parameter)
        {
            return !_metaScannerPluginControl.IsBusy
                && !string.IsNullOrEmpty(GetRelatedEntityName(parameter));
        }

        internal bool CanExecuteBackToScan(object parameter)
        {
            return _metaScannerPluginControl.HasScanResults();
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

        internal void ExecuteOpenInMakerPortal(object parameter)
        {
            try
            {
                _metaScannerPluginControl.OpenInMakerPortal(_detail.Entity);
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Open in Maker Portal");
            }
        }

        internal void ExecuteOpenRecords(object parameter)
        {
            try
            {
                _metaScannerPluginControl.OpenRecords(_detail.Entity);
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Open Records");
            }
        }

        internal void ExecuteExportAttributes(object parameter)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Export Columns",
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"{_detail.Entity.LogicalName}_columns_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (saveFileDialog.ShowDialog() ?? false)
                {
                    CsvExportHelper.ExportAttributes(saveFileDialog.FileName, _attributesViewSource.Cast<MetaAttribute>().ToList());
                }
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Export Columns");
            }
        }

        internal void ExecuteGoToEntity(object parameter)
        {
            try
            {
                _metaScannerPluginControl.GoToEntity(GetRelatedEntityName(parameter));
            }
            catch (Exception exception)
            {
                _metaScannerPluginControl.ShowErrorDialog(exception, "Go to Table");
            }
        }

        internal void ExecuteBackToScan(object parameter)
        {
            _metaScannerPluginControl.ShowScanResults();
        }

        internal void BeginLoad()
        {
            IsLoading = true;
        }

        internal void EndLoad()
        {
            IsLoading = false;
        }

        internal void SetEntityDetail(EntityDetail detail, string attributeLogicalName)
        {
            var isSameEntity = detail != null && _detail?.Entity.LogicalName == detail.Entity.LogicalName;
            Detail = detail;
            if (detail == null)
            {
                Badges = null;
                Facts = null;
                AttributesViewSource = null;
                RelationshipsViewSource = null;
                PropertiesViewSource = null;
                SelectedAttribute = null;
                SelectedRelationship = null;
                UpdateCountTexts();
                return;
            }

            Badges = GetBadges(detail).ToList();
            Facts = GetFacts(detail).ToList();

            AttributeTypeNames = new[] { AllTypes }.Concat(detail.Attributes.Select(ma => ma.TypeName).Distinct().OrderBy(tn => tn)).ToList();
            if (!AttributeTypeNames.Contains(_selectedAttributeTypeName))
            {
                _selectedAttributeTypeName = AllTypes;
                NotifyPropertyChanged(nameof(SelectedAttributeTypeName));
            }
            var attributesViewSource = new CollectionViewSource { Source = detail.Attributes }.View;
            attributesViewSource.Filter = FilterAttribute;
            AttributesViewSource = attributesViewSource;

            var relationshipsViewSource = new CollectionViewSource { Source = detail.Relationships }.View;
            relationshipsViewSource.Filter = FilterRelationship;
            RelationshipsViewSource = relationshipsViewSource;

            var propertiesViewSource = new CollectionViewSource { Source = detail.Properties }.View;
            propertiesViewSource.Filter = FilterProperty;
            PropertiesViewSource = propertiesViewSource;

            SelectedAttribute = string.IsNullOrEmpty(attributeLogicalName) ? null
                : detail.Attributes.FirstOrDefault(ma => ma.LogicalName.Equals(attributeLogicalName, StringComparison.OrdinalIgnoreCase));
            if (SelectedAttribute != null)
            {
                // Make sure the column the user navigated to is not hidden by the column filters
                if (!IsAttributeVisible(SelectedAttribute))
                {
                    ResetAttributeFilters();
                }
                SelectedTabIndex = ColumnsTabIndex;
            }
            else if (!isSameEntity)
            {
                SelectedRelationship = null;
            }
            UpdateCountTexts();
        }

        internal void SetAttributeFilter(string filter)
        {
            _attributeFilter = filter;
            RefreshAttributes();
        }

        internal void SetRelationshipFilter(string filter)
        {
            _relationshipFilter = filter;
            RefreshRelationships();
        }

        internal void SetPropertyFilter(string filter)
        {
            _propertyFilter = filter;
            PropertiesViewSource?.Refresh();
        }

        internal void Clear()
        {
            SetEntityDetail(null, null);
            EndLoad();
        }

        private void ResetAttributeFilters()
        {
            _attributeFilter = null;
            _customAttributesOnly = false;
            _selectedAttributeTypeName = AllTypes;
            if (SelectedAttribute?.IsHelper ?? false)
            {
                _metaScannerPluginControl.Settings.ShowHelperColumns = true;
            }
            NotifyPropertyChanged(nameof(CustomAttributesOnly));
            NotifyPropertyChanged(nameof(SelectedAttributeTypeName));
            NotifyPropertyChanged(nameof(ShowHelperColumns));
            AttributeFiltersReset?.Invoke(this, EventArgs.Empty);
            RefreshAttributes();
        }

        private void RefreshAttributes()
        {
            AttributesViewSource?.Refresh();
            UpdateCountTexts();
        }

        private void RefreshRelationships()
        {
            RelationshipsViewSource?.Refresh();
            UpdateCountTexts();
        }

        private void UpdateCountTexts()
        {
            var attributes = _detail?.Attributes ?? Enumerable.Empty<MetaAttribute>();
            var visibleAttributeCount = attributes.Count(IsAttributeVisible);
            var totalAttributeCount = attributes.Count();
            AttributeCountText = visibleAttributeCount == totalAttributeCount ? $"{totalAttributeCount} columns" : $"{visibleAttributeCount} of {totalAttributeCount} columns";

            var relationships = _detail?.Relationships ?? Enumerable.Empty<MetaRelationship>();
            var visibleRelationshipCount = relationships.Count(IsRelationshipVisible);
            var totalRelationshipCount = relationships.Count();
            RelationshipCountText = visibleRelationshipCount == totalRelationshipCount ? $"{totalRelationshipCount} relationships" : $"{visibleRelationshipCount} of {totalRelationshipCount} relationships";
        }

        private bool FilterAttribute(object obj)
        {
            return obj is MetaAttribute metaAttribute && IsAttributeVisible(metaAttribute);
        }

        private bool IsAttributeVisible(MetaAttribute metaAttribute)
        {
            return (ShowHelperColumns || !metaAttribute.IsHelper)
                && (!_customAttributesOnly || metaAttribute.IsCustom)
                && (_selectedAttributeTypeName == AllTypes || metaAttribute.TypeName == _selectedAttributeTypeName)
                && (string.IsNullOrEmpty(_attributeFilter)
                    || Contains(metaAttribute.DisplayName, _attributeFilter)
                    || Contains(metaAttribute.LogicalName, _attributeFilter)
                    || Contains(metaAttribute.SchemaName, _attributeFilter)
                    || Contains(metaAttribute.Details, _attributeFilter));
        }

        private bool FilterRelationship(object obj)
        {
            return obj is MetaRelationship metaRelationship && IsRelationshipVisible(metaRelationship);
        }

        private bool IsRelationshipVisible(MetaRelationship metaRelationship)
        {
            return (_selectedRelationshipKind == AllKinds || metaRelationship.Kind == _selectedRelationshipKind)
                && (string.IsNullOrEmpty(_relationshipFilter)
                    || Contains(metaRelationship.SchemaName, _relationshipFilter)
                    || Contains(metaRelationship.RelatedEntity, _relationshipFilter)
                    || Contains(metaRelationship.LookupColumn, _relationshipFilter)
                    || Contains(metaRelationship.IntersectEntity, _relationshipFilter));
        }

        private bool FilterProperty(object obj)
        {
            return obj is MetaProperty metaProperty
                && (string.IsNullOrEmpty(_propertyFilter)
                    || Contains(metaProperty.Name, _propertyFilter)
                    || Contains(metaProperty.Value, _propertyFilter));
        }

        private static string GetRelatedEntityName(object parameter)
        {
            return parameter is MetaRelationship metaRelationship ? metaRelationship.RelatedEntity : parameter as string;
        }

        private static IEnumerable<MetaBadge> GetBadges(EntityDetail detail)
        {
            var entity = detail.Entity;
            yield return entity.IsCustom ? new MetaBadge("Custom", MetaBadgeKind.Custom) : new MetaBadge("System", MetaBadgeKind.System);
            yield return entity.IsManaged ? new MetaBadge("Managed", MetaBadgeKind.Managed) : new MetaBadge("Unmanaged", MetaBadgeKind.Unmanaged);
            if (entity.IsActivity)
            {
                yield return new MetaBadge("Activity");
            }
            if (entity.IsIntersect)
            {
                yield return new MetaBadge("Intersect");
            }
            yield return entity.IsAuditEnabled
                ? new MetaBadge("Audit enabled", MetaBadgeKind.AuditEnabled)
                : new MetaBadge("Audit disabled", MetaBadgeKind.AuditDisabled);
        }

        private static IEnumerable<MetaProperty> GetFacts(EntityDetail detail)
        {
            yield return new MetaProperty("Type code", detail.Entity.ObjectTypeCode?.ToString());
            yield return new MetaProperty("Logical name", detail.Entity.LogicalName);
            yield return new MetaProperty("Schema name", detail.Entity.SchemaName);
            yield return new MetaProperty("Entity set name", detail.EntitySetName);
            yield return new MetaProperty("Primary id", detail.PrimaryIdAttribute);
            yield return new MetaProperty("Primary name", detail.PrimaryNameAttribute);
            yield return new MetaProperty("Ownership", detail.OwnershipType, false);
        }

        private static bool Contains(string value, string filter)
        {
            return value?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
