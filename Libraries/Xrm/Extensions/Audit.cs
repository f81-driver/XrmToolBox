namespace Formula81.XrmToolBox.Libraries.Xrm
{
    public partial class Audit
    {
        public class Columns
        {
            public static string AuditId { get => GetColumnName<Audit>(a => a.AuditId); }
            public static string Action { get => GetColumnName<Audit>(a => a.Action); }
            public static string AttributeMask { get => GetColumnName<Audit>(a => a.AttributeMask); }
            public static string ChangeData { get => GetColumnName<Audit>(a => a.ChangeData); }
            public static string CreatedOn { get => GetColumnName<Audit>(a => a.CreatedOn); }
            public static string ObjectId { get => GetColumnName<Audit>(a => a.ObjectId); }
            public static string Operation { get => GetColumnName<Audit>(a => a.Operation); }
            public static string UserId { get => GetColumnName<Audit>(a => a.UserId); }
        }
    }
}
