using Formula81.XrmToolBox.Libraries.Core.Components;
using Formula81.XrmToolBox.Libraries.Parts.Components;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Windows
{
    public class EntityAuditColumnsItem : ObservableObject
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }

        public IList<CheckableItem<string>> Columns { get; set; }

        public bool AllColumns { get; set; }

        private int? _columnCount;
        public int? ColumnCount { get => _columnCount; private set => SetValue(nameof(ColumnCount), value, ref _columnCount); }

        public EntityAuditColumnsItem(string name, string displayName, IEnumerable<CheckableItem<string>> columns)
        {
            Name = name;
            DisplayName = displayName;
            Columns = columns.ToList();
            foreach (var column in Columns)
            {
                column.PropertyChanged += Column_PropertyChanged;
            }
            UpdateColumnCount();
        }

        private void Column_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CheckableItem<string>.IsChecked):
                    UpdateColumnCount();
                    break;
            }
        }

        private void UpdateColumnCount()
        {
            ColumnCount = AllColumns ? null : (int?)Columns.Count(c => c.IsChecked);
        }
    }

    public partial class EntityAuditColumnsWindow : Window
    {
        private IEnumerable<EntityAuditColumnsItem> _entityAuditColumnsItems;

        public EntityAuditColumnsWindow()
        {
            InitializeComponent();
        }

        public void SetSource(IEnumerable<EntityMetadata> entityMetadatas, IDictionary<string, ColumnSet> columns)
        {
            _entityAuditColumnsItems = entityMetadatas.Select(em => new EntityAuditColumnsItem(em.LogicalName, em.DisplayName?.UserLocalizedLabel?.Label ?? em.LogicalName,
                    em.Attributes.Where(am => am.IsAuditEnabled?.Value ?? false)
                        .Select(am => new CheckableItem<string>(am.LogicalName, am.DisplayName?.UserLocalizedLabel?.Label ?? am.LogicalName)
                        {
                            IsChecked = columns.TryGetValue(em.LogicalName, out ColumnSet columnSet) && !columnSet.AllColumns && columnSet.Columns.Contains(am.LogicalName)
                        })))
                .ToList();
            AuditEntityListBox.ItemsSource = _entityAuditColumnsItems;
        }

        public IDictionary<string, ColumnSet> Get()
        {
            return _entityAuditColumnsItems.ToDictionary(eaci => eaci.Name, eaci => new ColumnSet(eaci.Columns.Where(c => c.IsChecked)
                    .Select(c=> c.Value).ToArray()));
        }
    }
}
