using Formula81.XrmToolBox.Shared.Xrm.Extensions.Metadata;
using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Helpers
{
    internal static class MetadataHelper
    {
        // Entity metadata properties that have their own tab, so they are left out of the Properties tab
        private static readonly string[] EntityDetailPropertyNames = new string[]
        {
            nameof(EntityMetadata.Attributes),
            nameof(EntityMetadata.OneToManyRelationships),
            nameof(EntityMetadata.ManyToOneRelationships),
            nameof(EntityMetadata.ManyToManyRelationships),
            nameof(EntityMetadata.Privileges),
            nameof(EntityMetadata.Keys)
        };

        internal static MetaEntity CreateEntity(EntityMetadata entityMetadata, byte[] iconData, bool isFavorite)
        {
            return new MetaEntity(entityMetadata.ObjectTypeCode,
                entityMetadata.MetadataId,
                entityMetadata.LogicalName,
                entityMetadata.GetDisplayLabel(),
                entityMetadata.SchemaName,
                GetLabel(entityMetadata.Description),
                entityMetadata.IsCustomEntity ?? false,
                entityMetadata.IsManaged ?? false,
                entityMetadata.IsActivity ?? false,
                entityMetadata.IsIntersect ?? false,
                entityMetadata.IsAuditEnabled?.Value ?? false,
                iconData,
                isFavorite);
        }

        internal static EntityDetail CreateEntityDetail(MetaEntity entity, EntityMetadata entityMetadata)
        {
            var logicalName = entityMetadata.LogicalName;
            var attributes = (entityMetadata.Attributes ?? Enumerable.Empty<AttributeMetadata>())
                .Select(am => new MetaAttribute(am))
                .OrderBy(ma => ma.DisplayName)
                .ToList();
            var relationships = (entityMetadata.ManyToOneRelationships ?? Enumerable.Empty<OneToManyRelationshipMetadata>())
                .Select(MetaRelationship.FromManyToOne)
                .Concat((entityMetadata.OneToManyRelationships ?? Enumerable.Empty<OneToManyRelationshipMetadata>())
                    .Select(MetaRelationship.FromOneToMany))
                .Concat((entityMetadata.ManyToManyRelationships ?? Enumerable.Empty<ManyToManyRelationshipMetadata>())
                    .Select(rm => MetaRelationship.FromManyToMany(rm, logicalName)))
                .OrderBy(mr => mr.Kind)
                .ThenBy(mr => mr.SchemaName)
                .ToList();
            var keys = (entityMetadata.Keys ?? Enumerable.Empty<EntityKeyMetadata>())
                .Select(km => new MetaKey(GetLabel(km.DisplayName) ?? km.LogicalName,
                    km.LogicalName,
                    string.Join(", ", km.KeyAttributes ?? new string[0]),
                    km.EntityKeyIndexStatus.ToString()))
                .ToList();
            var privileges = (entityMetadata.Privileges ?? Enumerable.Empty<SecurityPrivilegeMetadata>())
                .Select(pm => new MetaPrivilege(pm.Name, pm.PrivilegeType.ToString(), pm.CanBeBasic, pm.CanBeLocal, pm.CanBeDeep, pm.CanBeGlobal))
                .OrderBy(mp => mp.Name)
                .ToList();
            return new EntityDetail(entity,
                entityMetadata.EntitySetName,
                entityMetadata.PrimaryIdAttribute,
                entityMetadata.PrimaryNameAttribute,
                GetOwnershipName(entityMetadata.OwnershipType),
                attributes,
                relationships,
                keys,
                privileges,
                PropertyHelper.GetProperties(entityMetadata, EntityDetailPropertyNames));
        }

        internal static string GetLabel(Label label)
        {
            return label?.UserLocalizedLabel?.Label ?? label?.LocalizedLabels?.FirstOrDefault()?.Label;
        }

        internal static string GetTypeName(AttributeMetadata attributeMetadata)
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
                case StringAttributeMetadata stringAttributeMetadata:
                    return GetStringTypeName(stringAttributeMetadata.FormatName);
                case MemoAttributeMetadata memoAttributeMetadata:
                    return Equals(memoAttributeMetadata.FormatName, MemoFormatName.RichText) ? "Rich Text" : "Multiline Text";
                case MultiSelectPicklistAttributeMetadata _:
                    return "Choices";
                case StateAttributeMetadata _:
                    return "Status";
                case StatusAttributeMetadata _:
                    return "Status Reason";
                case PicklistAttributeMetadata _:
                    return "Choice";
                case EntityNameAttributeMetadata _:
                    return "Table Name";
                case BooleanAttributeMetadata _:
                    return "Yes/No";
                case DateTimeAttributeMetadata dateTimeAttributeMetadata:
                    return dateTimeAttributeMetadata.Format == DateTimeFormat.DateOnly ? "Date Only" : "Date and Time";
                case IntegerAttributeMetadata integerAttributeMetadata:
                    return GetIntegerTypeName(integerAttributeMetadata.Format);
                case BigIntAttributeMetadata _:
                    return "Big Integer";
                case DecimalAttributeMetadata _:
                    return "Decimal";
                case DoubleAttributeMetadata _:
                    return "Float";
                case MoneyAttributeMetadata _:
                    return "Currency";
                case ImageAttributeMetadata _:
                    return "Image";
                case FileAttributeMetadata _:
                    return "File";
                case UniqueIdentifierAttributeMetadata _:
                    return "Unique Identifier";
                case ManagedPropertyAttributeMetadata _:
                    return "Managed Property";
                default:
                    var typeName = attributeMetadata.AttributeTypeName?.Value ?? attributeMetadata.AttributeType?.ToString();
                    return typeName?.EndsWith("Type") ?? false ? typeName.Substring(0, typeName.Length - 4) : typeName;
            }
        }

        internal static string GetDetails(AttributeMetadata attributeMetadata)
        {
            string details;
            switch (attributeMetadata)
            {
                case LookupAttributeMetadata lookupAttributeMetadata:
                    details = string.Join(", ", lookupAttributeMetadata.Targets ?? new string[0]);
                    break;
                case StringAttributeMetadata stringAttributeMetadata:
                    details = stringAttributeMetadata.MaxLength.HasValue ? $"Max {stringAttributeMetadata.MaxLength}" : null;
                    break;
                case MemoAttributeMetadata memoAttributeMetadata:
                    details = memoAttributeMetadata.MaxLength.HasValue ? $"Max {memoAttributeMetadata.MaxLength}" : null;
                    break;
                case EnumAttributeMetadata enumAttributeMetadata:
                    var optionSet = enumAttributeMetadata.OptionSet;
                    var count = optionSet?.Options?.Count ?? 0;
                    details = (optionSet?.IsGlobal ?? false) ? $"{count} options, global: {optionSet.Name}" : $"{count} options";
                    break;
                case BooleanAttributeMetadata booleanAttributeMetadata:
                    details = $"{booleanAttributeMetadata.GetDisplayValue(true)} / {booleanAttributeMetadata.GetDisplayValue(false)}";
                    break;
                case DateTimeAttributeMetadata dateTimeAttributeMetadata:
                    details = GetDateTimeBehaviorName(dateTimeAttributeMetadata.DateTimeBehavior?.Value);
                    break;
                case DecimalAttributeMetadata decimalAttributeMetadata:
                    details = $"Precision {decimalAttributeMetadata.Precision}";
                    break;
                case DoubleAttributeMetadata doubleAttributeMetadata:
                    details = $"Precision {doubleAttributeMetadata.Precision}";
                    break;
                case MoneyAttributeMetadata moneyAttributeMetadata:
                    details = $"Precision {moneyAttributeMetadata.Precision}";
                    break;
                case FileAttributeMetadata fileAttributeMetadata:
                    details = fileAttributeMetadata.MaxSizeInKB.HasValue ? $"Max {fileAttributeMetadata.MaxSizeInKB} KB" : null;
                    break;
                default:
                    details = null;
                    break;
            }
            var source = GetSourceName(attributeMetadata.SourceType);
            return string.IsNullOrEmpty(source) ? details
                : string.IsNullOrEmpty(details) ? source
                : $"{source}, {details}";
        }

        internal static int? GetMaxLength(AttributeMetadata attributeMetadata)
        {
            return (attributeMetadata as StringAttributeMetadata)?.MaxLength
                ?? (attributeMetadata as MemoAttributeMetadata)?.MaxLength;
        }

        internal static IEnumerable<MetaOption> GetOptions(AttributeMetadata attributeMetadata)
        {
            switch (attributeMetadata)
            {
                case EnumAttributeMetadata enumAttributeMetadata:
                    return (enumAttributeMetadata.OptionSet?.Options ?? Enumerable.Empty<OptionMetadata>())
                        .Select(CreateOption)
                        .ToList();
                case BooleanAttributeMetadata booleanAttributeMetadata:
                    return new[] { booleanAttributeMetadata.OptionSet?.FalseOption, booleanAttributeMetadata.OptionSet?.TrueOption }
                        .Where(om => om != null)
                        .Select(CreateOption)
                        .ToList();
                default:
                    return null;
            }
        }

        internal static string GetRequiredLevelName(AttributeRequiredLevel? requiredLevel)
        {
            switch (requiredLevel)
            {
                case AttributeRequiredLevel.None: return "Optional";
                case AttributeRequiredLevel.SystemRequired: return "System Required";
                case AttributeRequiredLevel.ApplicationRequired: return "Business Required";
                case AttributeRequiredLevel.Recommended: return "Recommended";
                default: return null;
            }
        }

        internal static string GetSourceName(int? sourceType)
        {
            switch (sourceType)
            {
                case 1: return "Calculated";
                case 2: return "Rollup";
                case 3: return "Formula";
                default: return null;
            }
        }

        internal static string GetCascadeSummary(CascadeConfiguration cascadeConfiguration)
        {
            if (cascadeConfiguration == null)
            {
                return null;
            }
            var shared = new[] { cascadeConfiguration.Assign, cascadeConfiguration.Reparent, cascadeConfiguration.Share, cascadeConfiguration.Unshare };
            if (cascadeConfiguration.Delete == CascadeType.Cascade && shared.All(ct => ct == CascadeType.Cascade))
            {
                return "Parental";
            }
            if (shared.All(ct => ct == CascadeType.NoCascade))
            {
                switch (cascadeConfiguration.Delete)
                {
                    case CascadeType.RemoveLink: return "Referential, remove link";
                    case CascadeType.Restrict: return "Referential, restrict delete";
                }
            }
            return $"Custom, delete: {cascadeConfiguration.Delete}";
        }

        private static MetaOption CreateOption(OptionMetadata optionMetadata)
        {
            var description = GetLabel(optionMetadata.Description);
            if (optionMetadata is StatusOptionMetadata statusOptionMetadata)
            {
                description = string.IsNullOrEmpty(description) ? $"Status {statusOptionMetadata.State}" : $"Status {statusOptionMetadata.State}, {description}";
            }
            return new MetaOption(optionMetadata.Value, GetLabel(optionMetadata.Label), description, optionMetadata.Color);
        }

        private static string GetOwnershipName(OwnershipTypes? ownershipType)
        {
            switch (ownershipType)
            {
                case OwnershipTypes.UserOwned: return "User or Team";
                case OwnershipTypes.OrganizationOwned: return "Organization";
                case OwnershipTypes.BusinessOwned: return "Business Unit";
                case OwnershipTypes.None: return "None";
                default: return ownershipType?.ToString();
            }
        }

        private static string GetStringTypeName(StringFormatName formatName)
        {
            if (Equals(formatName, StringFormatName.Email)) return "Email";
            if (Equals(formatName, StringFormatName.Url)) return "URL";
            if (Equals(formatName, StringFormatName.Phone)) return "Phone";
            if (Equals(formatName, StringFormatName.TextArea)) return "Text Area";
            if (Equals(formatName, StringFormatName.TickerSymbol)) return "Ticker Symbol";
            if (Equals(formatName, StringFormatName.Json)) return "JSON";
            if (Equals(formatName, StringFormatName.RichText)) return "Rich Text";
            return "Text";
        }

        private static string GetIntegerTypeName(IntegerFormat? format)
        {
            switch (format)
            {
                case IntegerFormat.Duration: return "Duration";
                case IntegerFormat.TimeZone: return "Time Zone";
                case IntegerFormat.Language: return "Language";
                case IntegerFormat.Locale: return "Locale";
                default: return "Whole Number";
            }
        }

        private static string GetDateTimeBehaviorName(string behavior)
        {
            switch (behavior)
            {
                case "UserLocal": return "User local";
                case "DateOnly": return "Date only";
                case "TimeZoneIndependent": return "Time zone independent";
                default: return behavior;
            }
        }
    }
}
