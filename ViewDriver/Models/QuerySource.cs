using System;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    internal class QuerySource
    {
        private const string SystemDisplayName = "System";

        public static QuerySource System { get; } = new QuerySource(SystemDisplayName, Guid.Empty);

        public string DisplayName { get;  }
        public Guid UserId { get; }

        public QuerySource(string displayName, Guid userId)
        {
            DisplayName = displayName;
            UserId = userId;
        }
    }
}
