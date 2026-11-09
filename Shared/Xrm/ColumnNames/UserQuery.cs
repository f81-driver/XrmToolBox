namespace Formula81.XrmToolBox.Shared.Xrm
{
    public partial class UserQuery
    {
        public class ColumnNames
        {
            public static readonly string UserQueryId = GetColumnName<UserQuery>(uq => uq.UserQueryId);

            public static readonly string Name = GetColumnName<UserQuery>(uq => uq.Name);
            public static readonly string Description = GetColumnName<UserQuery>(uq => uq.Description);
            public static readonly string FetchXml = GetColumnName<UserQuery>(uq => uq.FetchXml);
            public static readonly string LayoutXml = GetColumnName<UserQuery>(uq => uq.LayoutXml);
            public static readonly string QueryType = GetColumnName<UserQuery>(uq => uq.QueryType);
            public static readonly string ReturnedTypeCode = GetColumnName<UserQuery>(uq => uq.ReturnedTypeCode);
            public static readonly string OwnerId = GetColumnName<UserQuery>(uq => uq.OwnerId);
            public static readonly string StateCode = GetColumnName<UserQuery>(uq => uq.StateCode);
            public static readonly string ModifiedOn = GetColumnName<UserQuery>(uq => uq.ModifiedOn);
        }
    }
}
