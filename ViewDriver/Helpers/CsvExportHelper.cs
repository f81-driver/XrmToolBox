using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Helpers
{
    internal static class CsvExportHelper
    {
        internal static void Export(string filePath, IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
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
