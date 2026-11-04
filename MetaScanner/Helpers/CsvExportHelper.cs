using Formula81.XrmToolBox.Tools.MetaScanner.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Formula81.XrmToolBox.Tools.MetaScanner.Helpers
{
    internal static class CsvExportHelper
    {
        private static readonly string[] AttributeHeaders = new string[] { "Display Name", "Logical Name", "Schema Name", "Type", "Details", "Required", "Custom", "Managed", "Audit Enabled", "Searchable", "Secured", "Description" };
        private static readonly string[] ScanResultHeaders = new string[] { "Kind", "Table", "Table Logical Name", "Display Name", "Logical Name", "Type", "Matched On", "Matched Text" };

        internal static void ExportAttributes(string filePath, IEnumerable<MetaAttribute> attributes)
        {
            Export(filePath, AttributeHeaders, attributes.Select(ma => new string[]
            {
                ma.DisplayName,
                ma.LogicalName,
                ma.SchemaName,
                ma.TypeName,
                ma.Details,
                ma.RequiredLevel,
                ma.IsCustom.ToString(),
                ma.IsManaged.ToString(),
                ma.IsAuditEnabled.ToString(),
                ma.IsSearchable.ToString(),
                ma.IsSecured.ToString(),
                ma.Description
            }));
        }

        internal static void ExportScanResults(string filePath, IEnumerable<ScanResult> scanResults)
        {
            Export(filePath, ScanResultHeaders, scanResults.Select(sr => new string[]
            {
                sr.Kind,
                sr.Entity?.DisplayName,
                sr.Entity?.LogicalName,
                sr.DisplayName,
                sr.LogicalName,
                sr.TypeName,
                sr.MatchedOn,
                sr.MatchedText
            }));
        }

        private static void Export(string filePath, IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
        {
            // Excel splits columns on the Windows list separator (";" in many European locales)
            var separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            var builder = new StringBuilder();
            AppendLine(builder, separator, headers);
            foreach (var row in rows)
            {
                AppendLine(builder, separator, row);
            }
            // UTF-8 with BOM so Excel recognizes special characters
            File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(true));
        }

        private static void AppendLine(StringBuilder builder, string separator, IEnumerable<string> values)
        {
            builder.AppendLine(string.Join(separator, values.Select(v => Escape(v, separator))));
        }

        private static string Escape(string value, string separator)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return value.Contains(separator) || value.Contains("\"") || value.Contains("\n") || value.Contains("\r")
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}
