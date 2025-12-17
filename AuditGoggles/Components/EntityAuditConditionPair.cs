using System;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Components
{
    internal class EntityAuditConditionPair
    {
        public IEnumerable<Guid> ObjectIds { get; }
        public IEnumerable<int> AttributeMasks { get; }

        public EntityAuditConditionPair(IEnumerable<Guid> objectIds, IEnumerable<int> attributeMasks)
        {
            ObjectIds = objectIds;
            AttributeMasks = attributeMasks;
        }
    }
}
