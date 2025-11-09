using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Mocks
{
    internal class EntityAuditColumnsWindowAuditEntityListBoxItemsSource : IEnumerable<EntityAuditColumnsItem>
    {
        private readonly ReadOnlyCollection<EntityAuditColumnsItem> _entityAuditColumnsItemCollection;

        public EntityAuditColumnsWindowAuditEntityListBoxItemsSource()
        {
            _entityAuditColumnsItemCollection = new List<EntityAuditColumnsItem>
            {
                new EntityAuditColumnsItem("account", "Account", new List<EntityAuditColumnsItemColumn>{ new EntityAuditColumnsItemColumn("name", "Name") { IsChecked = true} }),
                new EntityAuditColumnsItem("nrq_serviceorder", "Service Order", new List<EntityAuditColumnsItemColumn>{ new EntityAuditColumnsItemColumn("name", "Name") { IsChecked = true} })
            }.AsReadOnly();
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

    internal class EntityAuditColumnsWindowAuditEntityColumnsListBoxItemsSource : IEnumerable<EntityAuditColumnsItemColumn>
    {
        private readonly ReadOnlyCollection<EntityAuditColumnsItemColumn> _entityAuditColumnsItemCollection;

        public EntityAuditColumnsWindowAuditEntityColumnsListBoxItemsSource()
        {
            _entityAuditColumnsItemCollection = new List<EntityAuditColumnsItemColumn>
            {
                new EntityAuditColumnsItemColumn("name", "Name") { IsChecked = true},
                new EntityAuditColumnsItemColumn("value", "Value") { IsChecked = false }
            }.AsReadOnly();
        }

        public IEnumerator<EntityAuditColumnsItemColumn> GetEnumerator()
        {
            return _entityAuditColumnsItemCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
