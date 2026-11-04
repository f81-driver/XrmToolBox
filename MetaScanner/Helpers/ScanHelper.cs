using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Helpers
{
    internal static class ScanHelper
    {
        internal const int ScanResultsMax = 5000;

        internal static IList<ScanResult> Scan(IEnumerable<EntityMetadata> entityMetadatas, IDictionary<string, MetaEntity> entities, string text, out bool limitExceeded)
        {
            var results = new List<ScanResult>();
            limitExceeded = false;
            if (string.IsNullOrWhiteSpace(text))
            {
                return results;
            }
            text = text.Trim();
            var hasNumber = int.TryParse(text, out var number);
            foreach (var entityMetadata in entityMetadatas.OrderBy(em => em.LogicalName))
            {
                if (!entities.TryGetValue(entityMetadata.LogicalName, out var entity))
                {
                    continue;
                }
                if (TryMatch(text, out var matchedOn, out var matchedText,
                    ("Display name", entity.DisplayName), ("Logical name", entity.LogicalName), ("Schema name", entity.SchemaName), ("Description", entity.Description)))
                {
                    results.Add(new ScanResult(ScanResult.TableKind, entity, entity.DisplayName, entity.LogicalName, null, matchedOn, matchedText, null));
                }
                foreach (var attributeMetadata in (entityMetadata.Attributes ?? Enumerable.Empty<AttributeMetadata>())
                    .Where(am => string.IsNullOrEmpty(am.AttributeOf)))
                {
                    var displayName = MetadataHelper.GetLabel(attributeMetadata.DisplayName) ?? attributeMetadata.LogicalName;
                    if (TryMatch(text, out matchedOn, out matchedText,
                        ("Display name", displayName), ("Logical name", attributeMetadata.LogicalName), ("Schema name", attributeMetadata.SchemaName),
                        ("Description", MetadataHelper.GetLabel(attributeMetadata.Description))))
                    {
                        results.Add(new ScanResult(ScanResult.ColumnKind, entity, displayName, attributeMetadata.LogicalName,
                            MetadataHelper.GetTypeName(attributeMetadata), matchedOn, matchedText, attributeMetadata.LogicalName));
                    }
                    foreach (var option in MetadataHelper.GetOptions(attributeMetadata) ?? Enumerable.Empty<MetaOption>())
                    {
                        if (Contains(option.Label, text)
                            || (hasNumber && option.Value == number))
                        {
                            results.Add(new ScanResult(ScanResult.ChoiceKind, entity, displayName, attributeMetadata.LogicalName,
                                MetadataHelper.GetTypeName(attributeMetadata), "Choice value", $"{option.Value}: {option.Label}", attributeMetadata.LogicalName));
                        }
                    }
                }
                if (results.Count >= ScanResultsMax)
                {
                    limitExceeded = true;
                    return results.Take(ScanResultsMax).ToList();
                }
            }
            return results;
        }

        private static bool TryMatch(string text, out string matchedOn, out string matchedText, params (string Name, string Value)[] candidates)
        {
            foreach (var (name, value) in candidates)
            {
                if (Contains(value, text))
                {
                    matchedOn = name;
                    matchedText = value;
                    return true;
                }
            }
            matchedOn = null;
            matchedText = null;
            return false;
        }

        private static bool Contains(string value, string text)
        {
            return value?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
