using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Components
{
    internal class EntityAuditResult
    {
        public IEnumerable<EntityAudit> EntityAudits { get; }
        public string PagingCookie { get; }
        public bool MoreRecords { get; }

        public EntityAuditResult(IEnumerable<EntityAudit> entityAudits, string pagingCookie, bool moreRecords)
        {
            EntityAudits = entityAudits;
            PagingCookie = pagingCookie;
            MoreRecords = moreRecords;
        }
    }
}
