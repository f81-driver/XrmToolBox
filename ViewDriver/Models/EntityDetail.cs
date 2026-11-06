using System;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    public class EntityDetail
    {
        public string LogicalName { get; }
        public string DisplayName { get; }
        public int? ObjectTypeCode { get; }
        public string PrimaryIdAttribute { get; }
        public string PrimaryNameAttribute { get; }
        public IReadOnlyList<ViewAttribute> Attributes { get; }
        public IReadOnlyList<ViewAttribute> GridAttributes { get; }
        public IReadOnlyList<ViewAttribute> FilterAttributes { get; }
        // Many-to-one: tables whose columns can be added to a view of this table
        public IReadOnlyList<RelatedTable> RelatedTables { get; }

        private readonly Dictionary<string, ViewAttribute> _attributeMap;

        public EntityDetail(string logicalName, string displayName, int? objectTypeCode, string primaryIdAttribute, string primaryNameAttribute,
            IEnumerable<ViewAttribute> attributes, IEnumerable<RelatedTable> relatedTables)
        {
            LogicalName = logicalName;
            DisplayName = displayName;
            ObjectTypeCode = objectTypeCode;
            PrimaryIdAttribute = primaryIdAttribute;
            PrimaryNameAttribute = primaryNameAttribute;
            Attributes = attributes.OrderBy(va => va.DisplayName).ToList();
            GridAttributes = Attributes.Where(va => va.IsValidForGrid).ToList();
            FilterAttributes = Attributes.Where(va => va.IsValidForFilter).ToList();
            RelatedTables = relatedTables.ToList();
            _attributeMap = Attributes.ToDictionary(va => va.LogicalName, StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetAttribute(string logicalName, out ViewAttribute viewAttribute)
        {
            viewAttribute = null;
            return !string.IsNullOrEmpty(logicalName)
                && _attributeMap.TryGetValue(logicalName, out viewAttribute);
        }
    }
}
