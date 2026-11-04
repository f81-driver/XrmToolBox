using Formula81.XrmToolBox.Tools.MetaScanner.Helpers;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class MetaAttribute
    {
        private readonly Lazy<IEnumerable<MetaProperty>> _properties;

        public AttributeMetadata Metadata { get; }

        public string LogicalName { get; }
        public string DisplayName { get; }
        public string SchemaName { get; }
        public string Description { get; }
        public string TypeName { get; }
        public string Details { get; }
        public string RequiredLevel { get; }
        public string Source { get; }
        public bool IsCustom { get; }
        public bool IsManaged { get; }
        public bool IsPrimaryId { get; }
        public bool IsPrimaryName { get; }
        public bool IsAuditEnabled { get; }
        public bool IsSearchable { get; }
        public bool IsSecured { get; }
        public bool IsHelper { get; }
        public int? MaxLength { get; }
        public IEnumerable<MetaOption> Options { get; }

        public bool HasOptions => Options != null;
        public IEnumerable<MetaProperty> Properties => _properties.Value;

        public MetaAttribute(AttributeMetadata attributeMetadata)
        {
            Metadata = attributeMetadata;
            LogicalName = attributeMetadata.LogicalName;
            DisplayName = attributeMetadata.DisplayName?.UserLocalizedLabel?.Label ?? attributeMetadata.LogicalName;
            SchemaName = attributeMetadata.SchemaName;
            Description = attributeMetadata.Description?.UserLocalizedLabel?.Label;
            TypeName = MetadataHelper.GetTypeName(attributeMetadata);
            Details = MetadataHelper.GetDetails(attributeMetadata);
            RequiredLevel = MetadataHelper.GetRequiredLevelName(attributeMetadata.RequiredLevel?.Value);
            Source = MetadataHelper.GetSourceName(attributeMetadata.SourceType);
            IsCustom = attributeMetadata.IsCustomAttribute ?? false;
            IsManaged = attributeMetadata.IsManaged ?? false;
            IsPrimaryId = attributeMetadata.IsPrimaryId ?? false;
            IsPrimaryName = attributeMetadata.IsPrimaryName ?? false;
            IsAuditEnabled = attributeMetadata.IsAuditEnabled?.Value ?? false;
            IsSearchable = attributeMetadata.IsValidForAdvancedFind?.Value ?? false;
            IsSecured = attributeMetadata.IsSecured ?? false;
            // Name and yomi columns of lookups, choices and owners only mirror another column
            IsHelper = !string.IsNullOrEmpty(attributeMetadata.AttributeOf);
            MaxLength = MetadataHelper.GetMaxLength(attributeMetadata);
            Options = MetadataHelper.GetOptions(attributeMetadata);
            _properties = new Lazy<IEnumerable<MetaProperty>>(() => PropertyHelper.GetProperties(attributeMetadata));
        }
    }
}
