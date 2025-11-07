using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Models
{
    public class EntityAuditDetail
    {
        public string ChangedFieldName { get; }
        public EntityAuditValue OldValue { get; }
        public EntityAuditValue NewValue { get; }

        public IEnumerable<EntityAuditValue> Values { get => new EntityAuditValue[] { OldValue, NewValue }; }

        public EntityAuditDetail(string changedFieldName, EntityAuditValue oldValue, EntityAuditValue newValue)
        {
            ChangedFieldName = changedFieldName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public IEnumerable<EntityAuditValue> GetValues()
        {
            yield return OldValue;
            yield return NewValue;
        }
    }
}
