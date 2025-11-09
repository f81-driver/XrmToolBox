using Formula81.XrmToolBox.Shared.Core.Helpers;

namespace Formula81.XrmToolBox.Shared.Xrm
{
    public partial class Audit
    {
        public string ActionDisplayName { get => EnumHelper.GetEnumAttributeProperty(Action ?? Audit_Action.Unknown, (OptionSetMetadataAttribute osma) => osma.Name); }
    }
}
