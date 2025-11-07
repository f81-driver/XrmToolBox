using Formula81.XrmToolBox.Shared.XrmParts.Components;
using System;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Models
{
    public class EntityLookupValue : IIconifiable
    {
        public int? ObjectTypeCode { get; }
        public Guid Id { get; }
        public string Name { get; }
        public string EntityLogicalName { get; }
        public string EntityDisplayName { get; }
        public byte[] IconData { get; }

        public EntityLookupValue(int? objectTypeCode, Guid id, string name, string entityLogicalName, string entityDisplayName, byte[] iconData = null)
        {
            ObjectTypeCode = objectTypeCode;
            Id = id;
            Name = name;
            EntityLogicalName = entityLogicalName;
            EntityDisplayName = entityDisplayName;
            IconData = iconData;
        }

        public override string ToString()
        {
            return Name ?? Id.ToString();
        }
    }
}
