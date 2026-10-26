using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Helpers
{
    internal static class CsvExportHelper
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        private static readonly string[] Headers = new string[] { "Changed Date", "Changed By", "Entity", "Record", "Record Id", "Event", "Field", "Old Value", "New Value" };

        internal static void Export(string filePath, IEnumerable<EntityAudit> entityAudits)
        {
            // Excel splits columns on the Windows list separator (";" in many European locales)
            var separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            var builder = new StringBuilder();
            AppendLine(builder, separator, Headers);
            foreach (var entityAudit in entityAudits)
            {
                var auditColumns = new string[]
                {
                    entityAudit.ChangedDate.ToString(DateFormat, CultureInfo.InvariantCulture),
                    entityAudit.ChangedByName,
                    entityAudit.Record?.EntityDisplayName,
                    entityAudit.Record?.Name,
                    entityAudit.Record?.Id.ToString(),
                    entityAudit.EventName
                };
                var details = entityAudit.Details?.ToList() ?? new List<EntityAuditDetail>();
                if (details.Any())
                {
                    foreach (var detail in details)
                    {
                        AppendLine(builder, separator, auditColumns.Concat(new string[] { detail.ChangedFieldName, detail.OldValue?.DisplayText, detail.NewValue?.DisplayText }));
                    }
                }
                else
                {
                    AppendLine(builder, separator, auditColumns.Concat(new string[] { null, null, null }));
                }
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
