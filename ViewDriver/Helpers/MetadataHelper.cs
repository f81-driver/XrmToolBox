using Formula81.XrmToolBox.Shared.Xrm.Extensions.Metadata;
using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Helpers
{
    internal static class MetadataHelper
    {
        internal static ViewEntity CreateEntity(EntityMetadata entityMetadata, byte[] iconData, bool isFavorite)
        {
            return new ViewEntity(entityMetadata.ObjectTypeCode,
                entityMetadata.LogicalName,
                entityMetadata.GetDisplayLabel(),
                entityMetadata.SchemaName,
                GetLabel(entityMetadata.Description),
                entityMetadata.IsCustomEntity ?? false,
                entityMetadata.IsActivity ?? false,
                entityMetadata.IsValidForAdvancedFind ?? false,
                entityMetadata.IsPrivate ?? false,
                entityMetadata.IsIntersect ?? false,
                iconData,
                isFavorite);
        }

        /// <param name="getEntityDisplayName">Display name of another table, for the related tables of lookups</param>
        internal static EntityDetail CreateEntityDetail(EntityMetadata entityMetadata, Func<string, string> getEntityDisplayName)
        {
            var attributes = (entityMetadata.Attributes ?? Enumerable.Empty<AttributeMetadata>())
                // Name and yomi helper columns of lookups and choices, and columns that cannot be read, are of no use in a view
                .Where(am => string.IsNullOrEmpty(am.AttributeOf) && (am.IsValidForRead ?? true))
                .Select(am => CreateAttribute(am, entityMetadata))
                .ToList();
            var displayName = entityMetadata.GetDisplayLabel();
            var relatedTables = new[] { RelatedTable.CreateRoot(entityMetadata.LogicalName, displayName, entityMetadata.PrimaryIdAttribute) }
                .Concat((entityMetadata.ManyToOneRelationships ?? Enumerable.Empty<OneToManyRelationshipMetadata>())
                    .Where(rm => !string.IsNullOrEmpty(rm.ReferencingAttribute) && !string.IsNullOrEmpty(rm.ReferencedEntity))
                    .Select(rm => (Relationship: rm, Lookup: attributes.FirstOrDefault(va => va.LogicalName == rm.ReferencingAttribute)))
                    .Where(r => r.Lookup != null)
                    .Select(r => RelatedTable.CreateRelated(r.Lookup.LogicalName,
                        r.Lookup.DisplayName,
                        r.Relationship.ReferencedEntity,
                        getEntityDisplayName(r.Relationship.ReferencedEntity) ?? r.Relationship.ReferencedEntity,
                        r.Relationship.ReferencedAttribute))
                    .GroupBy(rt => (rt.LookupAttribute, rt.EntityLogicalName))
                    .Select(g => g.First())
                    .OrderBy(rt => rt.LookupDisplayName)
                    .ThenBy(rt => rt.EntityDisplayName))
                .ToList();
            return new EntityDetail(entityMetadata.LogicalName,
                displayName,
                entityMetadata.ObjectTypeCode,
                entityMetadata.PrimaryIdAttribute,
                entityMetadata.PrimaryNameAttribute,
                attributes,
                relatedTables);
        }

        private static ViewAttribute CreateAttribute(AttributeMetadata attributeMetadata, EntityMetadata entityMetadata)
        {
            return new ViewAttribute(entityMetadata.LogicalName,
                attributeMetadata.LogicalName,
                attributeMetadata.GetDisplayLabel(),
                GetTypeName(attributeMetadata),
                GetKind(attributeMetadata),
                GetOptions(attributeMetadata),
                (attributeMetadata as LookupAttributeMetadata)?.Targets,
                attributeMetadata.LogicalName == entityMetadata.PrimaryIdAttribute,
                attributeMetadata.LogicalName == entityMetadata.PrimaryNameAttribute,
                // Unknown is allowed: older metadata does not tell
                attributeMetadata.IsValidForGrid ?? true,
                attributeMetadata.IsValidForAdvancedFind?.Value ?? true);
        }

        internal static string GetLabel(Label label)
        {
            return label?.UserLocalizedLabel?.Label ?? label?.LocalizedLabels?.FirstOrDefault()?.Label;
        }

        private static AttributeKind GetKind(AttributeMetadata attributeMetadata)
        {
            switch (attributeMetadata)
            {
                case LookupAttributeMetadata _:
                    return AttributeKind.Lookup;
                case StringAttributeMetadata _:
                case MemoAttributeMetadata _:
                    return AttributeKind.Text;
                case MultiSelectPicklistAttributeMetadata _:
                    return AttributeKind.MultiChoice;
                case EnumAttributeMetadata _:
                    return AttributeKind.Choice;
                case BooleanAttributeMetadata _:
                    return AttributeKind.Boolean;
                case DateTimeAttributeMetadata _:
                    return AttributeKind.Date;
                case IntegerAttributeMetadata _:
                case BigIntAttributeMetadata _:
                case DecimalAttributeMetadata _:
                case DoubleAttributeMetadata _:
                case MoneyAttributeMetadata _:
                    return AttributeKind.Number;
                case UniqueIdentifierAttributeMetadata _:
                    return AttributeKind.Identifier;
                default:
                    return attributeMetadata.AttributeType == AttributeTypeCode.EntityName ? AttributeKind.Text : AttributeKind.Other;
            }
        }

        private static IReadOnlyList<ViewOption> GetOptions(AttributeMetadata attributeMetadata)
        {
            switch (attributeMetadata)
            {
                case EnumAttributeMetadata enumAttributeMetadata:
                    return (enumAttributeMetadata.OptionSet?.Options ?? Enumerable.Empty<OptionMetadata>())
                        .Where(om => om.Value.HasValue)
                        .Select(om => new ViewOption(om.Value.Value.ToString(CultureInfo.InvariantCulture), GetLabel(om.Label) ?? om.Value.ToString()))
                        .ToList();
                case BooleanAttributeMetadata booleanAttributeMetadata:
                    return new[]
                    {
                        new ViewOption("1", GetLabel(booleanAttributeMetadata.OptionSet?.TrueOption?.Label) ?? "Yes"),
                        new ViewOption("0", GetLabel(booleanAttributeMetadata.OptionSet?.FalseOption?.Label) ?? "No")
                    };
                default:
                    return null;
            }
        }

        private static string GetTypeName(AttributeMetadata attributeMetadata)
        {
            switch (attributeMetadata)
            {
                case LookupAttributeMetadata _ when attributeMetadata.AttributeType == AttributeTypeCode.Customer:
                    return "Customer";
                case LookupAttributeMetadata _ when attributeMetadata.AttributeType == AttributeTypeCode.Owner:
                    return "Owner";
                case LookupAttributeMetadata _ when attributeMetadata.AttributeType == AttributeTypeCode.PartyList:
                    return "Activity Party";
                case LookupAttributeMetadata _:
                    return "Lookup";
                case StringAttributeMetadata _:
                    return "Text";
                case MemoAttributeMetadata _:
                    return "Multiline Text";
                case MultiSelectPicklistAttributeMetadata _:
                    return "Choices";
                case StateAttributeMetadata _:
                    return "Status";
                case StatusAttributeMetadata _:
                    return "Status Reason";
                case PicklistAttributeMetadata _:
                    return "Choice";
                case BooleanAttributeMetadata _:
                    return "Yes/No";
                case DateTimeAttributeMetadata dateTimeAttributeMetadata:
                    return dateTimeAttributeMetadata.Format == DateTimeFormat.DateOnly ? "Date Only" : "Date and Time";
                case IntegerAttributeMetadata _:
                    return "Whole Number";
                case BigIntAttributeMetadata _:
                    return "Big Integer";
                case DecimalAttributeMetadata _:
                    return "Decimal";
                case DoubleAttributeMetadata _:
                    return "Float";
                case MoneyAttributeMetadata _:
                    return "Currency";
                case UniqueIdentifierAttributeMetadata _:
                    return "Unique Identifier";
                default:
                    var typeName = attributeMetadata.AttributeTypeName?.Value ?? attributeMetadata.AttributeType?.ToString();
                    return typeName?.EndsWith("Type") ?? false ? typeName.Substring(0, typeName.Length - 4) : typeName;
            }
        }
    }
}
