using Formula81.XrmToolBox.Libraries.Core.Helpers;

namespace Formula81.XrmToolBox.Libraries.Xrm
{
    public partial class Audit
    {
        public string ActionDisplayName { get => EnumHelper.GetEnumAttributeProperty(Action ?? Audit_Action.Unknown, (OptionSetMetadataAttribute osma) => osma.Name); }
    }
}
