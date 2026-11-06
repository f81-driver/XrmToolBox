namespace Formula81.XrmToolBox.Shared.Xrm
{
    public partial class SystemUser
    {
        public class ColumnNames
        {
            public static readonly string SystemUserId = GetColumnName<SystemUser>(su => su.SystemUserId);

            public static readonly string FullName = GetColumnName<SystemUser>(su => su.FullName);
            public static readonly string DomainName = GetColumnName<SystemUser>(su => su.DomainName);
            public static readonly string IsDisabled = GetColumnName<SystemUser>(su => su.IsDisabled);
            public static readonly string AccessMode = GetColumnName<SystemUser>(su => su.AccessMode);
        }
    }
}
