namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class MetaKey
    {
        public string DisplayName { get; }
        public string LogicalName { get; }
        public string Columns { get; }
        public string Status { get; }

        public MetaKey(string displayName, string logicalName, string columns, string status)
        {
            DisplayName = displayName;
            LogicalName = logicalName;
            Columns = columns;
            Status = status;
        }
    }
}
