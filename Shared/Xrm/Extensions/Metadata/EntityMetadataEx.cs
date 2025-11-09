using Microsoft.Xrm.Sdk.Metadata;

namespace Formula81.XrmToolBox.Shared.Xrm.Extensions.Metadata
{
    public static class EntityMetadataEx
    {
        public static string GetDisplayLabel(this EntityMetadata entityMetadata)
        {
            return entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.LogicalName;
        }
    }
}
