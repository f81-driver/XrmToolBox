using Formula81.XrmToolBox.Shared.Parts.Components;
using Formula81.XrmToolBox.Shared.Xrm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Mocks
{
    internal class EntityAuditFilterOperationListBoxItemsSource : IEnumerable<CheckableEnumItem>
    {
        private readonly ReadOnlyCollection<CheckableEnumItem> _auditOperationItemCollection;

        public EntityAuditFilterOperationListBoxItemsSource()
        {
            var auditOperationType = typeof(Audit_Operation);
            _auditOperationItemCollection = Enum.GetValues(auditOperationType)
                .Cast<Audit_Operation>()
                .Select(ao => new CheckableEnumItem((int)ao, auditOperationType.GetField(ao.ToString())
                    .GetCustomAttribute<OptionSetMetadataAttribute>()?.Name))
                .ToList()
                .AsReadOnly();
        }

        public IEnumerator<CheckableEnumItem> GetEnumerator()
        {
            return _auditOperationItemCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
