using Formula81.XrmToolBox.Shared.Core.Components;
using Formula81.XrmToolBox.Shared.Parts.Components;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Models
{
    public class EntityAuditColumnsItemColumn : CheckableItem<string>
    {
        public EntityAuditColumnsItemColumn(string name, string displayName) : base(name, displayName) { }
    }

    public class EntityAuditColumnsItem : ObservableObject
    {
        public string Name { get; }
        public string DisplayName { get; }
        public IList<EntityAuditColumnsItemColumn> Columns { get; }

        private bool _allColumns;
        public bool AllColumns { get => _allColumns; set { _allColumns = value; UpdateColumnCount(); } }

        private string _columnCount;
        public string ColumnCount { get => _columnCount; private set => SetValue(nameof(ColumnCount), value, ref _columnCount); }

        public EntityAuditColumnsItem(string name, string displayName, IEnumerable<EntityAuditColumnsItemColumn> columns)
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
            ColumnCount = AllColumns ? "*" : Columns.Count(c => c.IsChecked).ToString();
        }
    }
}
