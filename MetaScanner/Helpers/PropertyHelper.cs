using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Helpers
{
    internal static class PropertyHelper
    {
        private const int EnumerableItemsMax = 50;

        // Every metadata object carries this, it holds nothing worth showing
        private const string ExtensionDataPropertyName = "ExtensionData";

        internal static IEnumerable<MetaProperty> GetProperties(object metadata, params string[] excludedPropertyNames)
        {
            if (metadata == null)
            {
                return Enumerable.Empty<MetaProperty>();
            }
            return metadata.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(pi => pi.CanRead
                    && pi.GetIndexParameters().Length == 0
                    && pi.Name != ExtensionDataPropertyName
                    && !excludedPropertyNames.Contains(pi.Name))
                .Select(pi => new MetaProperty(pi.Name, FormatValue(GetValue(pi, metadata))))
                .OrderBy(mp => mp.Name)
                .ToList();
        }

        internal static string FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string text:
                    return text;
                case Label label:
                    return MetadataHelper.GetLabel(label);
                case CascadeConfiguration cascadeConfiguration:
                    return string.Join(", ", GetProperties(cascadeConfiguration)
                        .Where(mp => !string.IsNullOrEmpty(mp.Value))
                        .Select(mp => $"{mp.Name}: {mp.Value}"));
                case OptionSetMetadataBase optionSetMetadata:
                    return optionSetMetadata.Name;
                case DateTime dateTime:
                    return dateTime.ToString("G", CultureInfo.CurrentCulture);
                case IEnumerable enumerable:
                    var items = enumerable.Cast<object>().ToList();
                    var formatted = string.Join(", ", items.Take(EnumerableItemsMax).Select(FormatValue));
                    return items.Count > EnumerableItemsMax ? $"{formatted}, ... ({items.Count} items)" : formatted;
                default:
                    var type = value.GetType();
                    if (type.IsPrimitive || type.IsEnum || value is Guid || value is decimal)
                    {
                        return Convert.ToString(value, CultureInfo.CurrentCulture);
                    }
                    // Managed properties, AttributeTypeDisplayName, DateTimeBehavior and the like wrap a Value
                    var valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                    if (valueProperty != null && valueProperty.GetIndexParameters().Length == 0)
                    {
                        return FormatValue(GetValue(valueProperty, value));
                    }
                    var nameProperty = type.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    if (nameProperty?.PropertyType == typeof(string))
                    {
                        return GetValue(nameProperty, value) as string;
                    }
                    return value.ToString();
            }
        }

        private static object GetValue(PropertyInfo propertyInfo, object instance)
        {
            try
            {
                return propertyInfo.GetValue(instance);
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }
    }
}
