using Formula81.XrmToolBox.Shared.Json.Extensions;
using Formula81.XrmToolBox.Shared.Parts.Components;
using Formula81.XrmToolBox.Shared.Xrm;
using Formula81.XrmToolBox.Shared.Xrm.Caches;
using Formula81.XrmToolBox.Shared.Xrm.Extensions.Metadata;
using Formula81.XrmToolBox.Shared.Xrm.Helpers;
using Formula81.XrmToolBox.Tools.AuditGoggles.Caches;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Helpers
{
    internal class EntityAuditHelper
    {
        private const string CreatedByAttributeName = "createdby";
        private const string CreatedOnAttributeName = "createdon";
        private const string ModifiedByAttributeName = "modifiedby";
        private const string ModifiedOnAttributeName = "modifiedon";

        internal static readonly Audit_Operation[] SupportedAuditOperations = new Audit_Operation[] { Audit_Operation.Create, Audit_Operation.Update, Audit_Operation.Delete, Audit_Operation.Access };
        internal static readonly int[] SupportedAuditOperationValues = SupportedAuditOperations.Select(ao => (int)ao).ToArray();

        private readonly CrmServiceClient _serviceClient;

        internal EntityAuditHelper(CrmServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }

        internal EntityAudit ParseAudit(Audit audit, Entity entity, EntityMetadata entityMetadata, ColumnSet columns, ColorCombination colorCombination)
        {
            switch (audit.Operation)
            {
                case Audit_Operation.Create:
                case Audit_Operation.Update:
                case Audit_Operation.Delete:
                    switch (audit.Action)
                    {
                        case Audit_Action.Create:
                        case Audit_Action.Update:
                        case Audit_Action.Delete:
                        case Audit_Action.Activate:
                        case Audit_Action.Deactivate:
                        case Audit_Action.Assign:
                        case Audit_Action.SetState:
                            return GetCUDEntityAudit(audit, entity, entityMetadata, columns, colorCombination);
                        case Audit_Action.AssociateEntities:
                        case Audit_Action.DisassociateEntities:
                            return (columns?.AllColumns ?? true) ? GetNNEntityAudit(audit, entity, entityMetadata, colorCombination) : null;
                        default: return null;
                    }
                case Audit_Operation.Access:
                    return (columns?.AllColumns ?? true) ? GetAccessEntityAudit(audit, entity, entityMetadata, colorCombination) : null;
                default : return null;
            }
        }

        internal IEnumerable<EntityAudit> GetDefaultEntityAudits(Entity entity, EntityMetadata entityMetadata, ColorCombination colorCombination)
        {
            var createdEntityAudit = GetDefaultEntityAudit(Audit_Operation.Create, entity, entityMetadata, colorCombination);
            if (createdEntityAudit != null)
            {
                yield return createdEntityAudit;
            }
            var updateEntityAudit = GetDefaultEntityAudit(Audit_Operation.Update, entity, entityMetadata, colorCombination);
            if (updateEntityAudit != null)
            {
                yield return updateEntityAudit;
            }
        }

        private EntityAudit GetDefaultEntityAudit(Audit_Operation operation, Entity entity, EntityMetadata entityMetadata, ColorCombination colorCombination)
        {
            if (operation != Audit_Operation.Create && operation != Audit_Operation.Update)
            {
                throw new ArgumentException("Only Create and Update are supported for defaulting", nameof(operation));
            }
            var changedDateAttributeName = operation == Audit_Operation.Create ? CreatedOnAttributeName : ModifiedOnAttributeName;
            var changedByAttributeName = operation == Audit_Operation.Create ? CreatedByAttributeName : ModifiedByAttributeName;
            return entity.TryGetAttributeValue<DateTime?>(changedDateAttributeName, out var changedDate) ?
                new EntityAudit(changedDate.Value.ToLocalTime(),
                    (entity.TryGetAttributeValue<EntityReference>(changedByAttributeName, out var createdBy) ? createdBy : null)?.Name ?? createdBy?.Id.ToString(),
                    GetEntityLookupValue(entity, entityMetadata),
                    operation.ToString(),
                    colorCombination,
                    Enumerable.Empty<EntityAuditDetail>())
                : null;
        }

        private EntityAudit GetCUDEntityAudit(Audit audit, Entity entity, EntityMetadata entityMetadata, ColumnSet columns, ColorCombination colorCombination)
        {
            var attributeNumbers = audit.AttributeMask?.Split(',').Select(a => int.TryParse(a, out int columnNumber) ? (int?)columnNumber : null)
                .Where(a => a.HasValue)
                .Select(a => a.Value)
                    ?? Enumerable.Empty<int>();
            var changeData = JObject.Parse(audit.ChangeData);
            var attributes = entityMetadata.Attributes.Where(am => am.ColumnNumber.HasValue
                                && ((columns?.AllColumns ?? true) || (columns?.Columns.Contains(am.LogicalName) ?? true)))
                            .ToDictionary(am => am.ColumnNumber.Value);

            return audit.CreatedOn.HasValue ?
                new EntityAudit(audit.CreatedOn.Value.ToLocalTime(),
                    audit.UserId?.Name ?? audit.UserId.Id.ToString(),
                    GetEntityLookupValue(entity, entityMetadata),
                    audit.ActionDisplayName,
                    colorCombination,
                    attributeNumbers.Where(an => attributes.ContainsKey(an))
                        .Select(an => CreateEntityAuditDetail(changeData, entity, attributes[an]))
                        .OrderBy(ead => ead.ChangedFieldName)
                        .ToList())
                : null;
        }

        private EntityAudit GetNNEntityAudit(Audit audit, Entity entity, EntityMetadata entityMetadata, ColorCombination colorCombination)
        {
            if (string.IsNullOrEmpty(audit.AttributeMask))
            {
                var detailDatas = audit.ChangeData?.Split('~');
                if ((detailDatas?.Length ?? 0) == 2)
                {
                    var entityRef = XrmHelper.FromCommaSeparated(detailDatas[1]);
                    var entityRefMetadata = _serviceClient.GetEntityMetadata(entityRef.LogicalName);
                    var displayValue = GetEntityLookupValue(entityRef, entityRefMetadata);
                    var details = new EntityAuditDetail[] { new EntityAuditDetail(entityMetadata?.GetDisplayLabel(), null, new EntityAuditValue(entityRef, displayValue)) };
                    return new EntityAudit(audit.CreatedOn.Value.ToLocalTime(),
                        audit.UserId?.Name ?? audit.UserId.Id.ToString(),
                        GetEntityLookupValue(entity, entityMetadata),
                        audit.ActionDisplayName,
                        colorCombination,
                        details);
                }
            }
            return null;
        }

        private EntityAudit GetAccessEntityAudit(Audit audit, Entity entity, EntityMetadata entityMetadata, ColorCombination colorCombination)
        {
            return new EntityAudit(audit.CreatedOn.Value.ToLocalTime(),
                        audit.UserId?.Name ?? audit.UserId.Id.ToString(),
                        GetEntityLookupValue(entity, entityMetadata),
                        audit.ActionDisplayName,
                        colorCombination,
                        null);
        }

        private EntityAuditDetail CreateEntityAuditDetail(JObject changeData, Entity entity, AttributeMetadata attributeMetadata)
        {
            var changeColumnData = changeData.SelectToken($"$.changedAttributes[?(@.logicalName == 'Deleted columnNumber {attributeMetadata.ColumnNumber}')]")
                ?? changeData.SelectToken($"$.changedAttributes[?(@.logicalName == '{attributeMetadata.LogicalName}')]");
            //var fallbackValue = entity.TryGetAttributeValue(attributeMetadata.LogicalName, out object value) ? value : null;
            var oldValueToken = changeColumnData?.SelectToken("oldValue");
            var oldNameToken = changeColumnData?.SelectToken("oldName");
            var oldValue = ParseJTokenValue(oldValueToken, attributeMetadata, oldNameToken/*, fallbackValue*/);
            var newValueToken = changeColumnData?.SelectToken("newValue");
            var newNameToken = changeColumnData?.SelectToken("newName");
            var newValue = ParseJTokenValue(newValueToken, attributeMetadata, newNameToken);
            return new EntityAuditDetail(attributeMetadata.DisplayName?.UserLocalizedLabel?.Label ?? attributeMetadata.LogicalName, oldValue, newValue);
        }

        private EntityAuditValue ParseJTokenValue(JToken valueToken, AttributeMetadata attributeMetadata, JToken nameToken/*, object fallbackValue = null*/)
        {
            object value = null;
            object displayValue = null;

            if (valueToken != null
                && attributeMetadata.AttributeType.HasValue)
            {
                switch (attributeMetadata.AttributeType.Value)
                {
                    case AttributeTypeCode.Boolean:
                        value = valueToken.TryValue<bool>(out var boolValue) ? (bool?)boolValue : null;
                        if (value != null
                            && attributeMetadata is BooleanAttributeMetadata booleanAttributeMetadata)
                        {
                            displayValue = booleanAttributeMetadata.GetDisplayValue(boolValue);
                        }
                        break;

                    case AttributeTypeCode.Memo:
                    case AttributeTypeCode.String:
                        value = valueToken.Value<string>();
                        displayValue = value?.ToString();
                        break;

                    case AttributeTypeCode.DateTime:
                        var x = attributeMetadata as DateTimeAttributeMetadata;
                        value = valueToken.TryValue<DateTime>(out var dateTimeValue) ? (DateTime?)dateTimeValue : null;
                        displayValue = value == null ? null
                            : dateTimeValue.ToString(x.DateTimeBehavior.Equals(DateTimeBehavior.DateOnly) ? "d" : "g");
                        break;

                    case AttributeTypeCode.Decimal:
                        value = valueToken.TryValue<decimal>(out var decimalValue) ? (decimal?)decimalValue : null;
                        displayValue = value?.ToString();
                        break;

                    case AttributeTypeCode.Double:
                    case AttributeTypeCode.Money:
                        value = valueToken.TryValue<double>(out var doubleValue) ? (double?)doubleValue : null;
                        displayValue = value?.ToString();
                        break;

                    case AttributeTypeCode.Integer:
                        value = valueToken.TryValue<int>(out var intValue) ? (int?)intValue : null;
                        displayValue = value?.ToString();
                        break;

                    //case AttributeTypeCode.PartyList: break;
                    case AttributeTypeCode.Picklist:
                    case AttributeTypeCode.State:
                    case AttributeTypeCode.Status:
                        value = valueToken.TryValue<int>(out var optionSetValue) ? new OptionSetValue(optionSetValue) : null;
                        displayValue = nameToken?.Value<string>();
                        break;
                    case AttributeTypeCode.Uniqueidentifier:
                        value = valueToken.Value<Guid?>();
                        break;
                    //case AttributeTypeCode.CalendarRules: break;
                    //case AttributeTypeCode.Virtual: break;
                    //case AttributeTypeCode.BigInt: break;
                    //case AttributeTypeCode.ManagedProperty: break;
                    //case AttributeTypeCode.EntityName: break;

                    case AttributeTypeCode.Customer:
                    case AttributeTypeCode.Lookup:
                    case AttributeTypeCode.Owner:
                        var auditValue = valueToken.Value<string>();
                        if (string.IsNullOrEmpty(auditValue))
                        {
                            value = null;
                        }
                        else
                        {
                            var entityRef = XrmHelper.FromCommaSeparated(auditValue);
                            if (entityRef != null)
                            {
                                var entityMetadata = _serviceClient.GetEntityMetadata(entityRef.LogicalName);
                                displayValue = GetEntityLookupValue(entityRef, entityMetadata);
                                value = entityRef;
                            }
                        }
                        break;
                }
            }
            return value == null ? null : new EntityAuditValue(value, displayValue);
        }

        private static EntityLookupValue GetEntityLookupValue(EntityReference entityRef, EntityMetadata entityMetadata)
        {
            var iconData = EntityIconCache.Instance.Get(entityMetadata);
            entityRef.Name = EntityNameCache.Instance.Get(entityRef.LogicalName, entityRef.Id);
            return new EntityLookupValue(entityMetadata.ObjectTypeCode,
                entityRef.Id,
                entityRef.Name,
                entityRef.LogicalName,
                entityMetadata?.GetDisplayLabel(),
                iconData);
        }

        private static EntityLookupValue GetEntityLookupValue(Entity entity, EntityMetadata entityMetadata)
        {
            var iconData = EntityIconCache.Instance.Get(entityMetadata);
            return new EntityLookupValue(entityMetadata.ObjectTypeCode,
                entity.Id,
                entity.GetAttributeValue<string>(entityMetadata.PrimaryNameAttribute),
                entity.LogicalName,
                entityMetadata?.GetDisplayLabel(),
                iconData);
        }

        internal static string[] GetEntityAuditEntityColumns(EntityMetadata entityMetadata)
        {
            return entityMetadata?.IsAuditEnabled?.Value ?? false ? entityMetadata.Attributes.Where(am => (am.IsAuditEnabled?.Value ?? false)
                            && (am.IsValidForRead ?? false)
                            && string.IsNullOrEmpty(am.AttributeOf))
                    .Select(am => am.LogicalName)
                    .ToArray()
                : new string[] { entityMetadata.PrimaryIdAttribute, entityMetadata.PrimaryNameAttribute,
                    CreatedByAttributeName, CreatedOnAttributeName,
                    ModifiedByAttributeName, ModifiedOnAttributeName };
        }

        internal static bool CheckChangedDate(EntityAudit entityAudit, IEnumerable<ConditionExpression> criteriaConditions)
        {
            var createdonConditions = criteriaConditions?.Where(ce => ce.AttributeName.Equals(Audit.ColumnNames.CreatedOn));
            if (createdonConditions?.Any() ?? false)
            {
                var ooa = createdonConditions.FirstOrDefault(ce => ce.Operator.Equals(ConditionOperator.OnOrAfter))?.Values.OfType<DateTime?>().FirstOrDefault();
                if (ooa.HasValue)
                {
                    if (entityAudit.ChangedDate < ooa.Value)
                    {
                        return false;
                    }
                }
                var oob = createdonConditions.FirstOrDefault(ce => ce.Operator.Equals(ConditionOperator.OnOrBefore))?.Values.OfType<DateTime?>().FirstOrDefault();
                if (oob.HasValue)
                {
                    if (entityAudit.ChangedDate > oob.Value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
