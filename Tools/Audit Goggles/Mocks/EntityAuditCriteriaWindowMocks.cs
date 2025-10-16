using Formula81.XrmToolBox.Libraries.Parts.Components;
using Formula81.XrmToolBox.Tools.AuditGoggles.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Mocks
{
    internal class EntityAuditCriteriaWindowAuditEntityListBoxItemsSource : IEnumerable<EntityAuditColumnsItem>
    {
        private readonly ReadOnlyCollection<EntityAuditColumnsItem> _entityAuditColumnsItemCollection;

        public EntityAuditCriteriaWindowAuditEntityListBoxItemsSource()
        {
            _entityAuditColumnsItemCollection = new List<EntityAuditColumnsItem> {
                new EntityAuditColumnsItem("account", "Account", new List<CheckableItem<string>>{ new CheckableItem<string>("name", "Name") { IsChecked = true} }),
                new EntityAuditColumnsItem("nrq_serviceorder", "Service Order", new List<CheckableItem<string>>{ new CheckableItem<string>("name", "Name") { IsChecked = true} })
            }
                .AsReadOnly();
        }

        public IEnumerator<EntityAuditColumnsItem> GetEnumerator()
        {
            return _entityAuditColumnsItemCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
