using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    public enum AttributeKind
    {
        Other,
        Text,
        Number,
        Date,
        Lookup,
        Choice,
        MultiChoice,
        Boolean,
        Identifier
    }

    public class ViewAttribute
    {
        public string EntityLogicalName { get; }
        public string LogicalName { get; }
        public string DisplayName { get; }
        public string TypeName { get; }
        public AttributeKind Kind { get; }
        public IReadOnlyList<ViewOption> Options { get; }
        public IReadOnlyList<string> Targets { get; }
        public bool IsPrimaryId { get; }
        public bool IsPrimaryName { get; }
        public bool IsValidForGrid { get; }
        public bool IsValidForFilter { get; }

        public bool HasOptions => Options.Count > 0;

        public string ToolTip => $"{DisplayName}\n{EntityLogicalName}.{LogicalName}\n{TypeName}";

        public ViewAttribute(string entityLogicalName, string logicalName, string displayName, string typeName, AttributeKind kind,
            IReadOnlyList<ViewOption> options, IReadOnlyList<string> targets, bool isPrimaryId, bool isPrimaryName,
            bool isValidForGrid, bool isValidForFilter)
        {
            EntityLogicalName = entityLogicalName;
            LogicalName = logicalName;
            DisplayName = displayName;
            TypeName = typeName;
            Kind = kind;
            Options = options ?? new ViewOption[0];
            Targets = targets ?? new string[0];
            IsPrimaryId = isPrimaryId;
            IsPrimaryName = isPrimaryName;
            IsValidForGrid = isValidForGrid;
            IsValidForFilter = isValidForFilter;
        }
    }
}
