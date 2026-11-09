namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// Source of the available columns: the table of the view itself, or a table it looks up (many-to-one)
    /// </summary>
    public class RelatedTable
    {
        public bool IsRoot { get; }
        public string LookupAttribute { get; }
        public string LookupDisplayName { get; }
        public string EntityLogicalName { get; }
        public string EntityDisplayName { get; }
        // Primary id of the related table, the "from" of the link-entity
        public string PrimaryIdAttribute { get; }

        public string DisplayText => IsRoot ? $"{EntityDisplayName} (this table)" : $"{LookupDisplayName} ({EntityDisplayName})";
        public string ToolTip => IsRoot ? EntityLogicalName : $"{LookupAttribute} → {EntityLogicalName}";

        private RelatedTable(bool isRoot, string lookupAttribute, string lookupDisplayName, string entityLogicalName, string entityDisplayName, string primaryIdAttribute)
        {
            IsRoot = isRoot;
            LookupAttribute = lookupAttribute;
            LookupDisplayName = lookupDisplayName;
            EntityLogicalName = entityLogicalName;
            EntityDisplayName = entityDisplayName;
            PrimaryIdAttribute = primaryIdAttribute;
        }

        public static RelatedTable CreateRoot(string entityLogicalName, string entityDisplayName, string primaryIdAttribute)
        {
            return new RelatedTable(true, null, null, entityLogicalName, entityDisplayName, primaryIdAttribute);
        }

        public static RelatedTable CreateRelated(string lookupAttribute, string lookupDisplayName, string entityLogicalName, string entityDisplayName, string primaryIdAttribute)
        {
            return new RelatedTable(false, lookupAttribute, lookupDisplayName, entityLogicalName, entityDisplayName, primaryIdAttribute);
        }
    }
}
