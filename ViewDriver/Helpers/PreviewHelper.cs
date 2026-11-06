using Formula81.XrmToolBox.Tools.ViewDriver.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Formula81.XrmToolBox.Tools.ViewDriver.Helpers
{
    internal static class PreviewHelper
    {
        internal static PreviewResult Retrieve(IOrganizationService service, string fetchXml, IReadOnlyList<string> cellNames, int pageSize, int pageNumber, string pagingCookie)
        {
            var pagedFetchXml = CreatePagedFetchXml(fetchXml, pageSize, pageNumber, pagingCookie, out var isPaged);
            var entityCollection = service.RetrieveMultiple(new FetchExpression(pagedFetchXml));
            var rows = entityCollection.Entities
                .Select(e => new PreviewRow(e.Id, e.LogicalName, cellNames.Select(cn => FormatValue(e, cn)).ToArray()))
                .ToList();
            return new PreviewResult(cellNames,
                rows,
                pageNumber,
                isPaged && entityCollection.MoreRecords,
                entityCollection.PagingCookie,
                isPaged ? entityCollection.TotalRecordCount : -1,
                entityCollection.TotalRecordCountLimitExceeded);
        }

        /// <summary>
        /// Adds paging to the view query; aggregate queries and queries with a top cannot be paged and run as they are
        /// </summary>
        internal static string CreatePagedFetchXml(string fetchXml, int pageSize, int pageNumber, string pagingCookie, out bool isPaged)
        {
            var fetchElement = XElement.Parse(fetchXml);
            isPaged = !string.Equals((string)fetchElement.Attribute("aggregate"), "true", StringComparison.OrdinalIgnoreCase)
                && fetchElement.Attribute("top") == null;
            if (!isPaged)
            {
                return fetchXml;
            }
            fetchElement.SetAttributeValue("count", pageSize);
            fetchElement.SetAttributeValue("page", pageNumber);
            fetchElement.SetAttributeValue("paging-cookie", string.IsNullOrEmpty(pagingCookie) ? null : pagingCookie);
            fetchElement.SetAttributeValue("returntotalrecordcount", "true");
            return fetchElement.ToString(SaveOptions.DisableFormatting);
        }

        internal static string FormatValue(Entity entity, string cellName)
        {
            if (entity.FormattedValues.TryGetValue(cellName, out var formattedValue))
            {
                return formattedValue;
            }
            if (!entity.Attributes.TryGetValue(cellName, out var value))
            {
                return null;
            }
            if (value is AliasedValue aliasedValue)
            {
                value = aliasedValue.Value;
            }
            switch (value)
            {
                case null:
                    return null;
                case EntityReference entityReference:
                    return entityReference.Name ?? entityReference.Id.ToString();
                case OptionSetValue optionSetValue:
                    return optionSetValue.Value.ToString(CultureInfo.CurrentCulture);
                case OptionSetValueCollection optionSetValueCollection:
                    return string.Join("; ", optionSetValueCollection.Select(osv => osv.Value.ToString(CultureInfo.CurrentCulture)));
                case Money money:
                    return money.Value.ToString("N2", CultureInfo.CurrentCulture);
                case DateTime dateTime:
                    return dateTime.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
                case EntityCollection entityCollection:
                    // Activity parties
                    return string.Join("; ", entityCollection.Entities
                        .Select(e => e.GetAttributeValue<EntityReference>("partyid")?.Name ?? e.GetAttributeValue<string>("addressused"))
                        .Where(n => !string.IsNullOrEmpty(n)));
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.CurrentCulture);
                default:
                    return value.ToString();
            }
        }
    }
}
