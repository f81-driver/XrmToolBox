namespace Formula81.XrmToolBox.Shared.Xrm
{
    public partial class SavedQuery
    {
        public class ColumnNames
        {
            public static readonly string SavedQueryId = GetColumnName<SavedQuery>(sq => sq.SavedQueryId);

            public static readonly string Name = GetColumnName<SavedQuery>(sq => sq.Name);
            public static readonly string Description = GetColumnName<SavedQuery>(sq => sq.Description);
            public static readonly string FetchXml = GetColumnName<SavedQuery>(sq => sq.FetchXml);
            public static readonly string LayoutXml = GetColumnName<SavedQuery>(sq => sq.LayoutXml);
            public static readonly string QueryType = GetColumnName<SavedQuery>(sq => sq.QueryType);
            public static readonly string ReturnedTypeCode = GetColumnName<SavedQuery>(sq => sq.ReturnedTypeCode);
            public static readonly string IsDefault = GetColumnName<SavedQuery>(sq => sq.IsDefault);
            public static readonly string StateCode = GetColumnName<SavedQuery>(sq => sq.StateCode);
            public static readonly string ModifiedOn = GetColumnName<SavedQuery>(sq => sq.ModifiedOn);
        }
    }
}
