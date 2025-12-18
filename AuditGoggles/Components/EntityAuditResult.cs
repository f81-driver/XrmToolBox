using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Components
{
    internal class EntityAuditResult
    {
        public static EntityAuditResult Empty = new EntityAuditResult(Enumerable.Empty<EntityAudit>(), string.Empty, true, 0, false);

        public IEnumerable<EntityAudit> EntityAudits { get; }
        public string PagingCookie { get; }
        public bool MoreRecords { get; }
        public int TotalRecordCount { get; }
        public bool TotalRecordCountLimitExceeded { get; }

        public EntityAuditResult(IEnumerable<EntityAudit> entityAudits, string pagingCookie, bool moreRecords, int totalRecordCount, bool totalRecordCountLimitExceeded)
        {
            EntityAudits = entityAudits;
            PagingCookie = pagingCookie;
            MoreRecords = moreRecords;
            TotalRecordCount = totalRecordCount;
            TotalRecordCountLimitExceeded = totalRecordCountLimitExceeded;
        }
    }
}
