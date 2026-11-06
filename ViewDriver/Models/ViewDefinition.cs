using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Models
{
    /// <summary>
    /// The FetchXML and LayoutXML of a view. Edits are made on the XML itself, so anything the editor does not know is kept.
    /// </summary>
    public class ViewDefinition
    {
        private const string FetchElementName = "fetch";
        private const string EntityElementName = "entity";
        private const string LinkEntityElementName = "link-entity";
        private const string AttributeElementName = "attribute";
        private const string FilterElementName = "filter";
        private const string ConditionElementName = "condition";
        private const string OrderElementName = "order";
        private const string GridElementName = "grid";
        private const string RowElementName = "row";
        private const string CellElementName = "cell";

        private const string NameAttributeName = "name";
        private const string AliasAttributeName = "alias";
        private const string FromAttributeName = "from";
        private const string ToAttributeName = "to";
        private const string LinkTypeAttributeName = "link-type";
        private const string TypeAttributeName = "type";
        private const string WidthAttributeName = "width";

        private const string AliasPrefix = "a_";

        public XDocument Fetch { get; private set; }
        public XDocument Layout { get; }

        private XElement EntityElement => Fetch.Root.Element(EntityElementName);
        private XElement RowElement => Layout.Root.Element(RowElementName);

        public string EntityName => (string)EntityElement.Attribute(NameAttributeName);

        private ViewDefinition(XDocument fetch, XDocument layout)
        {
            Fetch = fetch;
            Layout = layout;
        }

        public static ViewDefinition Parse(string fetchXml, string layoutXml, EntityDetail entityDetail)
        {
            var fetch = ParseFetch(fetchXml, entityDetail.LogicalName);
            var layout = string.IsNullOrWhiteSpace(layoutXml) ? null : XDocument.Parse(layoutXml);
            if (layout?.Root?.Name != GridElementName)
            {
                layout = CreateLayout(entityDetail);
            }
            if (layout.Root.Element(RowElementName) == null)
            {
                layout.Root.Add(CreateRowElement(entityDetail));
            }
            var viewDefinition = new ViewDefinition(fetch, layout);
            if (string.IsNullOrWhiteSpace(layoutXml))
            {
                // Without a layout every queried column is shown
                foreach (var cellName in viewDefinition.GetAttributeCellNames().Where(n => !IsPrimaryId(n, entityDetail)))
                {
                    viewDefinition.RowElement.Add(CreateCellElement(cellName, ViewColumn.DefaultWidth));
                }
            }
            return viewDefinition;
        }

        /// <summary>
        /// A new view from a template. Columns, sorting and conditions on columns the table does not have are skipped.
        /// </summary>
        public static ViewDefinition CreateFromTemplate(EntityDetail entityDetail, ViewTemplate viewTemplate)
        {
            string Resolve(string templateAttribute)
            {
                var attributeName = templateAttribute == TemplateColumns.PrimaryName ? entityDetail.PrimaryNameAttribute : templateAttribute;
                return !string.IsNullOrEmpty(attributeName) && entityDetail.TryGetAttribute(attributeName, out var viewAttribute)
                    ? viewAttribute.LogicalName
                    : null;
            }

            var cells = viewTemplate.Columns
                .Select(c => (Name: Resolve(c.Attribute), c.Width))
                .Where(c => c.Name != null)
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            // A view without columns is of no use, so it gets the primary name at least
            if (!cells.Any() && !string.IsNullOrEmpty(entityDetail.PrimaryNameAttribute))
            {
                cells.Add((entityDetail.PrimaryNameAttribute, 300));
            }

            var entityElement = new XElement(EntityElementName, new XAttribute(NameAttributeName, entityDetail.LogicalName));
            foreach (var attributeName in cells.Select(c => c.Name)
                .Concat(string.IsNullOrEmpty(entityDetail.PrimaryIdAttribute) ? Enumerable.Empty<string>() : new[] { entityDetail.PrimaryIdAttribute })
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                entityElement.Add(new XElement(AttributeElementName, new XAttribute(NameAttributeName, attributeName)));
            }
            foreach (var viewTemplateOrder in viewTemplate.Orders)
            {
                var attributeName = Resolve(viewTemplateOrder.Attribute);
                if (attributeName != null)
                {
                    entityElement.Add(new XElement(OrderElementName, new XAttribute(AttributeElementName, attributeName),
                        new XAttribute("descending", viewTemplateOrder.Descending ? "true" : "false")));
                }
            }
            var conditionElements = viewTemplate.Conditions
                .Select(c => (Condition: c, AttributeName: Resolve(c.Attribute)))
                .Where(c => c.AttributeName != null)
                .Select(c => CreateConditionElement(c.AttributeName, c.Condition))
                .ToList();
            if (conditionElements.Any())
            {
                entityElement.Add(new XElement(FilterElementName, new XAttribute(TypeAttributeName, viewTemplate.IsMatchAny ? "or" : "and"), conditionElements));
            }
            var fetch = new XDocument(new XElement(FetchElementName,
                new XAttribute("version", "1.0"),
                new XAttribute("output-format", "xml-platform"),
                new XAttribute("mapping", "logical"),
                new XAttribute("distinct", "false"),
                entityElement));

            var layout = CreateLayout(entityDetail);
            foreach (var (name, width) in cells)
            {
                layout.Root.Element(RowElementName).Add(CreateCellElement(name, Math.Max(ViewColumn.MinWidth, Math.Min(ViewColumn.MaxWidth, width))));
            }
            return new ViewDefinition(fetch, layout);
        }

        /// <summary>
        /// A template of this view: the columns, sorting and top level conditions that work on every table. The rest is left out,
        /// and told in <paramref name="leftOut"/>.
        /// </summary>
        public ViewTemplate ToTemplate(string name, string description, EntityDetail entityDetail, out List<string> leftOut)
        {
            var viewTemplate = new ViewTemplate { Name = name, Description = description, IsMatchAny = IsMatchAny };
            var leftOutList = new List<string>();
            string ToTemplateAttribute(string attributeName)
            {
                return string.Equals(attributeName, entityDetail.PrimaryNameAttribute, StringComparison.OrdinalIgnoreCase) ? TemplateColumns.PrimaryName
                    : TemplateColumns.Contains(attributeName) ? attributeName.ToLowerInvariant()
                    : null;
            }
            string GetDisplayName(string cellName)
            {
                return !cellName.Contains(".") && entityDetail.TryGetAttribute(cellName, out var viewAttribute) ? viewAttribute.DisplayName : cellName;
            }

            foreach (var (cellName, width) in GetCells())
            {
                var templateAttribute = cellName.Contains(".") ? null : ToTemplateAttribute(cellName);
                if (templateAttribute != null)
                {
                    viewTemplate.Columns.Add(new ViewTemplateColumn { Attribute = templateAttribute, Width = width });
                }
                else
                {
                    leftOutList.Add($"Column {GetDisplayName(cellName)}");
                }
            }
            foreach (var orderElement in GetOrderElements())
            {
                var attributeName = (string)orderElement.Attribute("attribute");
                var templateAttribute = ToTemplateAttribute(attributeName);
                if (templateAttribute != null)
                {
                    viewTemplate.Orders.Add(new ViewTemplateOrder
                    {
                        Attribute = templateAttribute,
                        Descending = string.Equals((string)orderElement.Attribute("descending"), "true", StringComparison.OrdinalIgnoreCase)
                    });
                }
                else
                {
                    leftOutList.Add($"Sort on {GetDisplayName(attributeName ?? (string)orderElement.Attribute("alias") ?? "?")}");
                }
            }
            foreach (var conditionElement in GetConditionElements())
            {
                var attributeName = (string)conditionElement.Attribute("attribute");
                // A condition on a joined table (entityname) does not work on another table
                var templateAttribute = conditionElement.Attribute("entityname") == null ? ToTemplateAttribute(attributeName) : null;
                if (templateAttribute != null)
                {
                    viewTemplate.Conditions.Add(new ViewTemplateCondition
                    {
                        Attribute = templateAttribute,
                        Operator = (string)conditionElement.Attribute("operator"),
                        Value = (string)conditionElement.Attribute("value"),
                        Values = conditionElement.Elements("value").Select(e => e.Value).ToList()
                    });
                }
                else
                {
                    leftOutList.Add($"Condition on {GetDisplayName(attributeName ?? "?")}");
                }
            }
            if (NestedFilterCount > 0)
            {
                leftOutList.Add($"{NestedFilterCount} nested filter group(s)");
            }
            if (LinkFilterCount > 0 || LinkOrderCount > 0)
            {
                leftOutList.Add("Filters and sorting on related tables");
            }
            leftOut = leftOutList;
            return viewTemplate;
        }

        private static XElement CreateConditionElement(string attributeName, ViewTemplateCondition viewTemplateCondition)
        {
            var conditionElement = new XElement(ConditionElementName,
                new XAttribute("attribute", attributeName),
                new XAttribute("operator", viewTemplateCondition.Operator ?? "eq"));
            if (viewTemplateCondition.Value != null)
            {
                conditionElement.SetAttributeValue("value", viewTemplateCondition.Value);
            }
            foreach (var value in viewTemplateCondition.Values)
            {
                conditionElement.Add(new XElement("value", value));
            }
            return conditionElement;
        }

        public string GetFetchXml(bool indent)
        {
            return Fetch.Root.ToString(indent ? SaveOptions.None : SaveOptions.DisableFormatting);
        }

        public string GetLayoutXml(bool indent)
        {
            return Layout.Root.ToString(indent ? SaveOptions.None : SaveOptions.DisableFormatting);
        }

        #region Columns

        public IEnumerable<(string Name, int Width)> GetCells()
        {
            return RowElement.Elements(CellElementName)
                .Select(e => ((string)e.Attribute(NameAttributeName), int.TryParse((string)e.Attribute(WidthAttributeName), out var width) ? width : ViewColumn.DefaultWidth))
                .Where(c => !string.IsNullOrEmpty(c.Item1));
        }

        /// <summary>
        /// Writes the cells in the order and with the widths of the columns, keeping other settings of existing cells
        /// </summary>
        public void SetCells(IEnumerable<ViewColumn> viewColumns)
        {
            var cellElements = RowElement.Elements(CellElementName)
                .GroupBy(e => (string)e.Attribute(NameAttributeName) ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.First());
            RowElement.Elements(CellElementName).Remove();
            foreach (var viewColumn in viewColumns)
            {
                if (cellElements.TryGetValue(viewColumn.Name, out var cellElement))
                {
                    cellElement.SetAttributeValue(WidthAttributeName, viewColumn.Width);
                }
                else
                {
                    cellElement = CreateCellElement(viewColumn.Name, viewColumn.Width);
                }
                RowElement.Add(cellElement);
            }
        }

        /// <summary>
        /// Makes sure the column of a cell ("column" or "alias.column") is queried
        /// </summary>
        public void EnsureAttribute(string cellName)
        {
            if (!TryGetCellEntityElement(cellName, out var entityElement, out var attributeName))
            {
                throw new InvalidOperationException($"The related table of column '{cellName}' is not in the query");
            }
            if (!entityElement.Elements(AttributeElementName).Any(e => (string)e.Attribute(NameAttributeName) == attributeName))
            {
                var attributeElement = new XElement(AttributeElementName, new XAttribute(NameAttributeName, attributeName));
                var lastAttributeElement = entityElement.Elements(AttributeElementName).LastOrDefault();
                if (lastAttributeElement != null)
                {
                    lastAttributeElement.AddAfterSelf(attributeElement);
                }
                else
                {
                    entityElement.AddFirst(attributeElement);
                }
            }
        }

        /// <summary>
        /// Stops querying the column of a removed cell, unless it is still needed; drops a related table that is no longer used
        /// </summary>
        public void RemoveAttribute(string cellName, string primaryIdAttribute)
        {
            if (!TryGetCellEntityElement(cellName, out var entityElement, out var attributeName))
            {
                return;
            }
            var isRoot = entityElement == EntityElement;
            if ((isRoot && string.Equals(attributeName, primaryIdAttribute, StringComparison.OrdinalIgnoreCase))
                || entityElement.Elements(OrderElementName).Any(e => (string)e.Attribute(AttributeElementName) == attributeName))
            {
                return;
            }
            entityElement.Elements(AttributeElementName).Where(e => (string)e.Attribute(NameAttributeName) == attributeName).Remove();
            // An outer join without columns, filters or nested joins has no effect; an inner join (the default) still filters.
            // A condition elsewhere can still use the join through its alias (entityname), such as "records without a related X".
            var alias = (string)entityElement.Attribute(AliasAttributeName);
            if (!isRoot
                && !entityElement.HasElements
                && string.Equals((string)entityElement.Attribute(LinkTypeAttributeName), "outer", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrEmpty(alias) || !Fetch.Descendants(ConditionElementName).Any(e => (string)e.Attribute("entityname") == alias)))
            {
                entityElement.Remove();
            }
        }

        /// <summary>
        /// Returns the alias of the outer join to a related table, adding the join when the query does not have it yet
        /// </summary>
        public string EnsureLink(RelatedTable relatedTable)
        {
            var linkElement = EntityElement.Elements(LinkEntityElementName).FirstOrDefault(e =>
                (string)e.Attribute(NameAttributeName) == relatedTable.EntityLogicalName
                && (string)e.Attribute(FromAttributeName) == relatedTable.PrimaryIdAttribute
                && (string)e.Attribute(ToAttributeName) == relatedTable.LookupAttribute);
            if (linkElement == null)
            {
                linkElement = new XElement(LinkEntityElementName,
                    new XAttribute(NameAttributeName, relatedTable.EntityLogicalName),
                    new XAttribute(FromAttributeName, relatedTable.PrimaryIdAttribute),
                    new XAttribute(ToAttributeName, relatedTable.LookupAttribute),
                    new XAttribute("visible", "false"),
                    new XAttribute(LinkTypeAttributeName, "outer"));
                var lastLinkElement = EntityElement.Elements(LinkEntityElementName).LastOrDefault();
                if (lastLinkElement != null)
                {
                    lastLinkElement.AddAfterSelf(linkElement);
                }
                else
                {
                    EntityElement.Add(linkElement);
                }
            }
            var alias = (string)linkElement.Attribute(AliasAttributeName);
            if (string.IsNullOrEmpty(alias))
            {
                alias = CreateAlias(relatedTable.LookupAttribute);
                linkElement.SetAttributeValue(AliasAttributeName, alias);
            }
            return alias;
        }

        /// <summary>
        /// Finds the table of a join by its alias, and the lookup column it joins on when it is joined to the view table
        /// </summary>
        public bool TryGetLink(string alias, out string entityName, out string lookupAttribute)
        {
            var linkElement = FindLinkElement(alias);
            entityName = (string)linkElement?.Attribute(NameAttributeName);
            lookupAttribute = linkElement?.Parent == EntityElement ? (string)linkElement.Attribute(ToAttributeName) : null;
            return linkElement != null;
        }

        public IEnumerable<string> GetLinkEntityNames()
        {
            return EntityElement.Descendants(LinkEntityElementName)
                .Select(e => (string)e.Attribute(NameAttributeName))
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct();
        }

        /// <summary>
        /// Queried columns as cell names: "column" for the view table, "alias.column" for joined tables with an alias
        /// </summary>
        public ISet<string> GetAttributeCellNames()
        {
            var cellNames = new HashSet<string>(EntityElement.Elements(AttributeElementName)
                .Where(e => e.Attribute(AliasAttributeName) == null)
                .Select(e => (string)e.Attribute(NameAttributeName))
                .Where(n => !string.IsNullOrEmpty(n)));
            foreach (var linkElement in EntityElement.Descendants(LinkEntityElementName))
            {
                var alias = (string)linkElement.Attribute(AliasAttributeName);
                if (!string.IsNullOrEmpty(alias))
                {
                    cellNames.UnionWith(linkElement.Elements(AttributeElementName)
                        .Where(e => e.Attribute(AliasAttributeName) == null)
                        .Select(e => $"{alias}.{(string)e.Attribute(NameAttributeName)}"));
                }
            }
            return cellNames;
        }

        #endregion

        #region Filter

        public bool IsMatchAny
        {
            get => string.Equals((string)GetFilterElement(false)?.Attribute(TypeAttributeName), "or", StringComparison.OrdinalIgnoreCase);
            set => GetFilterElement(true).SetAttributeValue(TypeAttributeName, value ? "or" : "and");
        }

        public IEnumerable<XElement> GetConditionElements()
        {
            return GetFilterElement(false)?.Elements(ConditionElementName) ?? Enumerable.Empty<XElement>();
        }

        public XElement AddCondition(string attributeName, string conditionOperator)
        {
            var conditionElement = new XElement(ConditionElementName,
                new XAttribute("attribute", attributeName),
                new XAttribute("operator", conditionOperator));
            var filterElement = GetFilterElement(true);
            var lastConditionElement = filterElement.Elements(ConditionElementName).LastOrDefault();
            if (lastConditionElement != null)
            {
                lastConditionElement.AddAfterSelf(conditionElement);
            }
            else
            {
                filterElement.AddFirst(conditionElement);
            }
            return conditionElement;
        }

        public void RemoveCondition(XElement conditionElement)
        {
            var filterElement = conditionElement.Parent;
            conditionElement.Remove();
            if (filterElement != null && !filterElement.HasElements)
            {
                filterElement.Remove();
            }
        }

        public int NestedFilterCount => GetFilterElement(false)?.Elements(FilterElementName).Count() ?? 0;

        public int LinkFilterCount => EntityElement.Descendants(LinkEntityElementName).Sum(e => e.Elements(FilterElementName).Count());

        private XElement GetFilterElement(bool create)
        {
            var filterElement = EntityElement.Element(FilterElementName);
            if (filterElement == null && create)
            {
                filterElement = new XElement(FilterElementName, new XAttribute(TypeAttributeName, "and"));
                var anchorElement = EntityElement.Elements().LastOrDefault(e => e.Name == AttributeElementName || e.Name == OrderElementName);
                if (anchorElement != null)
                {
                    anchorElement.AddAfterSelf(filterElement);
                }
                else
                {
                    EntityElement.AddFirst(filterElement);
                }
            }
            return filterElement;
        }

        #endregion

        #region Orders

        public IEnumerable<XElement> GetOrderElements()
        {
            return EntityElement.Elements(OrderElementName);
        }

        public XElement AddOrder(string attributeName)
        {
            var orderElement = new XElement(OrderElementName, new XAttribute("attribute", attributeName), new XAttribute("descending", "false"));
            var anchorElement = EntityElement.Elements().LastOrDefault(e => e.Name == AttributeElementName || e.Name == OrderElementName);
            if (anchorElement != null)
            {
                anchorElement.AddAfterSelf(orderElement);
            }
            else
            {
                EntityElement.AddFirst(orderElement);
            }
            return orderElement;
        }

        public void RemoveOrder(XElement orderElement)
        {
            orderElement.Remove();
        }

        public int LinkOrderCount => EntityElement.Descendants(LinkEntityElementName).Sum(e => e.Elements(OrderElementName).Count());

        #endregion

        /// <summary>
        /// Replaces the query, for example from the FetchXML editor or FetchXML Builder. Cells of columns that are no longer queried are
        /// removed, and newly queried columns get a cell, so adding a column in FetchXML Builder also shows it in the view.
        /// </summary>
        public void ReplaceFetch(string fetchXml, EntityDetail entityDetail)
        {
            var fetch = ParseFetch(fetchXml, EntityName);
            var previousCellNames = GetAttributeCellNames();
            Fetch = fetch;
            var cellNames = GetAttributeCellNames();
            foreach (var cellElement in RowElement.Elements(CellElementName).ToList())
            {
                if (!cellNames.Contains((string)cellElement.Attribute(NameAttributeName) ?? string.Empty))
                {
                    cellElement.Remove();
                }
            }
            var layoutCellNames = new HashSet<string>(GetCells().Select(c => c.Name));
            foreach (var cellName in cellNames.Where(n => !previousCellNames.Contains(n) && !layoutCellNames.Contains(n) && !IsPrimaryId(n, entityDetail)))
            {
                RowElement.Add(CreateCellElement(cellName, ViewColumn.DefaultWidth));
            }
        }

        private bool TryGetCellEntityElement(string cellName, out XElement entityElement, out string attributeName)
        {
            var separatorIndex = cellName.IndexOf('.');
            if (separatorIndex < 0)
            {
                entityElement = EntityElement;
                attributeName = cellName;
                return true;
            }
            entityElement = FindLinkElement(cellName.Substring(0, separatorIndex));
            attributeName = cellName.Substring(separatorIndex + 1);
            return entityElement != null;
        }

        private XElement FindLinkElement(string alias)
        {
            return EntityElement.Descendants(LinkEntityElementName).FirstOrDefault(e => (string)e.Attribute(AliasAttributeName) == alias);
        }

        private string CreateAlias(string lookupAttribute)
        {
            var aliases = new HashSet<string>(EntityElement.Descendants(LinkEntityElementName)
                .Select(e => (string)e.Attribute(AliasAttributeName))
                .Where(a => !string.IsNullOrEmpty(a)), StringComparer.OrdinalIgnoreCase);
            var alias = AliasPrefix + lookupAttribute;
            for (var index = 2; aliases.Contains(alias); index++)
            {
                alias = $"{AliasPrefix}{lookupAttribute}{index}";
            }
            return alias;
        }

        private static bool IsPrimaryId(string cellName, EntityDetail entityDetail)
        {
            return string.Equals(cellName, entityDetail.PrimaryIdAttribute, StringComparison.OrdinalIgnoreCase);
        }

        private static XDocument ParseFetch(string fetchXml, string entityLogicalName)
        {
            if (string.IsNullOrWhiteSpace(fetchXml))
            {
                throw new InvalidOperationException("The view has no FetchXML");
            }
            var fetch = XDocument.Parse(fetchXml);
            var entityElement = fetch.Root?.Name == FetchElementName ? fetch.Root.Element(EntityElementName) : null;
            if (entityElement == null)
            {
                throw new InvalidOperationException("The FetchXML must have a <fetch> element with an <entity> element");
            }
            var entityName = (string)entityElement.Attribute(NameAttributeName);
            if (!string.Equals(entityName, entityLogicalName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The FetchXML queries table '{entityName}', but the view is for table '{entityLogicalName}'");
            }
            return fetch;
        }

        private static XDocument CreateLayout(EntityDetail entityDetail)
        {
            var gridElement = new XElement(GridElementName,
                new XAttribute(NameAttributeName, "resultset"),
                new XAttribute("select", "1"),
                new XAttribute("icon", "1"),
                new XAttribute("preview", "1"),
                CreateRowElement(entityDetail));
            if (entityDetail.ObjectTypeCode.HasValue)
            {
                gridElement.SetAttributeValue("object", entityDetail.ObjectTypeCode.Value);
            }
            if (!string.IsNullOrEmpty(entityDetail.PrimaryNameAttribute))
            {
                gridElement.SetAttributeValue("jump", entityDetail.PrimaryNameAttribute);
            }
            return new XDocument(gridElement);
        }

        private static XElement CreateRowElement(EntityDetail entityDetail)
        {
            var rowElement = new XElement(RowElementName, new XAttribute(NameAttributeName, "result"));
            if (!string.IsNullOrEmpty(entityDetail.PrimaryIdAttribute))
            {
                rowElement.SetAttributeValue("id", entityDetail.PrimaryIdAttribute);
            }
            return rowElement;
        }

        private static XElement CreateCellElement(string name, int width)
        {
            return new XElement(CellElementName, new XAttribute(NameAttributeName, name), new XAttribute(WidthAttributeName, width));
        }
    }
}
