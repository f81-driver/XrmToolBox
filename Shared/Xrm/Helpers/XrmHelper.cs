using Microsoft.Xrm.Sdk;
using System;

namespace Formula81.XrmToolBox.Shared.Xrm.Helpers
{
    public static class XrmHelper
    {
        public static EntityReference FromCommaSeparated(string value)
        {
            EntityReference entityRef = null;
            if (!string.IsNullOrEmpty(value))
            {
                var values = value.Split(',');
                if (Guid.TryParse(values[0], out var id0))
                {
                    entityRef = new EntityReference(values[1], id0);
                }
                else
                {
                    if (Guid.TryParse(values[1], out var id1))
                    {
                        entityRef = new EntityReference(values[0], id1);
                    }
                }
            }
            return entityRef;
        }
    }
}
