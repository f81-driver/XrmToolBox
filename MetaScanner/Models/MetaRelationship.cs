using Formula81.XrmToolBox.Tools.MetaScanner.Helpers;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Models
{
    public class MetaRelationship
    {
        public const string OneToManyKind = "One-to-many";
        public const string ManyToOneKind = "Many-to-one";
        public const string ManyToManyKind = "Many-to-many";

        private readonly Lazy<IEnumerable<MetaProperty>> _properties;

        public string Kind { get; }
        public string SchemaName { get; }
        public string RelatedEntity { get; }
        public string LookupColumn { get; }
        public string IntersectEntity { get; }
        public string Behavior { get; }
        public bool IsCustom { get; }
        public bool IsManaged { get; }

        public IEnumerable<MetaProperty> Properties => _properties.Value;

        private MetaRelationship(RelationshipMetadataBase relationshipMetadata, string kind, string relatedEntity, string lookupColumn, string intersectEntity, string behavior)
        {
            Kind = kind;
            SchemaName = relationshipMetadata.SchemaName;
            RelatedEntity = relatedEntity;
            LookupColumn = lookupColumn;
            IntersectEntity = intersectEntity;
            Behavior = behavior;
            IsCustom = relationshipMetadata.IsCustomRelationship ?? false;
            IsManaged = relationshipMetadata.IsManaged ?? false;
            _properties = new Lazy<IEnumerable<MetaProperty>>(() => PropertyHelper.GetProperties(relationshipMetadata));
        }

        public static MetaRelationship FromOneToMany(OneToManyRelationshipMetadata relationshipMetadata)
        {
            return new MetaRelationship(relationshipMetadata, OneToManyKind, relationshipMetadata.ReferencingEntity,
                $"{relationshipMetadata.ReferencingEntity}.{relationshipMetadata.ReferencingAttribute}", null,
                MetadataHelper.GetCascadeSummary(relationshipMetadata.CascadeConfiguration));
        }

        public static MetaRelationship FromManyToOne(OneToManyRelationshipMetadata relationshipMetadata)
        {
            return new MetaRelationship(relationshipMetadata, ManyToOneKind, relationshipMetadata.ReferencedEntity,
                relationshipMetadata.ReferencingAttribute, null,
                MetadataHelper.GetCascadeSummary(relationshipMetadata.CascadeConfiguration));
        }

        public static MetaRelationship FromManyToMany(ManyToManyRelationshipMetadata relationshipMetadata, string logicalName)
        {
            var relatedEntity = string.Equals(relationshipMetadata.Entity1LogicalName, logicalName, StringComparison.OrdinalIgnoreCase)
                ? relationshipMetadata.Entity2LogicalName
                : relationshipMetadata.Entity1LogicalName;
            return new MetaRelationship(relationshipMetadata, ManyToManyKind, relatedEntity, null, relationshipMetadata.IntersectEntityName, null);
        }
    }
}
