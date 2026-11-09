namespace Formula81.XrmToolBox.Shared.Xrm
{
    public partial class Team
    {
        public class ColumnNames
        {
            public static readonly string TeamId = GetColumnName<Team>(t => t.TeamId);

            public static readonly string Name = GetColumnName<Team>(t => t.Name);
            public static readonly string TeamType = GetColumnName<Team>(t => t.TeamType);
            public static readonly string IsDefault = GetColumnName<Team>(t => t.IsDefault);
        }
    }
}
