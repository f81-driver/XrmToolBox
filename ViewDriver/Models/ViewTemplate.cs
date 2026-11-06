using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// The start of a new view, for any table. A template only holds columns every table can have (the system columns and
    /// the primary name of the table), so it works on all tables; columns a table does not have are skipped.
    /// </summary>
    public class ViewTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<ViewTemplateColumn> Columns { get; set; } = new List<ViewTemplateColumn>();
        public List<ViewTemplateOrder> Orders { get; set; } = new List<ViewTemplateOrder>();
        public bool IsMatchAny { get; set; }
        public List<ViewTemplateCondition> Conditions { get; set; } = new List<ViewTemplateCondition>();

        /// <summary>Comes with the tool: it cannot be changed or deleted, only duplicated</summary>
        [XmlIgnore]
        public bool IsBuiltIn { get; set; }

        [XmlIgnore]
        public string DisplayName => IsBuiltIn ? $"{Name} (built-in)" : Name;

        [XmlIgnore]
        public string ColumnsText => Columns.Any() ? string.Join(", ", Columns.Select(c => TemplateColumns.GetLabel(c.Attribute))) : "No columns";

        [XmlIgnore]
        public string SortText => Orders.Any()
            ? string.Join(", ", Orders.Select(o => $"{TemplateColumns.GetLabel(o.Attribute)} {(o.Descending ? "descending" : "ascending")}"))
            : "None";

        [XmlIgnore]
        public string FilterText => Conditions.Any()
            ? string.Join(IsMatchAny ? " or " : " and ", Conditions.Select(c => c.ToString()))
            : "None";

        [XmlIgnore]
        public string ToolTip => string.IsNullOrEmpty(Description)
            ? $"Columns: {ColumnsText}\nSort: {SortText}\nFilter: {FilterText}"
            : $"{Description}\n\nColumns: {ColumnsText}\nSort: {SortText}\nFilter: {FilterText}";

        public ViewTemplate Clone()
        {
            return new ViewTemplate
            {
                Name = Name,
                Description = Description,
                Columns = Columns.Select(c => new ViewTemplateColumn { Attribute = c.Attribute, Width = c.Width }).ToList(),
                Orders = Orders.Select(o => new ViewTemplateOrder { Attribute = o.Attribute, Descending = o.Descending }).ToList(),
                IsMatchAny = IsMatchAny,
                Conditions = Conditions.Select(c => new ViewTemplateCondition { Attribute = c.Attribute, Operator = c.Operator, Value = c.Value, Values = c.Values.ToList() }).ToList()
            };
        }

        public static IReadOnlyList<ViewTemplate> BuiltIn { get; } = new[]
        {
            new ViewTemplate
            {
                Name = "Default",
                Description = "The primary name of the table, active records only",
                Columns = { new ViewTemplateColumn { Attribute = TemplateColumns.PrimaryName, Width = 300 } },
                Orders = { new ViewTemplateOrder { Attribute = TemplateColumns.PrimaryName } },
                Conditions = { new ViewTemplateCondition { Attribute = "statecode", Operator = "eq", Value = "0" } },
                IsBuiltIn = true
            },
            new ViewTemplate
            {
                Name = "Trace",
                Description = "Who created and last modified the records and when, followed by the primary name of the table, last modified first, no filter",
                Columns =
                {
                    new ViewTemplateColumn { Attribute = "createdby", Width = 150 },
                    new ViewTemplateColumn { Attribute = "createdon", Width = 125 },
                    new ViewTemplateColumn { Attribute = "modifiedby", Width = 150 },
                    new ViewTemplateColumn { Attribute = "modifiedon", Width = 125 },
                    new ViewTemplateColumn { Attribute = TemplateColumns.PrimaryName, Width = 300 }
                },
                Orders = { new ViewTemplateOrder { Attribute = "modifiedon", Descending = true } },
                IsBuiltIn = true
            }
        };
    }

    public class ViewTemplateColumn
    {
        [XmlAttribute]
        public string Attribute { get; set; }
        [XmlAttribute]
        public int Width { get; set; }

        [XmlIgnore]
        public string Label => TemplateColumns.GetLabel(Attribute);
    }

    public class ViewTemplateOrder
    {
        [XmlAttribute]
        public string Attribute { get; set; }
        [XmlAttribute]
        public bool Descending { get; set; }
    }

    public class ViewTemplateCondition
    {
        [XmlAttribute]
        public string Attribute { get; set; }
        [XmlAttribute]
        public string Operator { get; set; }
        /// <summary>The value attribute of the condition, when it has one</summary>
        [XmlAttribute]
        public string Value { get; set; }
        /// <summary>The value elements of the condition, for operators such as in</summary>
        [XmlElement("Value")]
        public List<string> Values { get; set; } = new List<string>();

        public override string ToString()
        {
            var values = Value != null ? new[] { Value } : Values.ToArray();
            var operatorLabel = ConditionOperator.Get(Operator).Label;
            return values.Length == 0
                ? $"{TemplateColumns.GetLabel(Attribute)} {operatorLabel}"
                : $"{TemplateColumns.GetLabel(Attribute)} {operatorLabel} {string.Join(", ", values)}";
        }
    }

    /// <summary>
    /// The columns a template can hold: the primary name of the table, and the system columns most tables have
    /// </summary>
    public static class TemplateColumns
    {
        /// <summary>Stands for the primary name column of the table the template is used for</summary>
        public const string PrimaryName = "{primaryname}";

        public static IReadOnlyList<(string Attribute, string Label, int Width)> All { get; } = new[]
        {
            (PrimaryName, "Primary name", 300),
            ("createdon", "Created On", 125),
            ("createdby", "Created By", 150),
            ("createdonbehalfby", "Created By (Delegate)", 150),
            ("modifiedon", "Modified On", 125),
            ("modifiedby", "Modified By", 150),
            ("modifiedonbehalfby", "Modified By (Delegate)", 150),
            ("overriddencreatedon", "Record Created On", 125),
            ("ownerid", "Owner", 150),
            ("owningbusinessunit", "Owning Business Unit", 150),
            ("statecode", "Status", 100),
            ("statuscode", "Status Reason", 125)
        };

        public static bool Contains(string attribute)
        {
            return All.Any(c => string.Equals(c.Attribute, attribute, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetLabel(string attribute)
        {
            var column = All.FirstOrDefault(c => string.Equals(c.Attribute, attribute, StringComparison.OrdinalIgnoreCase));
            return column.Label ?? attribute;
        }
    }

    /// <summary>
    /// The templates people made themselves, kept once for all connections
    /// </summary>
    public class ViewTemplateSettings
    {
        public List<ViewTemplate> Templates { get; set; } = new List<ViewTemplate>();
    }
}
