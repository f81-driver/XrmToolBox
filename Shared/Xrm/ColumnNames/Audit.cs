namespace Formula81.XrmToolBox.Shared.Xrm
{
    public partial class Audit
    {
        public class ColumnNames
        {
            public static readonly string AuditId = GetColumnName<Audit>(a => a.AuditId);

            public static readonly string Action = GetColumnName<Audit>(a => a.Action);
            public static readonly string AttributeMask = GetColumnName<Audit>(a => a.AttributeMask);
            public static readonly string ChangeData = GetColumnName<Audit>(a => a.ChangeData);
            public static readonly string CreatedOn = GetColumnName<Audit>(a => a.CreatedOn);
            public static readonly string ObjectId = GetColumnName<Audit>(a => a.ObjectId);
            public static readonly string Operation = GetColumnName<Audit>(a => a.Operation);
            public static readonly string UserId = GetColumnName<Audit>(a => a.UserId);

            public const string ObjectTypeCode = "objecttypecode";
        }
    }
}
