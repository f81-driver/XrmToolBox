using Microsoft.Xrm.Sdk.Metadata;

namespace Formula81.XrmToolBox.Shared.Xrm.Extensions.Metadata
{
    public static class BooleanAttributeMetadataEx
    {
        public static string GetDisplayValue(this BooleanAttributeMetadata booleanAttributeMetadata, bool? value)
        {
            if (value.HasValue)
            {
                var option = value.Value ? booleanAttributeMetadata.OptionSet.TrueOption : booleanAttributeMetadata.OptionSet.FalseOption;
                return option.Label?.UserLocalizedLabel?.Label ?? value.Value.ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
