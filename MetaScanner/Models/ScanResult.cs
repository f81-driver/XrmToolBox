using Formula81.XrmToolBox.Shared.XrmParts.Components;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class ScanResult : IIconifiable
    {
        public const string TableKind = "Table";
        public const string ColumnKind = "Column";
        public const string ChoiceKind = "Choice value";

        public string Kind { get; }
        public MetaEntity Entity { get; }
        public string DisplayName { get; }
        public string LogicalName { get; }
        public string TypeName { get; }
        public string MatchedOn { get; }
        public string MatchedText { get; }
        public string AttributeLogicalName { get; }

        public int? ObjectTypeCode => Entity?.ObjectTypeCode;
        public byte[] IconData => Entity?.IconData;

        public ScanResult(string kind, MetaEntity entity, string displayName, string logicalName, string typeName, string matchedOn, string matchedText, string attributeLogicalName)
        {
            Kind = kind;
            Entity = entity;
            DisplayName = displayName;
            LogicalName = logicalName;
            TypeName = typeName;
            MatchedOn = matchedOn;
            MatchedText = matchedText;
            AttributeLogicalName = attributeLogicalName;
        }
    }
}
