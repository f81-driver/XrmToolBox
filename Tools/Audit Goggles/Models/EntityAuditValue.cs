using Microsoft.Xrm.Sdk;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Models
{
    public class EntityAuditValue
    {
        public object Value { get; }
        public object DisplayValue { get; }

        public bool IsEntityReference { get => Value is EntityReference; }

        public EntityAuditValue(object value, object displayValue)
        {
            Value = value;
            DisplayValue = displayValue;
        }
    }
}
