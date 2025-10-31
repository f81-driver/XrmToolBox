using Microsoft.Xrm.Sdk.Metadata;

namespace Formula81.XrmToolBox.Libraries.Xrm.Extensions.Metadata
{
    public static class AttributeMetadataEx
    {
        public static string GetDisplayLabel(this AttributeMetadata attributeMetadata)
        {
            return attributeMetadata.DisplayName?.UserLocalizedLabel?.Label ?? attributeMetadata.LogicalName;
        }
    }
}
